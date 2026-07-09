<#
.SYNOPSIS
  Creates a local self-signed Authenticode certificate for USB test builds.

.DESCRIPTION
  For local QA only. Store / public releases require a real OV/EV code-signing
  certificate (or Partner Center Store signing for MSIX).

.EXAMPLE
  .\tools\New-DevCodeSigningCert.ps1
  .\build_winui_usb.ps1 -Sign -AllowSelfSigned -SignThumbprint <thumbprint>
#>
param(
    [string]$Subject = "CN=EventBeacon Dev Code Signing",
    [string]$ExportPfxPath = "artifacts\\EventBeacon-DevCodeSigning.pfx",
    [SecureString]$Password,
    [int]$ValidYears = 2
)

$ErrorActionPreference = "Stop"

if (-not $Password) {
    $Password = ConvertTo-SecureString "EventBeacon-Dev-Only-ChangeMe" -AsPlainText -Force
    Write-Warning "Mot de passe PFX par défaut utilisé (dev only). Changez-le pour un usage hors machine locale."
}

$notAfter = (Get-Date).AddYears($ValidYears)
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -HashAlgorithm SHA256 `
    -NotAfter $notAfter

$exportDir = Split-Path -Parent $ExportPfxPath
if (-not [string]::IsNullOrWhiteSpace($exportDir)) {
    New-Item -ItemType Directory -Force -Path $exportDir | Out-Null
}

Export-PfxCertificate -Cert $cert -FilePath $ExportPfxPath -Password $Password | Out-Null

# Also export CER for Trusted Publishers (optional install)
$cerPath = [IO.Path]::ChangeExtension($ExportPfxPath, ".cer")
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host ""
Write-Host "Certificat de développement créé." -ForegroundColor Green
Write-Host "  Subject     : $($cert.Subject)"
Write-Host "  Thumbprint  : $($cert.Thumbprint)"
Write-Host "  Valid until : $($cert.NotAfter)"
Write-Host "  PFX         : $ExportPfxPath"
Write-Host "  CER         : $cerPath"
Write-Host ""
Write-Host "Pour signer un build USB de test :"
Write-Host "  `$env:EVENTVIEWER_SIGN_THUMBPRINT = '$($cert.Thumbprint)'"
Write-Host "  .\build_winui_usb.ps1 -Sign -AllowSelfSigned"
Write-Host ""
Write-Host "Pour faire confiance à la signature sur CETTE machine (optionnel) :"
Write-Host "  Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\\CurrentUser\\TrustedPublisher"
Write-Host ""
Write-Host "NE PAS utiliser ce certificat pour une distribution publique / Store." -ForegroundColor Yellow
