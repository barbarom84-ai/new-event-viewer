<#
.SYNOPSIS
  Signs binaries with Authenticode (SHA256 + optional timestamp).

.DESCRIPTION
  Prefers signtool.exe when the Windows SDK is installed; otherwise uses
  Set-AuthenticodeSignature (PowerShell).

  Certificate sources (first match wins):
    1. -PfxPath / $env:EVENTVIEWER_SIGN_PFX (+ password)
    2. -Thumbprint / $env:EVENTVIEWER_SIGN_THUMBPRINT (CurrentUser\My or LocalMachine\My)
    3. -Certificate object

.EXAMPLE
  .\tools\Sign-Authenticode.ps1 -Path bin\WinUI_USB\EventViewer.WinUI.exe -PfxPath secret.pfx
#>
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Path,

    [string]$PfxPath = $env:EVENTVIEWER_SIGN_PFX,

    [SecureString]$PfxPassword,

    [string]$PfxPasswordPlain = $env:EVENTVIEWER_SIGN_PFX_PASSWORD,

    [string]$Thumbprint = $env:EVENTVIEWER_SIGN_THUMBPRINT,

    [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,

    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [switch]$SkipTimestamp,

    [switch]$AllowSelfSigned
)

$ErrorActionPreference = "Stop"

function Resolve-SignTool {
    $candidates = @()
    $kitRoots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    )
    foreach ($root in $kitRoots) {
        if (Test-Path $root) {
            $candidates += Get-ChildItem $root -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending |
                Select-Object -ExpandProperty FullName
        }
    }

    $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        $candidates = @($cmd.Source) + $candidates
    }

    return $candidates | Select-Object -First 1
}

function Get-SigningCertificate {
    if ($Certificate) {
        return $Certificate
    }

    if (-not [string]::IsNullOrWhiteSpace($PfxPath)) {
        if (-not (Test-Path $PfxPath)) {
            throw "PFX introuvable: $PfxPath"
        }

        $pwd = $PfxPassword
        if (-not $pwd -and -not [string]::IsNullOrWhiteSpace($PfxPasswordPlain)) {
            $pwd = ConvertTo-SecureString $PfxPasswordPlain -AsPlainText -Force
        }

        if ($pwd) {
            $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pwd)
            try {
                $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
                return [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
                    (Resolve-Path $PfxPath).Path,
                    $plain,
                    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
                )
            }
            finally {
                [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
            }
        }

        return [System.Security.Cryptography.X509Certificates.X509Certificate2]::new((Resolve-Path $PfxPath).Path)
    }

    if (-not [string]::IsNullOrWhiteSpace($Thumbprint)) {
        $normalized = ($Thumbprint -replace '\s', '').ToUpperInvariant()
        foreach ($storePath in @('Cert:\CurrentUser\My', 'Cert:\LocalMachine\My')) {
            $found = Get-ChildItem $storePath -CodeSigningCert -ErrorAction SilentlyContinue |
                Where-Object { $_.Thumbprint -eq $normalized } |
                Select-Object -First 1
            if ($found) {
                return $found
            }
        }
        throw "Certificat code signing introuvable pour le thumbprint $normalized"
    }

    return $null
}

function Test-IsSelfSigned([System.Security.Cryptography.X509Certificates.X509Certificate2]$cert) {
    return $cert.Subject -eq $cert.Issuer
}

function Sign-WithSignTool {
    param(
        [string]$SignTool,
        [string]$FilePath,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert,
        [string]$Timestamp
    )

    $toolArgs = @(
        "sign",
        "/fd", "SHA256",
        "/sha1", $Cert.Thumbprint,
        "/v"
    )
    if (-not [string]::IsNullOrWhiteSpace($Timestamp)) {
        $toolArgs += @("/tr", $Timestamp, "/td", "SHA256")
    }
    $toolArgs += $FilePath

    & $SignTool @toolArgs
    if ($LASTEXITCODE -ne 0) {
        throw "signtool a echoue ($LASTEXITCODE) pour $FilePath"
    }
}

function Sign-WithPowerShell {
    param(
        [string]$FilePath,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert,
        [string]$Timestamp
    )

    $params = @{
        FilePath      = $FilePath
        Certificate   = $Cert
        HashAlgorithm = "SHA256"
    }
    if (-not [string]::IsNullOrWhiteSpace($Timestamp)) {
        $params.TimestampServer = $Timestamp
    }

    $result = Set-AuthenticodeSignature @params
    # Self-signed builds are rarely fully "Valid" until the CER is in Trusted Root / Publisher.
    if ($result.Status -eq "Valid") {
        return
    }

    $msg = "$($result.Status): $($result.StatusMessage)"
    if ($result.Status -in @("UnknownError", "NotTrusted", "HashMismatch") -or
        $msg -match "root|trusted|chaine|chain|not trusted|auto-signed|self") {
        Write-Warning "Signature apposee mais confiance partielle (souvent OK pour un cert de test): $msg"
        return
    }

    throw "Set-AuthenticodeSignature a echoue ($msg)"
}

$cert = Get-SigningCertificate
if (-not $cert) {
    Write-Host "Aucun certificat fourni - signature ignoree." -ForegroundColor Yellow
    Write-Host 'Fournissez -PfxPath, -Thumbprint, ou EVENTVIEWER_SIGN_PFX / EVENTVIEWER_SIGN_THUMBPRINT.'
    exit 0
}

$selfSigned = Test-IsSelfSigned $cert
if ($selfSigned -and -not $AllowSelfSigned) {
    Write-Warning "Certificat auto-signe detecte. Ajoutez -AllowSelfSigned pour un build de test local uniquement."
    throw "Refus de signer avec un certificat auto-signe sans -AllowSelfSigned."
}

# Timestamp servers often reject self-signed certs; skip unless explicitly forced.
$timestamp = $null
if (-not $SkipTimestamp -and -not $selfSigned) {
    $timestamp = $TimestampUrl
}

$signTool = Resolve-SignTool
if ($signTool) {
    Write-Host "signtool: $signTool" -ForegroundColor DarkGray
}
else {
    Write-Host "signtool introuvable - utilisation de Set-AuthenticodeSignature" -ForegroundColor DarkGray
}

$files = @()
foreach ($p in $Path) {
    if (Test-Path $p -PathType Container) {
        $files += Get-ChildItem $p -Filter "EventViewer.WinUI.exe" -Recurse -File -ErrorAction SilentlyContinue
        $files += Get-ChildItem $p -Recurse -Include *.msix, *.appx -File -ErrorAction SilentlyContinue
    }
    elseif (Test-Path $p) {
        $files += Get-Item $p
    }
    else {
        throw "Chemin introuvable: $p"
    }
}

$files = $files | Sort-Object FullName -Unique
if ($files.Count -eq 0) {
    throw "Aucun fichier a signer sous: $($Path -join ', ')"
}

Write-Host "Certificat: $($cert.Subject)" -ForegroundColor Cyan
Write-Host "Thumbprint: $($cert.Thumbprint)"
Write-Host "Fichiers: $($files.Count)"

foreach ($file in $files) {
    Write-Host "  Signing $($file.FullName)..."
    if ($signTool) {
        Sign-WithSignTool -SignTool $signTool -FilePath $file.FullName -Cert $cert -Timestamp $timestamp
    }
    else {
        Sign-WithPowerShell -FilePath $file.FullName -Cert $cert -Timestamp $timestamp
    }
}

Write-Host "Signature terminee." -ForegroundColor Green
foreach ($file in $files) {
    $sig = Get-AuthenticodeSignature $file.FullName
    Write-Host ("  {0} => {1}" -f $file.Name, $sig.Status)
}
