using System.Collections.Concurrent;
using System.Diagnostics;
using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public class HookService : IHookService
{
    private readonly ILoggerService _loggerService;
    private readonly IFilterService _filterService;
    private readonly ConcurrentDictionary<string, ApiDefinition> _apiDefinitions;
    private readonly ConcurrentDictionary<Guid, ApiCallInfo> _capturedCalls;
    private readonly ConcurrentDictionary<string, string> _redirects;
    private readonly ConcurrentDictionary<Guid, string> _modifiedReturns;
    
    private int _targetProcessId;
    private bool _isHooking;
    private readonly object _lock = new();

    public event EventHandler<ApiCallInfo>? ApiCallCaptured;

    public bool IsHooking => _isHooking;
    public int TargetProcessId => _targetProcessId;

    public HookService(ILoggerService loggerService, IFilterService filterService)
    {
        _loggerService = loggerService;
        _filterService = filterService;
        _apiDefinitions = new ConcurrentDictionary<string, ApiDefinition>();
        _capturedCalls = new ConcurrentDictionary<Guid, ApiCallInfo>();
        _redirects = new ConcurrentDictionary<string, string>();
        _modifiedReturns = new ConcurrentDictionary<Guid, string>();
        
        InitializeApiDefinitions();
    }

    private void InitializeApiDefinitions()
    {
        var apis = new List<ApiDefinition>
        {
            new() { Name = "CreateFileW", ModuleName = "kernel32.dll", FunctionName = "CreateFileW", Description = "创建或打开文件", Category = ApiCategory.FileSystem },
            new() { Name = "ReadFile", ModuleName = "kernel32.dll", FunctionName = "ReadFile", Description = "读取文件", Category = ApiCategory.FileSystem },
            new() { Name = "WriteFile", ModuleName = "kernel32.dll", FunctionName = "WriteFile", Description = "写入文件", Category = ApiCategory.FileSystem },
            new() { Name = "DeleteFileW", ModuleName = "kernel32.dll", FunctionName = "DeleteFileW", Description = "删除文件", Category = ApiCategory.FileSystem },
            new() { Name = "CopyFileW", ModuleName = "kernel32.dll", FunctionName = "CopyFileW", Description = "复制文件", Category = ApiCategory.FileSystem },
            new() { Name = "MoveFileW", ModuleName = "kernel32.dll", FunctionName = "MoveFileW", Description = "移动文件", Category = ApiCategory.FileSystem },
            
            new() { Name = "RegOpenKeyExW", ModuleName = "advapi32.dll", FunctionName = "RegOpenKeyExW", Description = "打开注册表键", Category = ApiCategory.Registry },
            new() { Name = "RegQueryValueExW", ModuleName = "advapi32.dll", FunctionName = "RegQueryValueExW", Description = "查询注册表值", Category = ApiCategory.Registry },
            new() { Name = "RegSetValueExW", ModuleName = "advapi32.dll", FunctionName = "RegSetValueExW", Description = "设置注册表值", Category = ApiCategory.Registry },
            
            new() { Name = "InternetOpenW", ModuleName = "wininet.dll", FunctionName = "InternetOpenW", Description = "初始化网络连接", Category = ApiCategory.Network },
            new() { Name = "InternetConnectW", ModuleName = "wininet.dll", FunctionName = "InternetConnectW", Description = "连接到服务器", Category = ApiCategory.Network },
            new() { Name = "HttpSendRequestW", ModuleName = "wininet.dll", FunctionName = "HttpSendRequestW", Description = "发送HTTP请求", Category = ApiCategory.Network },
            
            new() { Name = "CreateProcessW", ModuleName = "kernel32.dll", FunctionName = "CreateProcessW", Description = "创建进程", Category = ApiCategory.Process },
            new() { Name = "OpenProcess", ModuleName = "kernel32.dll", FunctionName = "OpenProcess", Description = "打开进程", Category = ApiCategory.Process },
            new() { Name = "TerminateProcess", ModuleName = "kernel32.dll", FunctionName = "TerminateProcess", Description = "终止进程", Category = ApiCategory.Process },
            
            new() { Name = "CreateThread", ModuleName = "kernel32.dll", FunctionName = "CreateThread", Description = "创建线程", Category = ApiCategory.Thread },
            new() { Name = "OpenThread", ModuleName = "kernel32.dll", FunctionName = "OpenThread", Description = "打开线程", Category = ApiCategory.Thread },
            
            new() { Name = "VirtualAlloc", ModuleName = "kernel32.dll", FunctionName = "VirtualAlloc", Description = "分配虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualFree", ModuleName = "kernel32.dll", FunctionName = "VirtualFree", Description = "释放虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualProtect", ModuleName = "kernel32.dll", FunctionName = "VirtualProtect", Description = "修改内存保护", Category = ApiCategory.Memory },
            
            new() { Name = "CreateWindowExW", ModuleName = "user32.dll", FunctionName = "CreateWindowExW", Description = "创建窗口", Category = ApiCategory.Window },
            new() { Name = "ShowWindow", ModuleName = "user32.dll", FunctionName = "ShowWindow", Description = "显示窗口", Category = ApiCategory.Window },
            new() { Name = "MessageBoxW", ModuleName = "user32.dll", FunctionName = "MessageBoxW", Description = "显示消息框", Category = ApiCategory.Window },
        };

        foreach (var api in apis)
        {
            _apiDefinitions.TryAdd(api.Name, api);
        }
    }

    public void StartHooking(int processId)
    {
        lock (_lock)
        {
            if (_isHooking)
                return;

            _targetProcessId = processId;
            _isHooking = true;
            
            _loggerService.LogMessage($"开始钩取进程: {processId}", LogLevel.Info);
        }
    }

    public void StopHooking()
    {
        lock (_lock)
        {
            if (!_isHooking)
                return;

            try
            {
                _isHooking = false;
                
                _loggerService.LogMessage("停止钩取", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _loggerService.LogMessage($"停止钩取失败: {ex.Message}", LogLevel.Error);
            }
        }
    }

    public void EnableApiHook(string apiName)
    {
        if (_apiDefinitions.TryGetValue(apiName, out var api))
        {
            api.IsEnabled = true;
            _loggerService.LogMessage($"启用API钩取: {apiName}", LogLevel.Info);
        }
    }

    public void DisableApiHook(string apiName)
    {
        if (_apiDefinitions.TryGetValue(apiName, out var api))
        {
            api.IsEnabled = false;
            _loggerService.LogMessage($"禁用API钩取: {apiName}", LogLevel.Info);
        }
    }

    public List<ApiDefinition> GetAvailableApis()
    {
        return _apiDefinitions.Values.ToList();
    }

    public void ModifyReturnValue(Guid callId, string newValue)
    {
        _modifiedReturns.TryAdd(callId, newValue);
        
        if (_capturedCalls.TryGetValue(callId, out var callInfo))
        {
            callInfo.ModifiedReturnValue = newValue;
            callInfo.IsModified = true;
            _loggerService.LogMessage($"修改返回值: {callId} -> {newValue}", LogLevel.Info);
        }
    }

    public void SetRedirect(string apiName, string targetFunction)
    {
        _redirects.TryAdd(apiName, targetFunction);
        _loggerService.LogMessage($"设置重定向: {apiName} -> {targetFunction}", LogLevel.Info);
    }

    public void RemoveRedirect(string apiName)
    {
        _redirects.TryRemove(apiName, out _);
        _loggerService.LogMessage($"移除重定向: {apiName}", LogLevel.Info);
    }

    protected virtual void OnApiCallCaptured(ApiCallInfo e)
    {
        ApiCallCaptured?.Invoke(this, e);
    }

    public void SimulateApiCall(string apiName, string parameters, string returnValue)
    {
        if (!_apiDefinitions.TryGetValue(apiName, out var api) || !api.IsEnabled)
            return;

        var callInfo = new ApiCallInfo
        {
            ApiName = apiName,
            ModuleName = api.ModuleName,
            ProcessId = Process.GetCurrentProcess().Id,
            ProcessName = Process.GetCurrentProcess().ProcessName,
            Parameters = parameters,
            OriginalReturnValue = returnValue
        };

        if (_redirects.TryGetValue(apiName, out var redirectTarget))
        {
            callInfo.IsRedirected = true;
            callInfo.RedirectTarget = redirectTarget;
        }

        if (_filterService.ShouldCapture(callInfo))
        {
            _capturedCalls.TryAdd(callInfo.Id, callInfo);
            _loggerService.LogApiCall(callInfo);
            OnApiCallCaptured(callInfo);
        }
    }
}
