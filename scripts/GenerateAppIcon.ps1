# Generates multi-size AppIcon.ico for PortCheck
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-PortCheckBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))

    $pad = [Math]::Max(1, [int]($size * 0.08))
    $rect = New-Object System.Drawing.Rectangle $pad, $pad, ($size - 2 * $pad), ($size - 2 * $pad)

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 72, 152, 255),
        [System.Drawing.Color]::FromArgb(255, 38, 98, 210),
        45)
    $g.FillEllipse($bg, $rect)
    $bg.Dispose()

    $rimWidth = [Math]::Max(1.0, $size / 32.0)
    $rim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 255, 255, 255)), $rimWidth
    $g.DrawEllipse($rim, $rect)
    $rim.Dispose()

    $white = [System.Drawing.Brushes]::White
    $cx = $size / 2.0
    $cy = $size / 2.0
    $r = $size * 0.22

    $g.DrawEllipse($white, ($cx - $r), ($cy - $r - $size * 0.04), (2 * $r), (2 * $r))
    $handleWidth = [Math]::Max(1.5, $size / 12.0)
    $handle = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $handleWidth
    $g.DrawLine($handle, ($cx + $r * 0.65), ($cy + $r * 0.55), ($cx + $r * 1.35), ($cy + $r * 1.25))
    $handle.Dispose()

    $dot = [Math]::Max(2, $size / 10)
    $g.FillEllipse($white, ($cx - $dot / 2), ($cy - $dot / 2 - $size * 0.04), $dot, $dot)

    $g.Dispose()
    return $bmp
}

function Save-MultiSizeIcon([string]$path, [int[]]$sizes) {
    $images = @()
    foreach ($s in $sizes) { $images += ,(New-PortCheckBitmap $s) }

    $ms = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter $ms
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$images.Count)
    $offset = 6 + (16 * $images.Count)
    $pngData = @()
    foreach ($img in $images) {
        $pngMs = New-Object System.IO.MemoryStream
        $img.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngData += ,$pngMs.ToArray()
        $pngMs.Dispose()
    }
    for ($i = 0; $i -lt $images.Count; $i++) {
        if ($images[$i].Width -ge 256) { $w = [byte]0; $h = [byte]0 }
        else { $w = [byte]$images[$i].Width; $h = [byte]$images[$i].Height }
        $writer.Write($w)
        $writer.Write($h)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$pngData[$i].Length)
        $writer.Write([uint32]$offset)
        $offset += $pngData[$i].Length
    }
    foreach ($d in $pngData) { $writer.Write($d) }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes($path, $ms.ToArray())
    $writer.Dispose()
    $ms.Dispose()
    foreach ($img in $images) { $img.Dispose() }
}

$outDir = Join-Path $PSScriptRoot '..\src\PortCheck\Assets'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$icoPath = Join-Path $outDir 'AppIcon.ico'
Save-MultiSizeIcon $icoPath @(16, 32, 48, 256)
Write-Host "Wrote $icoPath"
