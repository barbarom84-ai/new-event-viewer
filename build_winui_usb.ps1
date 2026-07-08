param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Output = "bin\\WinUI_USB"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing EventViewer.WinUI (unpackaged USB)..." -ForegroundColor Cyan
dotnet publish "EventViewer.WinUI\\EventViewer.WinUI.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:WindowsPackageType=None `
    --self-contained true `
    -r win-x64 `
    -o $Output

Write-Host "Done: $Output\\EventViewer.WinUI.exe" -ForegroundColor Green
Write-Host "Run as Administrator to read Windows event logs."
