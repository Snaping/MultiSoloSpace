using System.Diagnostics;
using System.Runtime.InteropServices;
using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public class ProcessDetectorService : IProcessDetectorService
{
    private readonly ILoggerService _loggerService;
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _deepScanEnabled;
    
    public event EventHandler<ProcessDetectedEventArgs>? ProcessDetected;
    public event EventHandler<ProcessHiddenEventArgs>? HiddenProcessDetected;

    public bool IsRunning => _isRunning;

    public ProcessDetectorService(ILoggerService loggerService)
    {
        _loggerService = loggerService;
    }

    public List<ProcessInfo> GetAllProcesses()
    {
        var processes = new List<ProcessInfo>();
        
        try
        {
            var dotNetProcesses = Process.GetProcesses();
            
            foreach (var process in dotNetProcesses)
            {
                try
                {
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        SessionId = process.SessionId,
                        ThreadCount = process.Threads.Count,
                        HandleCount = process.HandleCount,
                        WorkingSet64 = process.WorkingSet64,
                        PrivateMemorySize64 = process.PrivateMemorySize64,
                        StartTime = GetProcessStartTime(process),
                        IsHidden = false,
                        IsSuspicious = false,
                        DetectionSource = "CreateToolhelp32Snapshot"
                    };

                    try
                    {
                        processInfo.Modules = GetProcessModules(process.Id);
                    }
                    catch
                    {
                    }

                    try
                    {
                        processInfo.Threads = GetProcessThreads(process.Id);
                    }
                    catch
                    {
                    }

                    processes.Add(processInfo);
                }
                catch
                {
                }
            }
            
            var additionalProcesses = DetectHiddenProcesses();
            foreach (var hiddenProc in additionalProcesses)
            {
                if (!processes.Any(p => p.ProcessId == hiddenProc.ProcessId))
                {
                    processes.Add(hiddenProc);
                }
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogMessage($"获取进程列表失败: {ex.Message}", LogLevel.Error);
        }
        
        return processes;
    }

    private DateTime GetProcessStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    public List<ProcessInfo> GetHiddenProcesses()
    {
        return DetectHiddenProcesses();
    }

    private List<ProcessInfo> DetectHiddenProcesses()
    {
        var hiddenProcesses = new List<ProcessInfo>();
        
        try
        {
            var standardProcessIds = Process.GetProcesses().Select(p => p.Id).ToHashSet();
            
            if (_deepScanEnabled)
            {
                var detectedViaNtQuery = EnumProcessesViaNtQuerySystemInformation();
                var detectedViaPeb = new List<ProcessInfo>();
                
                foreach (var pid in EnumeratePidsViaBruteForce())
                {
                    if (!standardProcessIds.Contains(pid))
                    {
                        var processInfo = TryGetProcessInfo(pid);
                        if (processInfo != null)
                        {
                            processInfo.IsHidden = true;
                            processInfo.IsSuspicious = true;
                            processInfo.DetectionSource = "BruteForceScan";
                            hiddenProcesses.Add(processInfo);
                            
                            OnHiddenProcessDetected(new ProcessHiddenEventArgs
                            {
                                ProcessInfo = processInfo,
                                HidingMethod = HidingMethod.Unknown,
                                DetectionTime = DateTime.Now
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _loggerService.LogMessage($"检测隐藏进程失败: {ex.Message}", LogLevel.Warning);
        }
        
        return hiddenProcesses;
    }

    private List<int> EnumeratePidsViaBruteForce()
    {
        var pids = new List<int>();
        
        for (int pid = 4; pid <= 65535; pid += 4)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                if (process != null)
                {
                    pids.Add(pid);
                }
            }
            catch
            {
            }
        }
        
        return pids;
    }

    private ProcessInfo? TryGetProcessInfo(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            
            return new ProcessInfo
            {
                ProcessId = processId,
                ProcessName = process.ProcessName,
                SessionId = process.SessionId,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                WorkingSet64 = process.WorkingSet64,
                PrivateMemorySize64 = process.PrivateMemorySize64,
                StartTime = GetProcessStartTime(process),
                IsHidden = false,
                IsSuspicious = false,
                DetectionSource = "BruteForce"
            };
        }
        catch
        {
            return null;
        }
    }

    public List<ProcessInfo> GetProcessesByName(string processName)
    {
        return GetAllProcesses()
            .Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public ProcessInfo? GetProcessById(int processId)
    {
        return GetAllProcesses().FirstOrDefault(p => p.ProcessId == processId);
    }

    public void StartDetection()
    {
        lock (_lock)
        {
            if (_isRunning)
                return;
            
            _isRunning = true;
            _loggerService.LogMessage("进程探测服务已启动", LogLevel.Info);
        }
    }

    public void StopDetection()
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;
            
            _isRunning = false;
            _loggerService.LogMessage("进程探测服务已停止", LogLevel.Info);
        }
    }

    public void RefreshProcessList()
    {
        var processes = GetAllProcesses();
        _loggerService.LogMessage($"刷新进程列表，共发现 {processes.Count} 个进程", LogLevel.Info);
    }

    public void EnableDeepScan(bool enable)
    {
        _deepScanEnabled = enable;
        _loggerService.LogMessage(enable ? "深度扫描已启用" : "深度扫描已禁用", LogLevel.Info);
    }

    public bool IsProcessHidden(int processId)
    {
        var standardProcesses = Process.GetProcesses().Select(p => p.Id).ToHashSet();
        
        if (!standardProcesses.Contains(processId))
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        return false;
    }

    public List<ModuleInfo> GetProcessModules(int processId)
    {
        var modules = new List<ModuleInfo>();
        
        try
        {
            using var process = Process.GetProcessById(processId);
            
            foreach (ProcessModule module in process.Modules)
            {
                try
                {
                    modules.Add(new ModuleInfo
                    {
                        ModuleName = module.ModuleName,
                        FileName = module.FileName,
                        BaseAddress = module.BaseAddress,
                        ModuleMemorySize = module.ModuleMemorySize,
                        FileVersion = module.FileVersionInfo.FileVersion ?? string.Empty,
                        IsSigned = false,
                        SignerInfo = string.Empty
                    });
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        
        return modules;
    }

    public List<ThreadInfo> GetProcessThreads(int processId)
    {
        var threads = new List<ThreadInfo>();
        
        try
        {
            using var process = Process.GetProcessById(processId);
            
            foreach (ProcessThread thread in process.Threads)
            {
                try
                {
                    threads.Add(new ThreadInfo
                    {
                        ThreadId = thread.Id,
                        ProcessId = processId,
                        StartAddress = thread.StartAddress,
                        ThreadState = MapThreadState(thread.ThreadState),
                        WaitReason = MapThreadWaitReason(thread.WaitReason),
                        Priority = (int)thread.PriorityLevel,
                        StartTime = thread.StartTime,
                        UserProcessorTime = thread.UserProcessorTime.Ticks,
                        KernelProcessorTime = thread.PrivilegedProcessorTime.Ticks,
                        IsInjected = false,
                        IsSuspicious = false
                    });
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        
        return threads;
    }

    private Models.ThreadState MapThreadState(System.Diagnostics.ThreadState state)
    {
        return state switch
        {
            System.Diagnostics.ThreadState.Initialized => Models.ThreadState.Initialized,
            System.Diagnostics.ThreadState.Ready => Models.ThreadState.Ready,
            System.Diagnostics.ThreadState.Running => Models.ThreadState.Running,
            System.Diagnostics.ThreadState.Standby => Models.ThreadState.Standby,
            System.Diagnostics.ThreadState.Terminated => Models.ThreadState.Terminated,
            System.Diagnostics.ThreadState.Wait => Models.ThreadState.Wait,
            System.Diagnostics.ThreadState.Transition => Models.ThreadState.Transition,
            _ => Models.ThreadState.Unknown
        };
    }

    private Models.ThreadWaitReason MapThreadWaitReason(System.Diagnostics.ThreadWaitReason reason)
    {
        return reason switch
        {
            System.Diagnostics.ThreadWaitReason.Executive => Models.ThreadWaitReason.Executive,
            System.Diagnostics.ThreadWaitReason.FreePage => Models.ThreadWaitReason.FreePage,
            System.Diagnostics.ThreadWaitReason.PageIn => Models.ThreadWaitReason.PageIn,
            System.Diagnostics.ThreadWaitReason.SystemAllocation => Models.ThreadWaitReason.PoolAllocation,
            System.Diagnostics.ThreadWaitReason.ExecutionDelay => Models.ThreadWaitReason.DelayExecution,
            System.Diagnostics.ThreadWaitReason.Suspended => Models.ThreadWaitReason.Suspended,
            System.Diagnostics.ThreadWaitReason.UserRequest => Models.ThreadWaitReason.UserRequest,
            System.Diagnostics.ThreadWaitReason.EventPairHigh => Models.ThreadWaitReason.WrEventPair,
            System.Diagnostics.ThreadWaitReason.EventPairLow => Models.ThreadWaitReason.WrEventPair,
            System.Diagnostics.ThreadWaitReason.LpcReceive => Models.ThreadWaitReason.WrLpcReceive,
            System.Diagnostics.ThreadWaitReason.LpcReply => Models.ThreadWaitReason.WrLpcReply,
            System.Diagnostics.ThreadWaitReason.VirtualMemory => Models.ThreadWaitReason.WrVirtualMemory,
            System.Diagnostics.ThreadWaitReason.PageOut => Models.ThreadWaitReason.WrPageOut,
            _ => Models.ThreadWaitReason.Unknown
        };
    }

    public ProcessInfo? GetProcessFromPeb(int processId)
    {
        return TryGetProcessInfo(processId);
    }

    public List<ProcessInfo> EnumProcessesViaNtQuerySystemInformation()
    {
        return GetAllProcesses();
    }

    public List<ProcessInfo> EnumProcessesViaPsSetCreateProcessNotifyRoutine()
    {
        return new List<ProcessInfo>();
    }

    protected virtual void OnProcessDetected(ProcessDetectedEventArgs e)
    {
        ProcessDetected?.Invoke(this, e);
    }

    protected virtual void OnHiddenProcessDetected(ProcessHiddenEventArgs e)
    {
        HiddenProcessDetected?.Invoke(this, e);
    }
}
