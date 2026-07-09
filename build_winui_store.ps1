param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Output = "bin\\WinUI_Store",
    [switch]$Sign,
    [switch]$AllowSelfSigned,
    [string]$SignPfxPath = $env:EVENTVIEWER_SIGN_PFX,
    [string]$SignPfxPassword = $env:EVENTVIEWER_SIGN_PFX_PASSWORD,
    [string]$SignThumbprint = $env:EVENTVIEWER_SIGN_THUMBPRINT
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

Write-Host "Publishing EventViewer.WinUI (Store flag, unpackaged self-contained)..." -ForegroundColor Cyan
dotnet publish "EventViewer.WinUI\\EventViewer.WinUI.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:StoreBuild=true `
    -p:WindowsPackageType=None `
    -p:PublishTrimmed=false `
    --self-contained true `
    -r win-x64 `
    -o $Output

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

$exe = Join-Path $Output "EventViewer.WinUI.exe"
Write-Host "Done: $exe" -ForegroundColor Green
Write-Host "Store behaviors: no maintenance UI, AppData/Documents paths."

$shouldSign = $Sign -or $SignPfxPath -or $SignThumbprint -or $env:EVENTVIEWER_SIGN_PFX -or $env:EVENTVIEWER_SIGN_THUMBPRINT
if ($shouldSign) {
    $signScript = Join-Path $repoRoot "tools\\Sign-Authenticode.ps1"
    $signArgs = @{ Path = @($exe) }
    if ($AllowSelfSigned) { $signArgs.AllowSelfSigned = $true }
    if ($SignPfxPath) { $signArgs.PfxPath = $SignPfxPath }
    if ($SignPfxPassword) { $signArgs.PfxPasswordPlain = $SignPfxPassword }
    if ($SignThumbprint) { $signArgs.Thumbprint = $SignThumbprint }

    Write-Host "Signing Store-flagged unpackaged build (sideload QA only)..." -ForegroundColor Cyan
    & $signScript @signArgs
}

Write-Host "For a signed MSIX, open the project in Visual Studio and use Package and Publish (Partner Center / Store signing)."
