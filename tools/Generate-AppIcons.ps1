# Generates WinUI Assets + app.ico from assets/app.ico
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "assets\app.ico"
$assets = Join-Path $root "EventViewer.WinUI\Assets"
$winuiIco = Join-Path $assets "app.ico"

if (-not (Test-Path $src)) {
    throw "Missing icon: $src"
}

Copy-Item $src $winuiIco -Force

function Save-Png([System.Drawing.Bitmap]$bmp, [string]$path) {
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Resize-Square([System.Drawing.Image]$img, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    return $bmp
}

function Save-Wide([System.Drawing.Image]$img, [int]$w, [int]$h, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 17, 19, 24))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $side = [Math]::Min($w, $h) * 0.62
    $x = ($w - $side) / 2
    $y = ($h - $side) / 2
    $g.DrawImage($img, [float]$x, [float]$y, [float]$side, [float]$side)
    $g.Dispose()
    Save-Png $bmp $path
    $bmp.Dispose()
}

$base = $null
$best = 0
foreach ($sz in @(256, 128, 64, 48, 32)) {
    try {
        $ico = New-Object System.Drawing.Icon($src, $sz, $sz)
        $candidate = $ico.ToBitmap()
        $ico.Dispose()
        if ($candidate.Width -gt $best) {
            if ($null -ne $base) { $base.Dispose() }
            $base = $candidate
            $best = $candidate.Width
        }
        else {
            $candidate.Dispose()
        }
    }
    catch {
        # ignore unsupported sizes
    }
}

if ($null -eq $base) {
    $ico = New-Object System.Drawing.Icon($src)
    $base = $ico.ToBitmap()
    $ico.Dispose()
}

Write-Host "Base source: $($base.Width)x$($base.Height)"

$squares = @{
    "StoreLogo.png" = 50
    "Square44x44Logo.scale-200.png" = 88
    "Square44x44Logo.targetsize-24_altform-unplated.png" = 24
    "Square150x150Logo.scale-200.png" = 300
    "LockScreenLogo.scale-200.png" = 48
    "Square44x44Logo.png" = 44
    "Square150x150Logo.png" = 150
}

foreach ($name in $squares.Keys) {
    $size = [int]$squares[$name]
    $bmp = Resize-Square $base $size
    $path = Join-Path $assets $name
    Save-Png $bmp $path
    $bmp.Dispose()
    Write-Host "Wrote $name ${size}x${size}"
}

Save-Wide $base 620 300 (Join-Path $assets "Wide310x150Logo.scale-200.png")
Write-Host "Wrote Wide310x150Logo.scale-200.png 620x300"
Save-Wide $base 310 150 (Join-Path $assets "Wide310x150Logo.png")
Write-Host "Wrote Wide310x150Logo.png 310x150"
Save-Wide $base 1240 600 (Join-Path $assets "SplashScreen.scale-200.png")
Write-Host "Wrote SplashScreen.scale-200.png 1240x600"
Save-Wide $base 620 300 (Join-Path $assets "SplashScreen.png")
Write-Host "Wrote SplashScreen.png 620x300"

$base.Dispose()
Write-Host "Done."
