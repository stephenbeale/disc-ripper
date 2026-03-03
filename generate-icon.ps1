# Generates disc-ripper.ico in DiscRipper/ using System.Drawing.
# Run once from the repo root: pwsh -File generate-icon.ps1
# The .ico is committed to source control; this script is not needed again unless you want to regenerate.

Add-Type -AssemblyName System.Drawing

function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $p = [int]([Math]::Max(1, $Size * 0.05))   # outer padding
    $d = $Size - 2 * $p                         # disc diameter

    # ── Disc body (dark slate) ──────────────────────────────────────────────
    $body = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 28, 28, 48))
    $g.FillEllipse($body, $p, $p, $d, $d)
    $body.Dispose()

    # ── Disc edge (subtle purple-grey rim) ──────────────────────────────────
    $rimW = [float]([Math]::Max(1, $Size * 0.04))
    $rim  = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200, 110, 100, 160), $rimW)
    $g.DrawEllipse($rim, $p + 1, $p + 1, $d - 2, $d - 2)
    $rim.Dispose()

    # ── Data ring (slightly lighter inner band) ─────────────────────────────
    $rp = [int]($Size * 0.20)
    $rd = $Size - 2 * $rp
    $ring = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 42, 42, 68))
    $g.FillEllipse($ring, $rp, $rp, $rd, $rd)
    $ring.Dispose()

    # ── Centre spindle hole ─────────────────────────────────────────────────
    $hp = [int]($Size * 0.42)
    $hd = $Size - 2 * $hp
    if ($hd -ge 1) {
        $hole = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 14, 14, 24))
        $g.FillEllipse($hole, $hp, $hp, $hd, $hd)
        $hole.Dispose()
    }

    # ── Shine highlight (top-left arc, larger sizes only) ──────────────────
    if ($Size -ge 32) {
        $sx = $p + [int]($d * 0.12)
        $sy = $p + [int]($d * 0.06)
        $sw = [int]($d * 0.38)
        $sh = [int]($d * 0.22)
        $shine = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40, 190, 180, 230))
        $shinePath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $shinePath.AddEllipse($sx, $sy, $sw, $sh)
        $g.FillPath($shine, $shinePath)
        $shine.Dispose()
        $shinePath.Dispose()
    }

    $g.Dispose()
    return $bmp
}

function ConvertTo-IcoBytes {
    param([int[]]$Sizes)

    $images = @()
    foreach ($s in $Sizes) {
        $bmp = New-IconBitmap -Size $s
        $ms  = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $images += [PSCustomObject]@{ Size = $s; Bytes = $ms.ToArray() }
        $ms.Dispose()
        $bmp.Dispose()
    }

    $out = New-Object System.IO.MemoryStream
    $bw  = New-Object System.IO.BinaryWriter($out)

    # ICO header
    $bw.Write([uint16]0)                  # Reserved
    $bw.Write([uint16]1)                  # Type = ICO
    $bw.Write([uint16]$images.Count)      # Image count

    # Directory entries
    $dataOffset = 6 + ($images.Count * 16)
    foreach ($img in $images) {
        $w = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
        $h = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)                # Colour count (0 = truecolour)
        $bw.Write([byte]0)                # Reserved
        $bw.Write([uint16]1)              # Planes
        $bw.Write([uint16]32)             # Bit depth
        $bw.Write([uint32]$img.Bytes.Length)
        $bw.Write([uint32]$dataOffset)
        $dataOffset += $img.Bytes.Length
    }

    foreach ($img in $images) { $bw.Write($img.Bytes) }

    $bw.Flush()
    $bytes = $out.ToArray()
    $out.Dispose()
    $bw.Dispose()
    return $bytes
}

$dest  = Join-Path $PSScriptRoot "DiscRipper\disc-ripper.ico"
$bytes = ConvertTo-IcoBytes -Sizes @(16, 32, 48, 256)
[System.IO.File]::WriteAllBytes($dest, $bytes)
Write-Host "Icon generated: $dest ($([Math]::Round($bytes.Length / 1KB, 1)) KB, 4 sizes: 16 32 48 256)"
