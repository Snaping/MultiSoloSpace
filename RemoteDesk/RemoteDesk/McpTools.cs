using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace RemoteDesk;

[McpServerToolType]
public static class McpTools
{
    private static ChromeRemoteClient? _client;

    public static void SetClient(ChromeRemoteClient client)
    {
        _client = client;
    }

    private static void EnsureClientConnected()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Chrome Remote client not initialized.");
        }
        if (!_client.IsConnected)
        {
            throw new InvalidOperationException("Not connected to Chrome. Please connect first.");
        }
    }

    [McpServerTool, Description("Starts Chrome browser with remote debugging enabled")]
    public static async Task<string> StartChrome(
        [Description("Optional URL to open in Chrome")] string? url = null,
        CancellationToken cancellationToken = default)
    {
        _client ??= new ChromeRemoteClient();
        
        var success = await _client.StartChromeAsync(url);
        if (success)
        {
            return "Chrome started successfully with remote debugging enabled on port 9222.";
        }
        return "Failed to start Chrome. Make sure Chrome is installed.";
    }

    [McpServerTool, Description("Connects to an existing Chrome page")]
    public static async Task<string> ConnectToPage(
        [Description("Page index to connect to (default: 0)")] int pageIndex = 0,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.ConnectToPageAsync(pageIndex);
        if (success)
        {
            return $"Connected to page {pageIndex}. Current URL: {_client.CurrentPageUrl}";
        }
        return $"Failed to connect to page {pageIndex}. Page may not exist.";
    }

    [McpServerTool, Description("Lists all open Chrome pages")]
    public static async Task<string> ListPages(CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var pages = await _client!.ListPagesAsync();
        if (pages.Count == 0)
        {
            return "No pages found.";
        }

        var result = new System.Text.StringBuilder();
        result.AppendLine("Open pages:");
        for (var i = 0; i < pages.Count; i++)
        {
            result.AppendLine($"[{i}] {pages[i].Title} - {pages[i].Url}");
        }
        return result.ToString();
    }

    [McpServerTool, Description("Navigates to a URL in the current page")]
    public static async Task<string> Navigate(
        [Description("URL to navigate to")] string url,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.NavigateAsync(url);
        if (success)
        {
            await Task.Delay(1000, cancellationToken);
            return $"Navigated to: {url}";
        }
        return "Failed to navigate.";
    }

    [McpServerTool, Description("Takes a screenshot of the current page")]
    public static async Task<string> TakeScreenshot(
        [Description("Image format: png, jpeg, webp (default: png)")] string format = "png",
        [Description("Quality for jpeg/webp (0-100, default: 80)")] int quality = 80,
        [Description("Take full page screenshot (default: false)")] bool fullPage = false,
        [Description("Optional file path to save the screenshot")] string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var bytes = await _client!.TakeScreenshotAsync(format, quality, fullPage);
        if (bytes == null)
        {
            return "Failed to take screenshot.";
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            var fullPath = Path.GetFullPath(filePath);
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
            return $"Screenshot saved to: {fullPath}";
        }

        var base64 = Convert.ToBase64String(bytes);
        return $"data:image/{format};base64,{base64}";
    }

    [McpServerTool, Description("Takes a text snapshot of the current page structure")]
    public static async Task<string> TakeSnapshot(CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var snapshot = await _client!.TakeSnapshotAsync();
        return snapshot ?? "Failed to take snapshot.";
    }

    [McpServerTool, Description("Clicks at the specified coordinates")]
    public static async Task<string> Click(
        [Description("X coordinate")] double x,
        [Description("Y coordinate")] double y,
        [Description("Double click (default: false)")] bool dblClick = false,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = dblClick 
            ? await _client!.DoubleClickAsync(x, y)
            : await _client!.ClickAsync(x, y);
        
        return success 
            ? $"Clicked at ({x}, {y}){(dblClick ? " (double click)" : "")}" 
            : "Failed to click.";
    }

    [McpServerTool, Description("Right-clicks at the specified coordinates")]
    public static async Task<string> RightClick(
        [Description("X coordinate")] double x,
        [Description("Y coordinate")] double y,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.RightClickAsync(x, y);
        return success ? $"Right-clicked at ({x}, {y})" : "Failed to right-click.";
    }

    [McpServerTool, Description("Drags from one position to another")]
    public static async Task<string> Drag(
        [Description("Start X coordinate")] double fromX,
        [Description("Start Y coordinate")] double fromY,
        [Description("End X coordinate")] double toX,
        [Description("End Y coordinate")] double toY,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.DragAsync(fromX, fromY, toX, toY);
        return success 
            ? $"Dragged from ({fromX}, {fromY}) to ({toX}, {toY})" 
            : "Failed to drag.";
    }

    [McpServerTool, Description("Types text into the focused element")]
    public static async Task<string> Type(
        [Description("Text to type")] string text,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.TypeTextAsync(text);
        return success ? $"Typed: {text}" : "Failed to type.";
    }

    [McpServerTool, Description("Hovers the mouse at the specified coordinates")]
    public static async Task<string> Hover(
        [Description("X coordinate")] double x,
        [Description("Y coordinate")] double y,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.HoverAsync(x, y);
        return success ? $"Hovered at ({x}, {y})" : "Failed to hover.";
    }

    [McpServerTool, Description("Evaluates JavaScript in the current page")]
    public static async Task<string> EvaluateScript(
        [Description("JavaScript to evaluate")] string script,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var result = await _client!.EvaluateScriptAsync(script);
        return result?.ToString() ?? "No result or failed to evaluate script.";
    }

    [McpServerTool, Description("Creates a new page/tab")]
    public static async Task<string> NewPage(
        [Description("URL to open in the new page (default: about:blank)")] string url = "about:blank",
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.NewPageAsync(url);
        return success ? $"Created new page with URL: {url}" : "Failed to create new page.";
    }

    [McpServerTool, Description("Closes a page/tab by index")]
    public static async Task<string> ClosePage(
        [Description("Page index to close")] int pageIndex,
        CancellationToken cancellationToken = default)
    {
        EnsureClientConnected();
        
        var success = await _client!.ClosePageAsync(pageIndex);
        return success ? $"Closed page {pageIndex}" : $"Failed to close page {pageIndex}";
    }

    [McpServerTool, Description("Gets the current connection status")]
    public static string GetStatus()
    {
        if (_client == null)
        {
            return "Client not initialized. Call start_chrome first.";
        }

        return _client.IsConnected 
            ? $"Connected to Chrome. Current page: {_client.CurrentPageUrl}" 
            : "Chrome client initialized but not connected to a page.";
    }
}
