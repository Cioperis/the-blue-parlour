# Original geometric cat-and-rose artwork. Windows/System.Drawing; no external assets.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(256,256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
function Brush([string]$hex) { [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($hex)) }
$blue = Brush '#193D5C'; $gold = Brush '#C9AC76'; $cream = Brush '#F2DFC0'; $rose = Brush '#94364B'; $pink = Brush '#D8909B'
$ink = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#193D5C'),7)
try {
 $graphics.Clear([Drawing.Color]::Transparent)
 $graphics.FillEllipse($gold,4,4,248,248)
 $graphics.FillEllipse($blue,12,12,232,232)
 $graphics.FillPolygon($cream,[Drawing.Point[]]@([Drawing.Point]::new(56,130),[Drawing.Point]::new(51,60),[Drawing.Point]::new(105,99)))
 $graphics.FillPolygon($cream,[Drawing.Point[]]@([Drawing.Point]::new(149,99),[Drawing.Point]::new(201,60),[Drawing.Point]::new(198,138)))
 $graphics.FillEllipse($cream,48,91,159,116)
 $graphics.DrawArc($ink,74,120,32,25,5,165)
 $graphics.DrawArc($ink,146,120,32,25,10,165)
 $graphics.DrawLines($ink,[Drawing.Point[]]@([Drawing.Point]::new(115,164),[Drawing.Point]::new(126,172),[Drawing.Point]::new(138,164)))
 $graphics.FillEllipse($rose,161,170,60,54)
 $graphics.FillEllipse($pink,176,179,30,27)
 $graphics.FillEllipse($rose,183,184,20,19)
 $target = Join-Path $PSScriptRoot '../src/BlueParlour.Desktop/Assets'
 New-Item -ItemType Directory -Force $target | Out-Null
 $bitmap.Save((Join-Path $target 'Parlour.png'),[Drawing.Imaging.ImageFormat]::Png)
 $sizes = @(16,24,32,48,64,128,256)
 $images = @()
 foreach ($size in $sizes) {
  $small = [Drawing.Bitmap]::new($size,$size)
  $g = [Drawing.Graphics]::FromImage($small)
  $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.DrawImage($bitmap,0,0,$size,$size)
  $memory = [IO.MemoryStream]::new()
  $small.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
  $images += ,$memory.ToArray()
  $memory.Dispose(); $g.Dispose(); $small.Dispose()
 }
 $file = [IO.File]::Create((Join-Path $target 'Parlour.ico'))
 $writer = [IO.BinaryWriter]::new($file)
 try {
  $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
  $offset = 6 + 16 * $sizes.Count
  for ($i=0; $i -lt $sizes.Count; $i++) {
   $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
   $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
   $writer.Write([byte]0); $writer.Write([byte]0)
   $writer.Write([uint16]1); $writer.Write([uint16]32)
   $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
   $offset += $images[$i].Length
  }
  foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
 } finally { $writer.Dispose(); $file.Dispose() }
} finally {
 $graphics.Dispose(); $bitmap.Dispose(); $ink.Dispose()
 foreach ($brush in @($blue,$gold,$cream,$rose,$pink)) { $brush.Dispose() }
}
