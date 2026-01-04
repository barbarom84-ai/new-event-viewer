Param(
    [string]$SourcePng = "obj\assets\eventviewericon.png",
    [string]$OutputDir = "assets",
    [string]$OutputIco = "app.ico"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectRoot = Resolve-Path (Join-Path $projectRoot "..")
Set-Location $projectRoot

$src = Resolve-Path $SourcePng
$destDir = Join-Path $projectRoot $OutputDir
$icoPath = Join-Path $destDir $OutputIco
$pngCopy = Join-Path $destDir "icon.png"

New-Item -ItemType Directory -Force -Path $destDir | Out-Null
Copy-Item -Path $src -Destination $pngCopy -Force

Add-Type -AssemblyName System.Drawing

$pngPath = (Resolve-Path $pngCopy).Path
$bitmap = [System.Drawing.Bitmap]::FromFile($pngPath)

# Crée une version 256x256 pour l'icône
$targetSize = 256
$resized = New-Object System.Drawing.Bitmap $bitmap, $targetSize, $targetSize

$icon = [System.Drawing.Icon]::FromHandle($resized.GetHicon())
$stream = New-Object System.IO.FileStream ($icoPath), ([System.IO.FileMode]::Create)
$icon.Save($stream)
$stream.Close()

$bitmap.Dispose()
$resized.Dispose()

Write-Host "Icon generated at $(Resolve-Path $icoPath)"

