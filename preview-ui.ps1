# SolidModelBrowser — Live XAML UI Previewer
# Renders MainWindow.xaml instantly using native Windows WPF (No IDE or compilation required)

Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseDir = Join-Path $scriptDir "SolidModelBrowser"

function Show-Preview {
    try {
        $colorsPath = Join-Path $baseDir "Styles\Colors.xaml"
        $stylesPath = Join-Path $baseDir "Styles\Styles.xaml"
        $mainPath = Join-Path $baseDir "MainWindow.xaml"
        $filePanelPath = Join-Path $baseDir "Controls\FileNavigationPanel.xaml"

        if (-not (Test-Path $mainPath)) {
            Write-Host "Error: Could not find $mainPath" -ForegroundColor Red
            return
        }

        # 1. Read source XAML files
        $colorsRaw = Get-Content $colorsPath -Raw
        $stylesRaw = Get-Content $stylesPath -Raw
        $mainRaw = Get-Content $mainPath -Raw
        $filePanelRaw = Get-Content $filePanelPath -Raw

        # 2. Sanitize controls for raw XamlReader
        $fpClean = $filePanelRaw -replace 'x:Class="[^"]*"', ''
        $fpClean = $fpClean -replace 'xmlns:local="[^"]*"', ''
        $fpClean = $fpClean -replace '<Style[^>]*BasedOn="\{StaticResource \{x:Type ToggleButton\}\}"[^>]*/>', ''
        $fpClean = $fpClean -replace 'UserControl', 'DockPanel'

        $mainClean = $mainRaw -replace 'x:Class="[^"]*"', ''
        $mainClean = $mainClean -replace 'MouseDown="[^"]*"', ''
        $mainClean = $mainClean -replace '<local:Scene[^/]*/>', '<Grid Background="#141416"><TextBlock Text="[ 3D Viewport Scene ]" Foreground="#444" VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="18" FontWeight="SemiBold"/></Grid>'
        $mainClean = [System.Text.RegularExpressions.Regex]::Replace($mainClean, '<local:TextEncodedImage[\s\S]*?</local:TextEncodedImage>', '<Border Width="14" Height="14" Background="#666" CornerRadius="2"/>')
        $mainClean = [System.Text.RegularExpressions.Regex]::Replace($mainClean, '<local:FileNavigationPanel[\s\S]*?</local:FileNavigationPanel>', ('<Border BorderBrush="#333" BorderThickness="0,0,1,0">' + $fpClean + '</Border>'))

        # 3. Setup Application & Resources
        if (-not [System.Windows.Application]::Current) {
            $app = New-Object System.Windows.Application
        } else {
            $app = [System.Windows.Application]::Current
        }

        $colorsDict = [System.Windows.Markup.XamlReader]::Parse($colorsRaw)
        $stylesDict = [System.Windows.Markup.XamlReader]::Parse($stylesRaw)

        $app.Resources.MergedDictionaries.Clear()
        $app.Resources.MergedDictionaries.Add($colorsDict)
        $app.Resources.MergedDictionaries.Add($stylesDict)

        # 4. Parse & Show Window
        $window = [System.Windows.Markup.XamlReader]::Parse($mainClean)
        
        # Wire close button if present
        $closeBtn = $window.FindName("ButtonClose")
        if ($closeBtn) {
            $closeBtn.Add_Click({ $window.Close() })
        }
        
        Write-Host "Displaying live WPF MainWindow.xaml preview..." -ForegroundColor Green
        Write-Host "Close the preview window to return." -ForegroundColor Gray
        $null = $window.ShowDialog()
    }
    catch {
        Write-Host "XAML Preview Error: $_" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
    }
}

Show-Preview
