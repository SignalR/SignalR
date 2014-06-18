param
(
  [Parameter(Mandatory=$true, Position=0, HelpMessage="CSV input file")]
  [string]$csvFile,
  
  [Parameter(Mandatory=$true, Position=1, HelpMessage="XLSX output file")]
  [string]$xlsxFile,
  
  [Parameter(Mandatory=$false, Position=2, HelpMessage="Crank run name")]
  [string]$title = "Crank Connection Test",
  
  [Parameter(Mandatory=$false, Position=3, HelpMessage="Column selection range")]
  [string]$colRange = "B1:G1",
  
  [Parameter(Mandatory=$false, Position=4, HelpMessage="Column selection range")]
  [string]$colCount = 5
)

function OpenExcel
{
    [void][reflection.assembly]::loadwithpartialname("microsoft.office.interop.excel")
    $excel = new-object -com excel.application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $excel
}

function OpenCsv ( $excel, $path )
{
    $origin = [int][Microsoft.Office.Interop.Excel.XlPlatform]::xlWindows
    $startRow = 1
    $dataType = [int][Microsoft.Office.Interop.Excel.XlTextparsingType]::xlDelimited
    $textQualifier = [int][Microsoft.Office.Interop.Excel.XlTextQualifier]::xlTextQualifierDoubleQuote
    $consecDelimiter = $false
    $tab = $false
    $semicolon = $false
    $comma = $true
    $space = $false
    $other = $false
    $otherChar = $false
    
    $excel.Workbooks.OpenText($csvFile, $origin, $startRow, $dataType, $textQualifier, $consecDelimiter, $tab, $semicolon, $comma, $space, $other, $otherChar)
}

$excel = OpenExcel

OpenCsv $excel $csvFile

$worksheet = $excel.workbooks.Item(1).worksheets.Item(1)
$worksheet.Name = $title
#$worksheet.UsedRange.entirecolumn.Autofit() | out-null
$worksheet.Activate()

$chartRange = $worksheet.Range($colRange).EntireColumn
$chart = $worksheet.shapes.addchart().chart
$chart.HasTitle = $true
$chart.ChartTitle.Text = $title
$chart.chartType = [Microsoft.Office.Interop.Excel.XlChartType]::xlLine
$chart.SetSourceData($chartRange)
$chart.ChartArea.Width,$chart.ChartArea.Height = 450,300
$chart.ChartArea.Left,$chart.ChartArea.Top = 600,50
$chart.Legend.Position = [Microsoft.Office.Interop.Excel.XlLegendPosition]::xlLegendPositionBottom

for ($i=1; $i -le $colCount; $i++)
{
    $series = $chart.SeriesCollection($i)
    if ($series.Name -like "*byte*")
    {
        $series.AxisGroup = [Microsoft.Office.Interop.Excel.XlAxisGroup]::xlSecondary
    }
}

$excel.workbooks.Item(1).saveas($xlsxFile, [int][Microsoft.Office.Interop.Excel.XlFileFormat]::xlOpenXMLWorkbook)
$excel.quit()

Remove-Variable -Name excel
[gc]::collect() 
[gc]::WaitForPendingFinalizers()