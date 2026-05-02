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
            new() { Name = "FindFirstFileW", ModuleName = "kernel32.dll", FunctionName = "FindFirstFileW", Description = "查找第一个文件", Category = ApiCategory.FileSystem },
            new() { Name = "FindNextFileW", ModuleName = "kernel32.dll", FunctionName = "FindNextFileW", Description = "查找下一个文件", Category = ApiCategory.FileSystem },
            new() { Name = "GetFileAttributesW", ModuleName = "kernel32.dll", FunctionName = "GetFileAttributesW", Description = "获取文件属性", Category = ApiCategory.FileSystem },
            new() { Name = "SetFileAttributesW", ModuleName = "kernel32.dll", FunctionName = "SetFileAttributesW", Description = "设置文件属性", Category = ApiCategory.FileSystem },
            new() { Name = "CreateFileMappingW", ModuleName = "kernel32.dll", FunctionName = "CreateFileMappingW", Description = "创建文件映射", Category = ApiCategory.FileSystem },
            new() { Name = "MapViewOfFile", ModuleName = "kernel32.dll", FunctionName = "MapViewOfFile", Description = "映射文件视图", Category = ApiCategory.FileSystem },
            
            new() { Name = "RegOpenKeyExW", ModuleName = "advapi32.dll", FunctionName = "RegOpenKeyExW", Description = "打开注册表键", Category = ApiCategory.Registry },
            new() { Name = "RegQueryValueExW", ModuleName = "advapi32.dll", FunctionName = "RegQueryValueExW", Description = "查询注册表值", Category = ApiCategory.Registry },
            new() { Name = "RegSetValueExW", ModuleName = "advapi32.dll", FunctionName = "RegSetValueExW", Description = "设置注册表值", Category = ApiCategory.Registry },
            new() { Name = "RegCreateKeyExW", ModuleName = "advapi32.dll", FunctionName = "RegCreateKeyExW", Description = "创建注册表键", Category = ApiCategory.Registry },
            new() { Name = "RegDeleteKeyW", ModuleName = "advapi32.dll", FunctionName = "RegDeleteKeyW", Description = "删除注册表键", Category = ApiCategory.Registry },
            new() { Name = "RegDeleteValueW", ModuleName = "advapi32.dll", FunctionName = "RegDeleteValueW", Description = "删除注册表值", Category = ApiCategory.Registry },
            new() { Name = "RegEnumKeyExW", ModuleName = "advapi32.dll", FunctionName = "RegEnumKeyExW", Description = "枚举注册表子键", Category = ApiCategory.Registry },
            new() { Name = "RegEnumValueW", ModuleName = "advapi32.dll", FunctionName = "RegEnumValueW", Description = "枚举注册表值", Category = ApiCategory.Registry },
            
            new() { Name = "InternetOpenW", ModuleName = "wininet.dll", FunctionName = "InternetOpenW", Description = "初始化网络连接", Category = ApiCategory.Network },
            new() { Name = "InternetConnectW", ModuleName = "wininet.dll", FunctionName = "InternetConnectW", Description = "连接到服务器", Category = ApiCategory.Network },
            new() { Name = "HttpSendRequestW", ModuleName = "wininet.dll", FunctionName = "HttpSendRequestW", Description = "发送HTTP请求", Category = ApiCategory.Network },
            new() { Name = "HttpOpenRequestW", ModuleName = "wininet.dll", FunctionName = "HttpOpenRequestW", Description = "打开HTTP请求", Category = ApiCategory.Network },
            new() { Name = "InternetReadFile", ModuleName = "wininet.dll", FunctionName = "InternetReadFile", Description = "读取网络数据", Category = ApiCategory.Network },
            new() { Name = "InternetWriteFile", ModuleName = "wininet.dll", FunctionName = "InternetWriteFile", Description = "写入网络数据", Category = ApiCategory.Network },
            new() { Name = "InternetCloseHandle", ModuleName = "wininet.dll", FunctionName = "InternetCloseHandle", Description = "关闭网络句柄", Category = ApiCategory.Network },
            new() { Name = "connect", ModuleName = "ws2_32.dll", FunctionName = "connect", Description = "连接到套接字", Category = ApiCategory.Network },
            new() { Name = "send", ModuleName = "ws2_32.dll", FunctionName = "send", Description = "发送数据", Category = ApiCategory.Network },
            new() { Name = "recv", ModuleName = "ws2_32.dll", FunctionName = "recv", Description = "接收数据", Category = ApiCategory.Network },
            new() { Name = "socket", ModuleName = "ws2_32.dll", FunctionName = "socket", Description = "创建套接字", Category = ApiCategory.Network },
            new() { Name = "WSASocketW", ModuleName = "ws2_32.dll", FunctionName = "WSASocketW", Description = "创建Windows套接字", Category = ApiCategory.Network },
            
            new() { Name = "CreateProcessW", ModuleName = "kernel32.dll", FunctionName = "CreateProcessW", Description = "创建进程", Category = ApiCategory.Process },
            new() { Name = "OpenProcess", ModuleName = "kernel32.dll", FunctionName = "OpenProcess", Description = "打开进程", Category = ApiCategory.Process },
            new() { Name = "TerminateProcess", ModuleName = "kernel32.dll", FunctionName = "TerminateProcess", Description = "终止进程", Category = ApiCategory.Process },
            new() { Name = "GetCurrentProcess", ModuleName = "kernel32.dll", FunctionName = "GetCurrentProcess", Description = "获取当前进程", Category = ApiCategory.Process },
            new() { Name = "GetCurrentProcessId", ModuleName = "kernel32.dll", FunctionName = "GetCurrentProcessId", Description = "获取当前进程ID", Category = ApiCategory.Process },
            new() { Name = "GetExitCodeProcess", ModuleName = "kernel32.dll", FunctionName = "GetExitCodeProcess", Description = "获取进程退出代码", Category = ApiCategory.Process },
            new() { Name = "GetPriorityClass", ModuleName = "kernel32.dll", FunctionName = "GetPriorityClass", Description = "获取进程优先级", Category = ApiCategory.Process },
            new() { Name = "SetPriorityClass", ModuleName = "kernel32.dll", FunctionName = "SetPriorityClass", Description = "设置进程优先级", Category = ApiCategory.Process },
            new() { Name = "GetProcessAffinityMask", ModuleName = "kernel32.dll", FunctionName = "GetProcessAffinityMask", Description = "获取进程亲和性掩码", Category = ApiCategory.Process },
            new() { Name = "SetProcessAffinityMask", ModuleName = "kernel32.dll", FunctionName = "SetProcessAffinityMask", Description = "设置进程亲和性掩码", Category = ApiCategory.Process },
            new() { Name = "CreateRemoteThread", ModuleName = "kernel32.dll", FunctionName = "CreateRemoteThread", Description = "创建远程线程", Category = ApiCategory.Process },
            new() { Name = "OpenProcessToken", ModuleName = "advapi32.dll", FunctionName = "OpenProcessToken", Description = "打开进程令牌", Category = ApiCategory.Process },
            new() { Name = "LookupPrivilegeValueW", ModuleName = "advapi32.dll", FunctionName = "LookupPrivilegeValueW", Description = "查找特权值", Category = ApiCategory.Process },
            new() { Name = "AdjustTokenPrivileges", ModuleName = "advapi32.dll", FunctionName = "AdjustTokenPrivileges", Description = "调整令牌特权", Category = ApiCategory.Process },
            new() { Name = "EnumProcesses", ModuleName = "psapi.dll", FunctionName = "EnumProcesses", Description = "枚举进程", Category = ApiCategory.Process },
            new() { Name = "EnumProcessModules", ModuleName = "psapi.dll", FunctionName = "EnumProcessModules", Description = "枚举进程模块", Category = ApiCategory.Process },
            new() { Name = "GetModuleBaseNameW", ModuleName = "psapi.dll", FunctionName = "GetModuleBaseNameW", Description = "获取模块基名", Category = ApiCategory.Process },
            new() { Name = "GetModuleFileNameExW", ModuleName = "psapi.dll", FunctionName = "GetModuleFileNameExW", Description = "获取模块文件名", Category = ApiCategory.Process },
            new() { Name = "GetProcessImageFileNameW", ModuleName = "psapi.dll", FunctionName = "GetProcessImageFileNameW", Description = "获取进程映像文件名", Category = ApiCategory.Process },
            new() { Name = "QueryFullProcessImageNameW", ModuleName = "kernel32.dll", FunctionName = "QueryFullProcessImageNameW", Description = "查询完整进程映像名", Category = ApiCategory.Process },
            
            new() { Name = "CreateThread", ModuleName = "kernel32.dll", FunctionName = "CreateThread", Description = "创建线程", Category = ApiCategory.Thread },
            new() { Name = "OpenThread", ModuleName = "kernel32.dll", FunctionName = "OpenThread", Description = "打开线程", Category = ApiCategory.Thread },
            new() { Name = "SuspendThread", ModuleName = "kernel32.dll", FunctionName = "SuspendThread", Description = "挂起线程", Category = ApiCategory.Thread },
            new() { Name = "ResumeThread", ModuleName = "kernel32.dll", FunctionName = "ResumeThread", Description = "恢复线程", Category = ApiCategory.Thread },
            new() { Name = "TerminateThread", ModuleName = "kernel32.dll", FunctionName = "TerminateThread", Description = "终止线程", Category = ApiCategory.Thread },
            new() { Name = "GetCurrentThread", ModuleName = "kernel32.dll", FunctionName = "GetCurrentThread", Description = "获取当前线程", Category = ApiCategory.Thread },
            new() { Name = "GetCurrentThreadId", ModuleName = "kernel32.dll", FunctionName = "GetCurrentThreadId", Description = "获取当前线程ID", Category = ApiCategory.Thread },
            new() { Name = "SetThreadContext", ModuleName = "kernel32.dll", FunctionName = "SetThreadContext", Description = "设置线程上下文", Category = ApiCategory.Thread },
            new() { Name = "GetThreadContext", ModuleName = "kernel32.dll", FunctionName = "GetThreadContext", Description = "获取线程上下文", Category = ApiCategory.Thread },
            new() { Name = "QueueUserAPC", ModuleName = "kernel32.dll", FunctionName = "QueueUserAPC", Description = "排队用户APC", Category = ApiCategory.Thread },
            new() { Name = "CreateRemoteThreadEx", ModuleName = "kernel32.dll", FunctionName = "CreateRemoteThreadEx", Description = "创建远程线程Ex", Category = ApiCategory.Thread },
            
            new() { Name = "VirtualAlloc", ModuleName = "kernel32.dll", FunctionName = "VirtualAlloc", Description = "分配虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualFree", ModuleName = "kernel32.dll", FunctionName = "VirtualFree", Description = "释放虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualProtect", ModuleName = "kernel32.dll", FunctionName = "VirtualProtect", Description = "修改内存保护", Category = ApiCategory.Memory },
            new() { Name = "VirtualQuery", ModuleName = "kernel32.dll", FunctionName = "VirtualQuery", Description = "查询虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualAllocEx", ModuleName = "kernel32.dll", FunctionName = "VirtualAllocEx", Description = "分配远程虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualFreeEx", ModuleName = "kernel32.dll", FunctionName = "VirtualFreeEx", Description = "释放远程虚拟内存", Category = ApiCategory.Memory },
            new() { Name = "VirtualProtectEx", ModuleName = "kernel32.dll", FunctionName = "VirtualProtectEx", Description = "修改远程内存保护", Category = ApiCategory.Memory },
            new() { Name = "ReadProcessMemory", ModuleName = "kernel32.dll", FunctionName = "ReadProcessMemory", Description = "读取进程内存", Category = ApiCategory.Memory },
            new() { Name = "WriteProcessMemory", ModuleName = "kernel32.dll", FunctionName = "WriteProcessMemory", Description = "写入进程内存", Category = ApiCategory.Memory },
            new() { Name = "NtAllocateVirtualMemory", ModuleName = "ntdll.dll", FunctionName = "NtAllocateVirtualMemory", Description = "分配虚拟内存(原生)", Category = ApiCategory.Memory },
            new() { Name = "NtFreeVirtualMemory", ModuleName = "ntdll.dll", FunctionName = "NtFreeVirtualMemory", Description = "释放虚拟内存(原生)", Category = ApiCategory.Memory },
            new() { Name = "NtProtectVirtualMemory", ModuleName = "ntdll.dll", FunctionName = "NtProtectVirtualMemory", Description = "保护虚拟内存(原生)", Category = ApiCategory.Memory },
            new() { Name = "NtReadVirtualMemory", ModuleName = "ntdll.dll", FunctionName = "NtReadVirtualMemory", Description = "读取虚拟内存(原生)", Category = ApiCategory.Memory },
            new() { Name = "NtWriteVirtualMemory", ModuleName = "ntdll.dll", FunctionName = "NtWriteVirtualMemory", Description = "写入虚拟内存(原生)", Category = ApiCategory.Memory },
            
            new() { Name = "CreateWindowExW", ModuleName = "user32.dll", FunctionName = "CreateWindowExW", Description = "创建窗口", Category = ApiCategory.Window },
            new() { Name = "ShowWindow", ModuleName = "user32.dll", FunctionName = "ShowWindow", Description = "显示窗口", Category = ApiCategory.Window },
            new() { Name = "MessageBoxW", ModuleName = "user32.dll", FunctionName = "MessageBoxW", Description = "显示消息框", Category = ApiCategory.Window },
            new() { Name = "FindWindowW", ModuleName = "user32.dll", FunctionName = "FindWindowW", Description = "查找窗口", Category = ApiCategory.Window },
            new() { Name = "FindWindowExW", ModuleName = "user32.dll", FunctionName = "FindWindowExW", Description = "查找子窗口", Category = ApiCategory.Window },
            new() { Name = "GetWindowTextW", ModuleName = "user32.dll", FunctionName = "GetWindowTextW", Description = "获取窗口文本", Category = ApiCategory.Window },
            new() { Name = "SetWindowTextW", ModuleName = "user32.dll", FunctionName = "SetWindowTextW", Description = "设置窗口文本", Category = ApiCategory.Window },
            new() { Name = "GetWindowLongW", ModuleName = "user32.dll", FunctionName = "GetWindowLongW", Description = "获取窗口长整型", Category = ApiCategory.Window },
            new() { Name = "SetWindowLongW", ModuleName = "user32.dll", FunctionName = "SetWindowLongW", Description = "设置窗口长整型", Category = ApiCategory.Window },
            new() { Name = "SetWindowsHookExW", ModuleName = "user32.dll", FunctionName = "SetWindowsHookExW", Description = "设置Windows钩子", Category = ApiCategory.Window },
            new() { Name = "UnhookWindowsHookEx", ModuleName = "user32.dll", FunctionName = "UnhookWindowsHookEx", Description = "卸载Windows钩子", Category = ApiCategory.Window },
            new() { Name = "CallNextHookEx", ModuleName = "user32.dll", FunctionName = "CallNextHookEx", Description = "调用下一个钩子", Category = ApiCategory.Window },
            new() { Name = "SendMessageW", ModuleName = "user32.dll", FunctionName = "SendMessageW", Description = "发送消息", Category = ApiCategory.Window },
            new() { Name = "PostMessageW", ModuleName = "user32.dll", FunctionName = "PostMessageW", Description = "投递消息", Category = ApiCategory.Window },
            
            new() { Name = "GetSystemTime", ModuleName = "kernel32.dll", FunctionName = "GetSystemTime", Description = "获取系统时间", Category = ApiCategory.Time },
            new() { Name = "GetLocalTime", ModuleName = "kernel32.dll", FunctionName = "GetLocalTime", Description = "获取本地时间", Category = ApiCategory.Time },
            new() { Name = "SetSystemTime", ModuleName = "kernel32.dll", FunctionName = "SetSystemTime", Description = "设置系统时间", Category = ApiCategory.Time },
            new() { Name = "SetLocalTime", ModuleName = "kernel32.dll", FunctionName = "SetLocalTime", Description = "设置本地时间", Category = ApiCategory.Time },
            new() { Name = "GetTickCount", ModuleName = "kernel32.dll", FunctionName = "GetTickCount", Description = "获取滴答计数", Category = ApiCategory.Time },
            new() { Name = "GetTickCount64", ModuleName = "kernel32.dll", FunctionName = "GetTickCount64", Description = "获取滴答计数64位", Category = ApiCategory.Time },
            new() { Name = "QueryPerformanceCounter", ModuleName = "kernel32.dll", FunctionName = "QueryPerformanceCounter", Description = "查询性能计数器", Category = ApiCategory.Time },
            new() { Name = "QueryPerformanceFrequency", ModuleName = "kernel32.dll", FunctionName = "QueryPerformanceFrequency", Description = "查询性能频率", Category = ApiCategory.Time },
            new() { Name = "Sleep", ModuleName = "kernel32.dll", FunctionName = "Sleep", Description = "睡眠", Category = ApiCategory.Time },
            new() { Name = "SleepEx", ModuleName = "kernel32.dll", FunctionName = "SleepEx", Description = "睡眠Ex", Category = ApiCategory.Time },
            new() { Name = "GetSystemTimeAsFileTime", ModuleName = "kernel32.dll", FunctionName = "GetSystemTimeAsFileTime", Description = "获取系统时间为文件时间", Category = ApiCategory.Time },
            new() { Name = "GetSystemTimePreciseAsFileTime", ModuleName = "kernel32.dll", FunctionName = "GetSystemTimePreciseAsFileTime", Description = "获取精确系统时间为文件时间", Category = ApiCategory.Time },
            new() { Name = "FileTimeToSystemTime", ModuleName = "kernel32.dll", FunctionName = "FileTimeToSystemTime", Description = "文件时间转系统时间", Category = ApiCategory.Time },
            new() { Name = "SystemTimeToFileTime", ModuleName = "kernel32.dll", FunctionName = "SystemTimeToFileTime", Description = "系统时间转文件时间", Category = ApiCategory.Time },
            new() { Name = "CompareFileTime", ModuleName = "kernel32.dll", FunctionName = "CompareFileTime", Description = "比较文件时间", Category = ApiCategory.Time },
            new() { Name = "NtQuerySystemTime", ModuleName = "ntdll.dll", FunctionName = "NtQuerySystemTime", Description = "查询系统时间(原生)", Category = ApiCategory.Time },
            new() { Name = "NtSetSystemTime", ModuleName = "ntdll.dll", FunctionName = "NtSetSystemTime", Description = "设置系统时间(原生)", Category = ApiCategory.Time },
            new() { Name = "NtDelayExecution", ModuleName = "ntdll.dll", FunctionName = "NtDelayExecution", Description = "延迟执行(原生)", Category = ApiCategory.Time },
            
            new() { Name = "NtOpenProcess", ModuleName = "ntdll.dll", FunctionName = "NtOpenProcess", Description = "打开进程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtTerminateProcess", ModuleName = "ntdll.dll", FunctionName = "NtTerminateProcess", Description = "终止进程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateThreadEx", ModuleName = "ntdll.dll", FunctionName = "NtCreateThreadEx", Description = "创建线程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenThread", ModuleName = "ntdll.dll", FunctionName = "NtOpenThread", Description = "打开线程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtTerminateThread", ModuleName = "ntdll.dll", FunctionName = "NtTerminateThread", Description = "终止线程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSuspendThread", ModuleName = "ntdll.dll", FunctionName = "NtSuspendThread", Description = "挂起线程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtResumeThread", ModuleName = "ntdll.dll", FunctionName = "NtResumeThread", Description = "恢复线程(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtQueryInformationProcess", ModuleName = "ntdll.dll", FunctionName = "NtQueryInformationProcess", Description = "查询进程信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetInformationProcess", ModuleName = "ntdll.dll", FunctionName = "NtSetInformationProcess", Description = "设置进程信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtQueryInformationThread", ModuleName = "ntdll.dll", FunctionName = "NtQueryInformationThread", Description = "查询线程信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetInformationThread", ModuleName = "ntdll.dll", FunctionName = "NtSetInformationThread", Description = "设置线程信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtQuerySystemInformation", ModuleName = "ntdll.dll", FunctionName = "NtQuerySystemInformation", Description = "查询系统信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetSystemInformation", ModuleName = "ntdll.dll", FunctionName = "NtSetSystemInformation", Description = "设置系统信息(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenKey", ModuleName = "ntdll.dll", FunctionName = "NtOpenKey", Description = "打开注册表键(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateKey", ModuleName = "ntdll.dll", FunctionName = "NtCreateKey", Description = "创建注册表键(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtDeleteKey", ModuleName = "ntdll.dll", FunctionName = "NtDeleteKey", Description = "删除注册表键(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtQueryValueKey", ModuleName = "ntdll.dll", FunctionName = "NtQueryValueKey", Description = "查询注册表值(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetValueKey", ModuleName = "ntdll.dll", FunctionName = "NtSetValueKey", Description = "设置注册表值(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtDeleteValueKey", ModuleName = "ntdll.dll", FunctionName = "NtDeleteValueKey", Description = "删除注册表值(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenFile", ModuleName = "ntdll.dll", FunctionName = "NtOpenFile", Description = "打开文件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateFile", ModuleName = "ntdll.dll", FunctionName = "NtCreateFile", Description = "创建文件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtReadFile", ModuleName = "ntdll.dll", FunctionName = "NtReadFile", Description = "读取文件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtWriteFile", ModuleName = "ntdll.dll", FunctionName = "NtWriteFile", Description = "写入文件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtDeleteFile", ModuleName = "ntdll.dll", FunctionName = "NtDeleteFile", Description = "删除文件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtClose", ModuleName = "ntdll.dll", FunctionName = "NtClose", Description = "关闭句柄(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtDuplicateObject", ModuleName = "ntdll.dll", FunctionName = "NtDuplicateObject", Description = "复制对象(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtWaitForSingleObject", ModuleName = "ntdll.dll", FunctionName = "NtWaitForSingleObject", Description = "等待单个对象(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtWaitForMultipleObjects", ModuleName = "ntdll.dll", FunctionName = "NtWaitForMultipleObjects", Description = "等待多个对象(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateEvent", ModuleName = "ntdll.dll", FunctionName = "NtCreateEvent", Description = "创建事件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenEvent", ModuleName = "ntdll.dll", FunctionName = "NtOpenEvent", Description = "打开事件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetEvent", ModuleName = "ntdll.dll", FunctionName = "NtSetEvent", Description = "设置事件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtResetEvent", ModuleName = "ntdll.dll", FunctionName = "NtResetEvent", Description = "重置事件(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateMutant", ModuleName = "ntdll.dll", FunctionName = "NtCreateMutant", Description = "创建互斥体(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenMutant", ModuleName = "ntdll.dll", FunctionName = "NtOpenMutant", Description = "打开互斥体(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtReleaseMutant", ModuleName = "ntdll.dll", FunctionName = "NtReleaseMutant", Description = "释放互斥体(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateSemaphore", ModuleName = "ntdll.dll", FunctionName = "NtCreateSemaphore", Description = "创建信号量(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenSemaphore", ModuleName = "ntdll.dll", FunctionName = "NtOpenSemaphore", Description = "打开信号量(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtReleaseSemaphore", ModuleName = "ntdll.dll", FunctionName = "NtReleaseSemaphore", Description = "释放信号量(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateTimer", ModuleName = "ntdll.dll", FunctionName = "NtCreateTimer", Description = "创建计时器(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenTimer", ModuleName = "ntdll.dll", FunctionName = "NtOpenTimer", Description = "打开计时器(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtSetTimer", ModuleName = "ntdll.dll", FunctionName = "NtSetTimer", Description = "设置计时器(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCancelTimer", ModuleName = "ntdll.dll", FunctionName = "NtCancelTimer", Description = "取消计时器(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreateSection", ModuleName = "ntdll.dll", FunctionName = "NtCreateSection", Description = "创建节(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtOpenSection", ModuleName = "ntdll.dll", FunctionName = "NtOpenSection", Description = "打开节(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtMapViewOfSection", ModuleName = "ntdll.dll", FunctionName = "NtMapViewOfSection", Description = "映射节视图(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtUnmapViewOfSection", ModuleName = "ntdll.dll", FunctionName = "NtUnmapViewOfSection", Description = "取消映射节视图(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtExtendSection", ModuleName = "ntdll.dll", FunctionName = "NtExtendSection", Description = "扩展节(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtQuerySection", ModuleName = "ntdll.dll", FunctionName = "NtQuerySection", Description = "查询节(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCreatePort", ModuleName = "ntdll.dll", FunctionName = "NtCreatePort", Description = "创建端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtConnectPort", ModuleName = "ntdll.dll", FunctionName = "NtConnectPort", Description = "连接端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtListenPort", ModuleName = "ntdll.dll", FunctionName = "NtListenPort", Description = "监听端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtAcceptConnectPort", ModuleName = "ntdll.dll", FunctionName = "NtAcceptConnectPort", Description = "接受连接端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtCompleteConnectPort", ModuleName = "ntdll.dll", FunctionName = "NtCompleteConnectPort", Description = "完成连接端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtRequestPort", ModuleName = "ntdll.dll", FunctionName = "NtRequestPort", Description = "请求端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtRequestWaitReplyPort", ModuleName = "ntdll.dll", FunctionName = "NtRequestWaitReplyPort", Description = "请求等待回复端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtReplyPort", ModuleName = "ntdll.dll", FunctionName = "NtReplyPort", Description = "回复端口(原生)", Category = ApiCategory.Driver },
            new() { Name = "NtImpersonateClientOfPort", ModuleName = "ntdll.dll", FunctionName = "NtImpersonateClientOfPort", Description = "模拟端口客户端(原生)", Category = ApiCategory.Driver },
            
            new() { Name = "CreateMutexW", ModuleName = "kernel32.dll", FunctionName = "CreateMutexW", Description = "创建互斥体", Category = ApiCategory.Synchronization },
            new() { Name = "OpenMutexW", ModuleName = "kernel32.dll", FunctionName = "OpenMutexW", Description = "打开互斥体", Category = ApiCategory.Synchronization },
            new() { Name = "ReleaseMutex", ModuleName = "kernel32.dll", FunctionName = "ReleaseMutex", Description = "释放互斥体", Category = ApiCategory.Synchronization },
            new() { Name = "CreateEventW", ModuleName = "kernel32.dll", FunctionName = "CreateEventW", Description = "创建事件", Category = ApiCategory.Synchronization },
            new() { Name = "OpenEventW", ModuleName = "kernel32.dll", FunctionName = "OpenEventW", Description = "打开事件", Category = ApiCategory.Synchronization },
            new() { Name = "SetEvent", ModuleName = "kernel32.dll", FunctionName = "SetEvent", Description = "设置事件", Category = ApiCategory.Synchronization },
            new() { Name = "ResetEvent", ModuleName = "kernel32.dll", FunctionName = "ResetEvent", Description = "重置事件", Category = ApiCategory.Synchronization },
            new() { Name = "WaitForSingleObject", ModuleName = "kernel32.dll", FunctionName = "WaitForSingleObject", Description = "等待单个对象", Category = ApiCategory.Synchronization },
            new() { Name = "WaitForMultipleObjects", ModuleName = "kernel32.dll", FunctionName = "WaitForMultipleObjects", Description = "等待多个对象", Category = ApiCategory.Synchronization },
            new() { Name = "WaitForSingleObjectEx", ModuleName = "kernel32.dll", FunctionName = "WaitForSingleObjectEx", Description = "等待单个对象Ex", Category = ApiCategory.Synchronization },
            new() { Name = "WaitForMultipleObjectsEx", ModuleName = "kernel32.dll", FunctionName = "WaitForMultipleObjectsEx", Description = "等待多个对象Ex", Category = ApiCategory.Synchronization },
            new() { Name = "CreateSemaphoreW", ModuleName = "kernel32.dll", FunctionName = "CreateSemaphoreW", Description = "创建信号量", Category = ApiCategory.Synchronization },
            new() { Name = "OpenSemaphoreW", ModuleName = "kernel32.dll", FunctionName = "OpenSemaphoreW", Description = "打开信号量", Category = ApiCategory.Synchronization },
            new() { Name = "ReleaseSemaphore", ModuleName = "kernel32.dll", FunctionName = "ReleaseSemaphore", Description = "释放信号量", Category = ApiCategory.Synchronization },
            new() { Name = "CreateWaitableTimerW", ModuleName = "kernel32.dll", FunctionName = "CreateWaitableTimerW", Description = "创建可等待计时器", Category = ApiCategory.Synchronization },
            new() { Name = "OpenWaitableTimerW", ModuleName = "kernel32.dll", FunctionName = "OpenWaitableTimerW", Description = "打开可等待计时器", Category = ApiCategory.Synchronization },
            new() { Name = "SetWaitableTimer", ModuleName = "kernel32.dll", FunctionName = "SetWaitableTimer", Description = "设置可等待计时器", Category = ApiCategory.Synchronization },
            new() { Name = "CancelWaitableTimer", ModuleName = "kernel32.dll", FunctionName = "CancelWaitableTimer", Description = "取消可等待计时器", Category = ApiCategory.Synchronization },
            new() { Name = "EnterCriticalSection", ModuleName = "kernel32.dll", FunctionName = "EnterCriticalSection", Description = "进入临界区", Category = ApiCategory.Synchronization },
            new() { Name = "LeaveCriticalSection", ModuleName = "kernel32.dll", FunctionName = "LeaveCriticalSection", Description = "离开临界区", Category = ApiCategory.Synchronization },
            new() { Name = "TryEnterCriticalSection", ModuleName = "kernel32.dll", FunctionName = "TryEnterCriticalSection", Description = "尝试进入临界区", Category = ApiCategory.Synchronization },
            new() { Name = "InitializeCriticalSection", ModuleName = "kernel32.dll", FunctionName = "InitializeCriticalSection", Description = "初始化临界区", Category = ApiCategory.Synchronization },
            new() { Name = "InitializeCriticalSectionAndSpinCount", ModuleName = "kernel32.dll", FunctionName = "InitializeCriticalSectionAndSpinCount", Description = "初始化临界区和自旋计数", Category = ApiCategory.Synchronization },
            new() { Name = "DeleteCriticalSection", ModuleName = "kernel32.dll", FunctionName = "DeleteCriticalSection", Description = "删除临界区", Category = ApiCategory.Synchronization },
            
            new() { Name = "CreateProcessWithLogonW", ModuleName = "advapi32.dll", FunctionName = "CreateProcessWithLogonW", Description = "使用登录创建进程", Category = ApiCategory.Security },
            new() { Name = "CreateProcessWithTokenW", ModuleName = "advapi32.dll", FunctionName = "CreateProcessWithTokenW", Description = "使用令牌创建进程", Category = ApiCategory.Security },
            new() { Name = "LogonUserW", ModuleName = "advapi32.dll", FunctionName = "LogonUserW", Description = "登录用户", Category = ApiCategory.Security },
            new() { Name = "ImpersonateLoggedOnUser", ModuleName = "advapi32.dll", FunctionName = "ImpersonateLoggedOnUser", Description = "模拟已登录用户", Category = ApiCategory.Security },
            new() { Name = "RevertToSelf", ModuleName = "advapi32.dll", FunctionName = "RevertToSelf", Description = "恢复到自身", Category = ApiCategory.Security },
            new() { Name = "DuplicateToken", ModuleName = "advapi32.dll", FunctionName = "DuplicateToken", Description = "复制令牌", Category = ApiCategory.Security },
            new() { Name = "DuplicateTokenEx", ModuleName = "advapi32.dll", FunctionName = "DuplicateTokenEx", Description = "复制令牌Ex", Category = ApiCategory.Security },
            new() { Name = "GetTokenInformation", ModuleName = "advapi32.dll", FunctionName = "GetTokenInformation", Description = "获取令牌信息", Category = ApiCategory.Security },
            new() { Name = "SetTokenInformation", ModuleName = "advapi32.dll", FunctionName = "SetTokenInformation", Description = "设置令牌信息", Category = ApiCategory.Security },
            new() { Name = "CheckTokenMembership", ModuleName = "advapi32.dll", FunctionName = "CheckTokenMembership", Description = "检查令牌成员资格", Category = ApiCategory.Security },
            new() { Name = "CryptProtectData", ModuleName = "crypt32.dll", FunctionName = "CryptProtectData", Description = "加密数据", Category = ApiCategory.Security },
            new() { Name = "CryptUnprotectData", ModuleName = "crypt32.dll", FunctionName = "CryptUnprotectData", Description = "解密数据", Category = ApiCategory.Security },
            new() { Name = "CryptAcquireContextW", ModuleName = "advapi32.dll", FunctionName = "CryptAcquireContextW", Description = "获取加密上下文", Category = ApiCategory.Security },
            new() { Name = "CryptReleaseContext", ModuleName = "advapi32.dll", FunctionName = "CryptReleaseContext", Description = "释放加密上下文", Category = ApiCategory.Security },
            new() { Name = "CryptGenKey", ModuleName = "advapi32.dll", FunctionName = "CryptGenKey", Description = "生成密钥", Category = ApiCategory.Security },
            new() { Name = "CryptDestroyKey", ModuleName = "advapi32.dll", FunctionName = "CryptDestroyKey", Description = "销毁密钥", Category = ApiCategory.Security },
            new() { Name = "CryptEncrypt", ModuleName = "advapi32.dll", FunctionName = "CryptEncrypt", Description = "加密", Category = ApiCategory.Security },
            new() { Name = "CryptDecrypt", ModuleName = "advapi32.dll", FunctionName = "CryptDecrypt", Description = "解密", Category = ApiCategory.Security },
            new() { Name = "NtOpenProcessToken", ModuleName = "ntdll.dll", FunctionName = "NtOpenProcessToken", Description = "打开进程令牌(原生)", Category = ApiCategory.Security },
            new() { Name = "NtOpenThreadToken", ModuleName = "ntdll.dll", FunctionName = "NtOpenThreadToken", Description = "打开线程令牌(原生)", Category = ApiCategory.Security },
            new() { Name = "NtQueryInformationToken", ModuleName = "ntdll.dll", FunctionName = "NtQueryInformationToken", Description = "查询令牌信息(原生)", Category = ApiCategory.Security },
            new() { Name = "NtSetInformationToken", ModuleName = "ntdll.dll", FunctionName = "NtSetInformationToken", Description = "设置令牌信息(原生)", Category = ApiCategory.Security },
            new() { Name = "NtAdjustPrivilegesToken", ModuleName = "ntdll.dll", FunctionName = "NtAdjustPrivilegesToken", Description = "调整令牌特权(原生)", Category = ApiCategory.Security },
            new() { Name = "NtAdjustGroupsToken", ModuleName = "ntdll.dll", FunctionName = "NtAdjustGroupsToken", Description = "调整令牌组(原生)", Category = ApiCategory.Security },
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
