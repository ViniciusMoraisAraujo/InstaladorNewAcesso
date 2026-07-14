# generate-icon.ps1
# Gera um ícone 32x32 32bpp para o Instalador NewAcesso
# Usa System.Drawing para criar o bitmap e serializa manualmente como .ico

Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path $PSScriptRoot "..\src\app.ico"

# ── Criar bitmap ──
$bmp = New-Object System.Drawing.Bitmap(32, 32)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Background: dark navy
$g.Clear([System.Drawing.Color]::FromArgb(255, 12, 12, 28))

# ── Círculo externo (gear-like) ──
$outerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 140, 200))
$g.FillEllipse($outerBrush, 3, 3, 26, 26)

# ── Círculo interno escuro ──
$innerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 12, 12, 28))
$g.FillEllipse($innerBrush, 7, 7, 18, 18)

# ── Detalhe: pequenos círculos ao redor (simulando engrenagem) ──
$dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 200, 255))
$dotPositions = @(
    @(16, 4), @(24, 8), @(28, 16), @(24, 24),
    @(16, 28), @(8, 24), @(4, 16), @(8, 8)
)
foreach ($pos in $dotPositions) {
    $g.FillEllipse($dotBrush, $pos[0] - 2, $pos[1] - 2, 4, 4)
}

# ── Seta triangular no centro (play/install) ──
$playBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 200, 255))
$points = @(
    [System.Drawing.Point]::new(12, 10),
    [System.Drawing.Point]::new(12, 22),
    [System.Drawing.Point]::new(22, 16)
)
$g.FillPolygon($playBrush, $points)

$dotBrush.Dispose()
$outerBrush.Dispose()
$innerBrush.Dispose()
$playBrush.Dispose()
$g.Dispose()

# ── Obter pixels BGRA ──
$rect = New-Object System.Drawing.Rectangle(0, 0, 32, 32)
$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$stride = [Math]::Abs($data.Stride)
$pixelSize = 4
$rowBytes = $stride  # já está alinhado para 32bpp (32*4=128)

$pixels = New-Object Byte[] ($rowBytes * 32)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
$bmp.UnlockBits($data)
$bmp.Dispose()

# ── Construir .ico manualmente ──
$stream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($stream)

# ICONDIR (6 bytes)
$writer.Write([UInt16]0)          # idReserved
$writer.Write([UInt16]1)          # idType (1 = icon)
$writer.Write([UInt16]1)          # idCount (1 image)

# ICONDIRENTRY (16 bytes)
$writer.Write([Byte]32)           # bWidth
$writer.Write([Byte]32)           # bHeight
$writer.Write([Byte]0)            # bColorCount
$writer.Write([Byte]0)            # bReserved
$writer.Write([UInt16]1)          # wPlanes
$writer.Write([UInt16]32)         # wBitCount

# Image data size = BITMAPINFOHEADER(40) + pixels(4096) + AND mask(128)
$imageSize = 40 + 4096 + 128
$writer.Write([UInt32]$imageSize) # dwBytesInRes
$writer.Write([UInt32]22)         # dwImageOffset (6 + 16)

# BITMAPINFOHEADER (40 bytes)
$writer.Write([UInt32]40)         # biSize
$writer.Write([Int32]32)          # biWidth
$writer.Write([Int32]64)          # biHeight (32 XOR + 32 AND)
$writer.Write([UInt16]1)          # biPlanes
$writer.Write([UInt16]32)         # biBitCount
$writer.Write([UInt32]0)          # biCompression (BI_RGB)
$writer.Write([UInt32]0)          # biSizeImage
$writer.Write([Int32]0)           # biXPelsPerMeter
$writer.Write([Int32]0)           # biYPelsPerMeter
$writer.Write([UInt32]0)          # biClrUsed
$writer.Write([UInt32]0)          # biClrImportant

# Pixel data (BGRA, bottom-to-top rows)
for ($y = 31; $y -ge 0; $y--) {
    $rowStart = $y * $rowBytes
    $writer.Write($pixels, $rowStart, $rowBytes)
}

# AND mask (1-bit per pixel, each row padded to 4 bytes) - all zeros since we use alpha channel
$andRowSize = 4  # 32 bits / 8 = 4 bytes, already aligned
for ($y = 0; $y -lt 32; $y++) {
    $writer.Write([UInt32]0)
}

$writer.Flush()

# ── Salvar arquivo ──
[System.IO.File]::WriteAllBytes($outputPath, $stream.ToArray())

$writer.Dispose()
$stream.Dispose()

Write-Host "Ícone gerado: $outputPath"
Write-Host "Tamanho: $((Get-Item $outputPath).Length) bytes"
