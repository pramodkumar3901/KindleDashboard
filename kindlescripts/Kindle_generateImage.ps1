param(
    [string] $XValuesS,
    [string] $YValuesS,

    [switch] $DarkTheme,
    [switch] $ShowGrid = $false,
    [switch] $ShowMarkers,
    [switch] $GradientFill = $true,
    [switch] $Curve = $false,        # Spline / SplineArea
    [switch] $Polygon,              # Regular Area instead of SplineArea

    [switch] $ShowAxisLabels = $false,
    [switch] $ShowAxisLines = $false,
    [switch] $FillArea = $true
)

Add-Type -AssemblyName System.Windows.Forms.DataVisualization

# Convert date CSV → numeric date values (OADate)
[double[]] $XValues = $XValuesS -split ',' | ForEach-Object {
    ([DateTime]::ParseExact($_.Trim(), 'dd-MM-yyyy', $null)).ToOADate()
}
[double[]] $YValues = $YValuesS -split ',' | ForEach-Object { [double]$_ }

# ============================================================
#  IF NO INPUT → USE DUMMY DATA
# ============================================================
if (-not $XValues -or -not $YValues) {
    Write-Host "No data provided. Using dummy sample data..."
    $XValues = @(23, 22, 21, 20, 19, 18)
    $YValues = @(92280, 92280, 90920, 91120, 91560, 90680)
}

# ZIP + SORT
$zipped = for ($i=0; $i -lt $XValues.Count; $i++) {
    [PSCustomObject]@{ X = $XValues[$i]; Y = $YValues[$i] }
}

$sorted = $zipped | Sort-Object X
$XValues = $sorted.X
$YValues = $sorted.Y

$maxY = ($YValues | Measure-Object -Maximum).Maximum

# ============================================================
#  CHART CREATION
# ============================================================
$chart = New-Object System.Windows.Forms.DataVisualization.Charting.Chart
$chart.Width = 630
$chart.Height = 210

$chartArea = New-Object System.Windows.Forms.DataVisualization.Charting.ChartArea

# Remove margins
$chartArea.Position.Auto = $false
$chartArea.Position.X = 0
$chartArea.Position.Y = 0
$chartArea.Position.Width = 100
$chartArea.Position.Height = 100

$chart.ChartAreas.Add($chartArea)

# X-axis range
$chartArea.AxisX.Minimum = $XValues[0]
$chartArea.AxisX.Maximum = $XValues[-1]

# Y-Axis range with padding
$minY = ($YValues | Measure-Object -Minimum).Minimum
$maxY = ($YValues | Measure-Object -Maximum).Maximum
$padding = ($maxY - $minY) * 0.1

$chartArea.AxisY.Minimum = $minY - $padding
$chartArea.AxisY.Maximum = $maxY + $padding

$chartArea.AxisY.LabelStyle.Format = "#,0.0,K"

# ============================================================
#  THEME COLORS
# ============================================================
if ($DarkTheme) {
    $bgColor       = [System.Drawing.Color]::Black
    $axisColor     = [System.Drawing.Color]::White
    $gridColor     = [System.Drawing.Color]::FromArgb(40, 255, 255, 255)
    $lineColor     = [System.Drawing.Color]::White
    $gradTop       = [System.Drawing.Color]::FromArgb(120, 255, 255, 255)
    $gradBottom    = [System.Drawing.Color]::FromArgb(10, 0, 0, 0)
}
else {
    $bgColor       = [System.Drawing.Color]::White
    $axisColor     = [System.Drawing.Color]::Black
    $gridColor     = [System.Drawing.Color]::FromArgb(40, 0, 0, 0)
    $lineColor     = [System.Drawing.Color]::Black
    $gradTop       = [System.Drawing.Color]::FromArgb(120, 0, 0, 0)
    $gradBottom    = [System.Drawing.Color]::FromArgb(10, 255, 255, 255)
}

$chart.BackColor = $bgColor
$chartArea.BackColor = $bgColor

$chartArea.AxisX.LineColor = $axisColor
$chartArea.AxisY.LineColor = $axisColor
$chartArea.AxisX.LabelStyle.ForeColor = $axisColor
$chartArea.AxisY.LabelStyle.ForeColor = $axisColor

# ============================================================
#  AXIS LABELS ON/OFF
# ============================================================
$chartArea.AxisX.LabelStyle.Enabled = $ShowAxisLabels
$chartArea.AxisY.LabelStyle.Enabled = $ShowAxisLabels

# ============================================================
#  AXIS LINES ON/OFF
# ============================================================
if (-not $ShowAxisLines) {
    $chartArea.AxisX.LineWidth = 0
    $chartArea.AxisY.LineWidth = 0
}

# ============================================================
#  GRID LINES
# ============================================================
if ($ShowGrid) {
    $chartArea.AxisX.MajorGrid.Enabled = $true
    $chartArea.AxisY.MajorGrid.Enabled = $true
    $chartArea.AxisX.MajorGrid.LineColor = $gridColor
    $chartArea.AxisY.MajorGrid.LineColor = $gridColor
    $chartArea.AxisX.MajorGrid.LineDashStyle = "Dot"
    $chartArea.AxisY.MajorGrid.LineDashStyle = "Dot"
} else {
    $chartArea.AxisX.MajorGrid.Enabled = $false
    $chartArea.AxisY.MajorGrid.Enabled = $false
}

# ============================================================
#  AREA (Gradient / Flat / Off)
# ============================================================
$area = New-Object System.Windows.Forms.DataVisualization.Charting.Series

if ($Curve) {
    $area.ChartType = if ($Polygon) { "Area" } else { "SplineArea" }
} else {
    $area.ChartType = "Area"
}

if (-not $FillArea) {
    # Disable fill
    $area.Color = [System.Drawing.Color]::FromArgb(0,0,0,0)
    $area.BackGradientStyle = "None"
}
else {
    if ($GradientFill) {
        $area.BackGradientStyle = "TopBottom"
        $area.Color = $gradTop
        $area.BackSecondaryColor = $gradBottom
    } else {
        $area.BackGradientStyle = "None"
        $area.Color = [System.Drawing.Color]::FromArgb(80, $lineColor)
    }
}

# ============================================================
#  LINE SERIES
# ============================================================
$line = New-Object System.Windows.Forms.DataVisualization.Charting.Series

if ($Curve) {
    $line.ChartType = "Spline"
    $line["LineTension"] = "0.5"
} else {
    $line.ChartType = "Line"
}

$line.Color = $lineColor
$line.BorderWidth = 2

# Markers
if ($ShowMarkers) {
    $line.MarkerStyle = "Circle"
    $line.MarkerSize = 5
    $line.MarkerColor = $lineColor
    $line.MarkerBorderColor = $axisColor
    $line.MarkerBorderWidth = 1
} else {
    $line.MarkerStyle = "None"
}

# Add points
for ($i=0; $i -lt $YValues.Count; $i++) {
    $area.Points.AddXY($XValues[$i], $YValues[$i]) | Out-Null
    $line.Points.AddXY($XValues[$i], $YValues[$i]) | Out-Null
}

$chart.Series.Add($area) | Out-Null
$chart.Series.Add($line) | Out-Null

# ============================================================
#  TOP-LEFT LABEL
# ============================================================
$annotation = New-Object System.Windows.Forms.DataVisualization.Charting.TextAnnotation
$currentPrice = $YValues[-1]
$annotation.Text = "Gold: $currentPrice K"
$annotation.ForeColor = $axisColor
$annotation.Font = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$annotation.X = 20
$annotation.Y = 1
$annotation.Alignment = "TopLeft"
$chart.Annotations.Add($annotation)

# ============================================================
#  OUTPUT → BASE64 PNG
# ============================================================
$stream = New-Object System.IO.MemoryStream
$chart.SaveImage($stream, "Png")
$bytes = $stream.ToArray()
$stream.Close()

$base64 = [Convert]::ToBase64String($bytes)
Write-Host $base64
