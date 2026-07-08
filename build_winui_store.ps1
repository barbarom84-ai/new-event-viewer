param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$Output = "bin\\WinUI_Store"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing EventViewer.WinUI (Store flag, unpackaged self-contained)..." -ForegroundColor Cyan
dotnet publish "EventViewer.WinUI\\EventViewer.WinUI.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:StoreBuild=true `
    -p:WindowsPackageType=None `
    --self-contained true `
    -r win-x64 `
    -o $Output

Write-Host "Done: $Output\\EventViewer.WinUI.exe" -ForegroundColor Green
Write-Host "Store behaviors: no maintenance UI, AppData/Documents paths."
Write-Host "For a signed MSIX, open the project in Visual Studio and use Package and Publish (single-project MSIX)."
