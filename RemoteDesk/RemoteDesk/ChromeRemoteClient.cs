using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RemoteDesk;

public class ChromeRemoteClient : IDisposable
{
    private const string ChromeDebugPort = "9222";
    private const string ChromeDebugUrl = "http://localhost:9222";
    
    private Process? _chromeProcess;
    private ClientWebSocket? _webSocket;
    private readonly HttpClient _httpClient = new();
    private int _messageId = 1;
    private readonly Dictionary<int, TaskCompletionSource<JObject>> _pendingRequests = new();
    private readonly CancellationTokenSource _cts = new();
    
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    public string? CurrentPageUrl { get; private set; }
    public int CurrentPageIndex { get; private set; } = 0;

    public event EventHandler<string>? ConsoleMessageReceived;
    public event EventHandler<byte[]>? ScreenshotReceived;

    public async Task<bool> StartChromeAsync(string? startUrl = null, string? chromePath = null)
    {
        try
        {
            if (_chromeProcess != null && !_chromeProcess.HasExited)
            {
                return true;
            }

            var chromeExePath = chromePath ?? FindChromePath();
            if (string.IsNullOrEmpty(chromeExePath))
            {
                throw new InvalidOperationException("Chrome browser not found.");
            }

            var userDataDir = Path.Combine(Path.GetTempPath(), "RemoteDesk_ChromeProfile");
            var args = new List<string>
            {
                $"--remote-debugging-port={ChromeDebugPort}",
                "--no-first-run",
                "--no-default-browser-check",
                "--disable-default-apps",
                $"--user-data-dir={userDataDir}",
                "--remote-allow-origins=*"
            };

            if (!string.IsNullOrEmpty(startUrl))
            {
                args.Add(startUrl);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = chromeExePath,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            _chromeProcess = Process.Start(startInfo);
            
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                try
                {
                    var response = await _httpClient.GetStringAsync($"{ChromeDebugUrl}/json");
                    if (!string.IsNullOrEmpty(response))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start Chrome: {ex.Message}");
            return false;
        }
    }

    private static string? FindChromePath()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Google\Chrome\Application\chrome.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public async Task<List<ChromePageInfo>> ListPagesAsync()
    {
        var response = await _httpClient.GetStringAsync($"{ChromeDebugUrl}/json");
        var pages = JsonConvert.DeserializeObject<List<ChromePageInfo>>(response) ?? new List<ChromePageInfo>();
        return pages.FindAll(p => p.Type == "page");
    }

    public async Task<bool> ConnectToPageAsync(int pageIndex = 0)
    {
        var pages = await ListPagesAsync();
        if (pages.Count <= pageIndex)
        {
            return false;
        }

        var page = pages[pageIndex];
        CurrentPageIndex = pageIndex;
        CurrentPageUrl = page.Url;

        if (_webSocket != null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            _webSocket.Dispose();
        }

        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(new Uri(page.WebSocketDebuggerUrl), CancellationToken.None);

        _ = ReceiveMessagesAsync();

        await EnableDomainsAsync();

        return true;
    }

    private async Task EnableDomainsAsync()
    {
        await SendCommandAsync("Page.enable");
        await SendCommandAsync("DOM.enable");
        await SendCommandAsync("Runtime.enable");
        await SendCommandAsync("Input.enable");
        await SendCommandAsync("Console.enable");
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[8192];
        while (_webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var json = JObject.Parse(message);

                    if (json["id"] != null)
                    {
                        var id = (int)json["id"]!;
                        if (_pendingRequests.TryGetValue(id, out var tcs))
                        {
                            _pendingRequests.Remove(id);
                            tcs.SetResult(json);
                        }
                    }
                    else if (json["method"] != null)
                    {
                        HandleEvent(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocket receive error: {ex.Message}");
                break;
            }
        }
    }

    private void HandleEvent(JObject json)
    {
        var method = json["method"]?.ToString();
        var @params = json["params"] as JObject;

        switch (method)
        {
            case "Console.messageAdded":
                var consoleMessage = @params?["message"]?["text"]?.ToString();
                if (!string.IsNullOrEmpty(consoleMessage))
                {
                    ConsoleMessageReceived?.Invoke(this, consoleMessage);
                }
                break;
        }
    }

    public async Task<JObject?> SendCommandAsync(string method, JObject? @params = null)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            return null;
        }

        var id = Interlocked.Increment(ref _messageId);
        var command = new JObject
        {
            ["id"] = id,
            ["method"] = method
        };

        if (@params != null)
        {
            command["params"] = @params;
        }

        var tcs = new TaskCompletionSource<JObject>();
        _pendingRequests[id] = tcs;

        var jsonBytes = Encoding.UTF8.GetBytes(command.ToString(Formatting.None));
        await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        if (await Task.WhenAny(tcs.Task, Task.Delay(30000)) != tcs.Task)
        {
            _pendingRequests.Remove(id);
            return null;
        }

        return tcs.Task.Result;
    }

    public async Task<bool> NavigateAsync(string url)
    {
        var result = await SendCommandAsync("Page.navigate", new JObject { ["url"] = url });
        CurrentPageUrl = url;
        return result?["result"] != null;
    }

    public async Task<byte[]?> TakeScreenshotAsync(string format = "png", int quality = 80, bool fullPage = false)
    {
        var @params = new JObject
        {
            ["format"] = format
        };

        if (format != "png")
        {
            @params["quality"] = quality;
        }

        var result = await SendCommandAsync("Page.captureScreenshot", @params);
        var data = result?["result"]?["data"]?.ToString();
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        var bytes = Convert.FromBase64String(data);
        ScreenshotReceived?.Invoke(this, bytes);
        return bytes;
    }

    public async Task<bool> ClickAsync(double x, double y, int clickCount = 1)
    {
        var @params = new JObject
        {
            ["type"] = "mousePressed",
            ["x"] = x,
            ["y"] = y,
            ["button"] = "left",
            ["clickCount"] = clickCount
        };
        await SendCommandAsync("Input.dispatchMouseEvent", @params);

        @params["type"] = "mouseReleased";
        await SendCommandAsync("Input.dispatchMouseEvent", @params);

        return true;
    }

    public async Task<bool> DoubleClickAsync(double x, double y)
    {
        return await ClickAsync(x, y, 2);
    }

    public async Task<bool> RightClickAsync(double x, double y)
    {
        var @params = new JObject
        {
            ["type"] = "mousePressed",
            ["x"] = x,
            ["y"] = y,
            ["button"] = "right",
            ["clickCount"] = 1
        };
        await SendCommandAsync("Input.dispatchMouseEvent", @params);

        @params["type"] = "mouseReleased";
        await SendCommandAsync("Input.dispatchMouseEvent", @params);

        return true;
    }

    public async Task<bool> DragAsync(double fromX, double fromY, double toX, double toY)
    {
        await SendCommandAsync("Input.dispatchMouseEvent", new JObject
        {
            ["type"] = "mousePressed",
            ["x"] = fromX,
            ["y"] = fromY,
            ["button"] = "left"
        });

        await Task.Delay(100);

        await SendCommandAsync("Input.dispatchMouseEvent", new JObject
        {
            ["type"] = "mouseMoved",
            ["x"] = toX,
            ["y"] = toY,
            ["button"] = "left"
        });

        await Task.Delay(100);

        await SendCommandAsync("Input.dispatchMouseEvent", new JObject
        {
            ["type"] = "mouseReleased",
            ["x"] = toX,
            ["y"] = toY,
            ["button"] = "left"
        });

        return true;
    }

    public async Task<bool> TypeTextAsync(string text)
    {
        foreach (var c in text)
        {
            var @params = new JObject
            {
                ["type"] = "keyDown",
                ["text"] = c.ToString()
            };
            await SendCommandAsync("Input.dispatchKeyEvent", @params);

            @params["type"] = "keyUp";
            await SendCommandAsync("Input.dispatchKeyEvent", @params);

            await Task.Delay(50);
        }

        return true;
    }

    public async Task<bool> PressKeyAsync(string key)
    {
        var @params = new JObject
        {
            ["type"] = "keyDown",
            ["key"] = key
        };
        await SendCommandAsync("Input.dispatchKeyEvent", @params);

        @params["type"] = "keyUp";
        await SendCommandAsync("Input.dispatchKeyEvent", @params);

        return true;
    }

    public async Task<bool> HoverAsync(double x, double y)
    {
        var @params = new JObject
        {
            ["type"] = "mouseMoved",
            ["x"] = x,
            ["y"] = y
        };
        await SendCommandAsync("Input.dispatchMouseEvent", @params);
        return true;
    }

    public async Task<JObject?> EvaluateScriptAsync(string script)
    {
        var result = await SendCommandAsync("Runtime.evaluate", new JObject
        {
            ["expression"] = script,
            ["returnByValue"] = true
        });
        return result?["result"]?["result"] as JObject;
    }

    public async Task<JObject?> GetDocumentAsync()
    {
        var result = await SendCommandAsync("DOM.getDocument");
        return result?["result"]?["root"] as JObject;
    }

    public async Task<string?> TakeSnapshotAsync()
    {
        var doc = await GetDocumentAsync();
        if (doc == null) return null;

        var snapshot = new StringBuilder();
        snapshot.AppendLine("Page Snapshot:");
        snapshot.AppendLine($"URL: {CurrentPageUrl}");
        snapshot.AppendLine("---");
        
        var result = await SendCommandAsync("DOM.querySelector", new JObject
        {
            ["nodeId"] = doc["nodeId"],
            ["selector"] = "body"
        });

        if (result?["result"]?["nodeId"] != null)
        {
            var bodyId = (int)result["result"]["nodeId"]!;
            var htmlResult = await SendCommandAsync("DOM.getOuterHTML", new JObject
            {
                ["nodeId"] = bodyId
            });
            var html = htmlResult?["result"]?["outerHTML"]?.ToString();
            if (!string.IsNullOrEmpty(html) && html.Length > 5000)
            {
                html = html.Substring(0, 5000) + "...";
            }
            snapshot.AppendLine(html);
        }

        return snapshot.ToString();
    }

    public async Task<bool> NewPageAsync(string url = "about:blank")
    {
        var response = await _httpClient.PostAsync(
            $"{ChromeDebugUrl}/json/new?{Uri.EscapeDataString(url)}",
            null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ClosePageAsync(int pageIndex)
    {
        var pages = await ListPagesAsync();
        if (pages.Count <= pageIndex) return false;

        var page = pages[pageIndex];
        var response = await _httpClient.GetAsync($"{ChromeDebugUrl}/json/close/{page.Id}");
        return response.IsSuccessStatusCode;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _webSocket?.Dispose();
        _httpClient.Dispose();
        
        if (_chromeProcess != null && !_chromeProcess.HasExited)
        {
            try
            {
                _chromeProcess.Kill();
            }
            catch
            {
            }
            _chromeProcess.Dispose();
        }

        _cts.Dispose();
    }
}

public class ChromePageInfo
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("webSocketDebuggerUrl")]
    public string WebSocketDebuggerUrl { get; set; } = string.Empty;
}
