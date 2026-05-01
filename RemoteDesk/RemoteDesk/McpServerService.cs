using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace RemoteDesk;

public class McpServerService
{
    private IHost? _host;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    
    public bool IsRunning => _host != null && _serverTask?.IsCompleted == false;

    public event EventHandler<string>? LogMessage;

    public async Task StartAsync(string pipeName, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();

        var builder = Host.CreateApplicationBuilder();
        
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(typeof(McpTools).Assembly);

        _host = builder.Build();

        McpTools.SetClient(new ChromeRemoteClient());

        _serverTask = Task.Run(async () =>
        {
            try
            {
                LogMessage?.Invoke(this, "Starting MCP Server...");
                await _host.RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                LogMessage?.Invoke(this, "MCP Server stopped.");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"MCP Server error: {ex.Message}");
            }
        }, _cts.Token);

        await Task.Delay(1000, cancellationToken);
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_serverTask != null)
        {
            try
            {
                await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch
            {
            }
            _serverTask = null;
        }

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }

        LogMessage?.Invoke(this, "MCP Server stopped.");
    }
}
