param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Output = "bin\\WinUI_USB",
    [switch]$Sign,
    [switch]$AllowSelfSigned,
    [string]$SignPfxPath = $env:EVENTVIEWER_SIGN_PFX,
    [string]$SignPfxPassword = $env:EVENTVIEWER_SIGN_PFX_PASSWORD,
    [string]$SignThumbprint = $env:EVENTVIEWER_SIGN_THUMBPRINT
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

Write-Host "Publishing EventViewer.WinUI (unpackaged USB)..." -ForegroundColor Cyan
dotnet publish "EventViewer.WinUI\\EventViewer.WinUI.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:WindowsPackageType=None `
    -p:PublishTrimmed=false `
    --self-contained true `
    -r win-x64 `
    -o $Output

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

$exe = Join-Path $Output "EventViewer.WinUI.exe"
if (-not (Test-Path $exe)) {
    throw "Publish output missing: $exe"
}

Write-Host "Done: $exe" -ForegroundColor Green

$shouldSign = $Sign -or $SignPfxPath -or $SignThumbprint -or $env:EVENTVIEWER_SIGN_PFX -or $env:EVENTVIEWER_SIGN_THUMBPRINT
if ($shouldSign) {
    $signScript = Join-Path $repoRoot "tools\\Sign-Authenticode.ps1"
    $signArgs = @{
        Path = @($exe)
    }
    if ($AllowSelfSigned) { $signArgs.AllowSelfSigned = $true }
    if ($SignPfxPath) { $signArgs.PfxPath = $SignPfxPath }
    if ($SignPfxPassword) { $signArgs.PfxPasswordPlain = $SignPfxPassword }
    if ($SignThumbprint) { $signArgs.Thumbprint = $SignThumbprint }

    Write-Host "Signing USB build..." -ForegroundColor Cyan
    & $signScript @signArgs
}
else {
    Write-Host "Unsigned build (ajouter -Sign ou EVENTVIEWER_SIGN_PFX / EVENTVIEWER_SIGN_THUMBPRINT)." -ForegroundColor DarkYellow
}

Write-Host "Run as Administrator to read Windows event logs."
