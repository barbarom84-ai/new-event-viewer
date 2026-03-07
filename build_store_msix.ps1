param(
  [string]$Configuration = "Release",
  [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host "== EventBeacon Tool - Build MSIX (Store) =="
Write-Host ""

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$wapproj = Join-Path $repoRoot "EventViewer.Package\EventViewer.Package.wapproj"

if (-not (Test-Path $wapproj)) {
  Write-Error "Packaging project introuvable: $wapproj"
}

# Try to locate VS MSBuild via vswhere
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
  Write-Host "vswhere introuvable. Installe Visual Studio 2022 + workloads UWP/.NET Desktop."
  Write-Host "Puis lance le build MSIX depuis Visual Studio (Packaging Project)."
  exit 1
}

$msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
  Write-Host "MSBuild.exe introuvable via vswhere."
  Write-Host "Installe Visual Studio 2022 + workloads UWP/.NET Desktop."
  exit 1
}

Write-Host "MSBuild: $msbuildPath"
Write-Host "Project: $wapproj"
Write-Host ""

& $msbuildPath $wapproj `
  /t:Build `
  /p:Configuration=$Configuration `
  /p:Platform=$Platform `
  /p:GenerateAppxPackageOnBuild=true `
  /p:AppxPackageSigningEnabled=false `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:StoreBuild=true

Write-Host ""
Write-Host "Build terminé. Le package est généralement dans:"
Write-Host "EventViewer.Package\AppPackages\"


