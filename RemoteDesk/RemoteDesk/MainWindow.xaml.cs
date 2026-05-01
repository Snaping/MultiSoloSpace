using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RemoteDesk;

public partial class MainWindow : Window
{
    private readonly ChromeRemoteClient _chromeClient;
    private readonly McpServerService _mcpService;
    private bool _isConnected = false;

    public MainWindow()
    {
        InitializeComponent();
        
        _chromeClient = new ChromeRemoteClient();
        _mcpService = new McpServerService();
        
        _mcpService.LogMessage += (sender, message) => 
        {
            Dispatcher.Invoke(() => Log(message));
        };
        
        _chromeClient.ConsoleMessageReceived += (sender, message) =>
        {
            Dispatcher.Invoke(() => Log($"[Chrome Console] {message}"));
        };
        
        Log("RemoteDesk initialized. Ready to control Chrome.");
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        txtLog.ScrollToEnd();
    }

    private async void BtnStartChrome_Click(object sender, RoutedEventArgs e)
    {
        Log("Starting Chrome...");
        btnStartChrome.IsEnabled = false;
        
        try
        {
            var success = await _chromeClient.StartChromeAsync(txtUrl.Text);
            if (success)
            {
                Log("Chrome started successfully!");
                
                await Task.Delay(2000);
                await RefreshPagesList();
            }
            else
            {
                Log("Failed to start Chrome. Make sure Chrome is installed.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
        finally
        {
            btnStartChrome.IsEnabled = true;
        }
    }

    private async Task RefreshPagesList()
    {
        try
        {
            var pages = await _chromeClient.ListPagesAsync();
            lbPages.Items.Clear();
            
            for (var i = 0; i < pages.Count; i++)
            {
                lbPages.Items.Add($"[{i}] {pages[i].Title} - {pages[i].Url}");
            }
            
            if (pages.Count > 0)
            {
                lbPages.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to list pages: {ex.Message}");
        }
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_isConnected)
        {
            Log("Already connected.");
            return;
        }

        var pageIndex = lbPages.SelectedIndex >= 0 ? lbPages.SelectedIndex : 0;
        Log($"Connecting to page {pageIndex}...");
        
        try
        {
            var success = await _chromeClient.ConnectToPageAsync(pageIndex);
            if (success)
            {
                _isConnected = true;
                Log($"Connected! Current URL: {_chromeClient.CurrentPageUrl}");
                btnConnect.Content = "Connected";
                btnConnect.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                Log("Failed to connect.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void LbPages_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (lbPages.SelectedIndex >= 0 && _isConnected)
        {
            var pageIndex = lbPages.SelectedIndex;
            Log($"Switching to page {pageIndex}...");
            
            try
            {
                var success = await _chromeClient.ConnectToPageAsync(pageIndex);
                if (success)
                {
                    Log($"Switched to page {pageIndex}: {_chromeClient.CurrentPageUrl}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }
    }

    private async void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        Log("Navigating back...");
        try
        {
            await _chromeClient.EvaluateScriptAsync("history.back()");
            await Task.Delay(500);
            Log("Navigated back.");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnGo_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        var url = txtUrl.Text;
        if (string.IsNullOrWhiteSpace(url))
        {
            Log("Please enter a URL.");
            return;
        }

        Log($"Navigating to: {url}");
        try
        {
            var success = await _chromeClient.NavigateAsync(url);
            if (success)
            {
                await Task.Delay(1000);
                Log($"Navigated to: {url}");
            }
            else
            {
                Log("Failed to navigate.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private bool EnsureConnected()
    {
        if (!_isConnected)
        {
            Log("Not connected to Chrome. Please start Chrome and connect first.");
            return false;
        }
        return true;
    }

    private async void BtnClick_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        if (!double.TryParse(txtClickX.Text, out var x) || 
            !double.TryParse(txtClickY.Text, out var y))
        {
            Log("Invalid coordinates.");
            return;
        }

        Log($"Clicking at ({x}, {y})...");
        try
        {
            await _chromeClient.ClickAsync(x, y);
            Log($"Clicked at ({x}, {y})");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnRightClick_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        if (!double.TryParse(txtClickX.Text, out var x) || 
            !double.TryParse(txtClickY.Text, out var y))
        {
            Log("Invalid coordinates.");
            return;
        }

        Log($"Right-clicking at ({x}, {y})...");
        try
        {
            await _chromeClient.RightClickAsync(x, y);
            Log($"Right-clicked at ({x}, {y})");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnDoubleClick_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        if (!double.TryParse(txtClickX.Text, out var x) || 
            !double.TryParse(txtClickY.Text, out var y))
        {
            Log("Invalid coordinates.");
            return;
        }

        Log($"Double-clicking at ({x}, {y})...");
        try
        {
            await _chromeClient.DoubleClickAsync(x, y);
            Log($"Double-clicked at ({x}, {y})");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnHover_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        if (!double.TryParse(txtClickX.Text, out var x) || 
            !double.TryParse(txtClickY.Text, out var y))
        {
            Log("Invalid coordinates.");
            return;
        }

        Log($"Hovering at ({x}, {y})...");
        try
        {
            await _chromeClient.HoverAsync(x, y);
            Log($"Hovered at ({x}, {y})");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnDrag_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        if (!double.TryParse(txtFromX.Text, out var fromX) || 
            !double.TryParse(txtFromY.Text, out var fromY) ||
            !double.TryParse(txtToX.Text, out var toX) || 
            !double.TryParse(txtToY.Text, out var toY))
        {
            Log("Invalid coordinates.");
            return;
        }

        Log($"Dragging from ({fromX}, {fromY}) to ({toX}, {toY})...");
        try
        {
            await _chromeClient.DragAsync(fromX, fromY, toX, toY);
            Log($"Dragged from ({fromX}, {fromY}) to ({toX}, {toY})");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnType_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        var text = txtInput.Text;
        if (string.IsNullOrEmpty(text))
        {
            Log("Please enter text to type.");
            return;
        }

        Log($"Typing: {text}");
        try
        {
            await _chromeClient.TypeTextAsync(text);
            Log($"Typed: {text}");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnEval_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        var script = txtScript.Text;
        if (string.IsNullOrWhiteSpace(script))
        {
            Log("Please enter a script.");
            return;
        }

        Log($"Evaluating script: {script}");
        try
        {
            var result = await _chromeClient.EvaluateScriptAsync(script);
            Log($"Result: {result}");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        Log("Taking screenshot...");
        try
        {
            var bytes = await _chromeClient.TakeScreenshotAsync();
            if (bytes != null)
            {
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                
                imgScreenshot.Source = bitmap;
                
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = Path.Combine(desktopPath, fileName);
                
                using var fs = new FileStream(filePath, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fs);
                
                Log($"Screenshot saved to: {filePath}");
            }
            else
            {
                Log("Failed to take screenshot.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnSnapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureConnected()) return;
        
        Log("Taking text snapshot...");
        try
        {
            var snapshot = await _chromeClient.TakeSnapshotAsync();
            if (!string.IsNullOrEmpty(snapshot))
            {
                Log("Snapshot:");
                Log(snapshot);
            }
            else
            {
                Log("Failed to take snapshot.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnStartMcp_Click(object sender, RoutedEventArgs e)
    {
        if (_mcpService.IsRunning)
        {
            Log("MCP Server is already running.");
            return;
        }

        Log("Starting MCP Server...");
        try
        {
            await _mcpService.StartAsync("remotedesk-mcp");
            txtMcpStatus.Text = "MCP Server: Running";
            txtMcpStatus.Foreground = System.Windows.Media.Brushes.Green;
            Log("MCP Server started. Available tools:");
            Log("- start_chrome, connect_to_page, list_pages");
            Log("- navigate, take_screenshot, take_snapshot");
            Log("- click, right_click, drag, type, hover");
            Log("- evaluate_script, new_page, close_page, get_status");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    private async void BtnStopMcp_Click(object sender, RoutedEventArgs e)
    {
        if (!_mcpService.IsRunning)
        {
            Log("MCP Server is not running.");
            return;
        }

        Log("Stopping MCP Server...");
        try
        {
            await _mcpService.StopAsync();
            txtMcpStatus.Text = "MCP Server: Stopped";
            txtMcpStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC0, 0x39, 0x2B));
            Log("MCP Server stopped.");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _mcpService.StopAsync().Wait();
        _chromeClient.Dispose();
    }
}
