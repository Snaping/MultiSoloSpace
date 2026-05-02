using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface IProcessDetectorService
{
    event EventHandler<ProcessDetectedEventArgs>? ProcessDetected;
    event EventHandler<ProcessHiddenEventArgs>? HiddenProcessDetected;
    
    bool IsRunning { get; }
    
    List<ProcessInfo> GetAllProcesses();
    List<ProcessInfo> GetHiddenProcesses();
    List<ProcessInfo> GetProcessesByName(string processName);
    ProcessInfo? GetProcessById(int processId);
    
    void StartDetection();
    void StopDetection();
    
    void RefreshProcessList();
    void EnableDeepScan(bool enable);
    bool IsProcessHidden(int processId);
    
    List<ModuleInfo> GetProcessModules(int processId);
    List<ThreadInfo> GetProcessThreads(int processId);
    
    ProcessInfo? GetProcessFromPeb(int processId);
    List<ProcessInfo> EnumProcessesViaNtQuerySystemInformation();
    List<ProcessInfo> EnumProcessesViaPsSetCreateProcessNotifyRoutine();
}

public class ProcessDetectedEventArgs : EventArgs
{
    public ProcessInfo ProcessInfo { get; set; } = new();
    public DetectionMethod DetectionMethod { get; set; }
    public DateTime DetectionTime { get; set; } = DateTime.Now;
}

public class ProcessHiddenEventArgs : EventArgs
{
    public ProcessInfo ProcessInfo { get; set; } = new();
    public HidingMethod HidingMethod { get; set; }
    public DateTime DetectionTime { get; set; } = DateTime.Now;
}

public enum DetectionMethod
{
    CreateToolhelp32Snapshot,
    NtQuerySystemInformation,
    PebWalk,
    PsSetCreateProcessNotifyRoutine,
    Etw,
    Wmi,
    Unknown
}

public enum HidingMethod
{
    UnlinkFromActiveProcessLinks,
    Dkcom,
    DirectKernelObjectModification,
    SsdtHook,
    ShadowSsdtHook,
    IrpHook,
    Minifilter,
    Rootkit,
    Unknown
}
