using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface IProcessHidingService
{
    event EventHandler<ProcessHidingEventArgs>? ProcessHidden;
    event EventHandler<ProcessHidingEventArgs>? ProcessUnhidden;
    
    bool IsDriverAvailable { get; }
    
    bool HideProcess(int processId, HidingMethod method = HidingMethod.UnlinkFromActiveProcessLinks);
    bool UnhideProcess(int processId);
    bool IsProcessHidden(int processId);
    
    List<int> GetHiddenProcessIds();
    List<ProcessInfo> GetHiddenProcesses();
    
    bool HideProcessFromTaskManager(int processId);
    bool HideProcessFromProcessExplorer(int processId);
    bool HideProcessFromWmi(int processId);
    bool HideProcessFromEtw(int processId);
    
    bool ProtectProcess(int processId);
    bool UnprotectProcess(int processId);
    bool IsProcessProtected(int processId);
    
    bool ElevateProcessPrivileges(int processId);
    bool SetProcessCritical(int processId, bool isCritical);
    
    bool InjectDll(int processId, string dllPath);
    bool UninjectDll(int processId, string dllPath);
    
    bool HookProcessApi(int processId, string apiName, IntPtr hookAddress);
    bool UnhookProcessApi(int processId, string apiName);
    
    bool SetProcessAffinity(int processId, IntPtr affinityMask);
    bool SetProcessPriority(int processId, ProcessPriority priority);
    
    bool SuspendProcess(int processId);
    bool ResumeProcess(int processId);
    bool TerminateProcess(int processId, uint exitCode = 0);
    
    bool EnableProcessMonitoring(int processId);
    bool DisableProcessMonitoring(int processId);
}

public class ProcessHidingEventArgs : EventArgs
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public HidingMethod Method { get; set; }
    public DateTime EventTime { get; set; } = DateTime.Now;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum ProcessPriority
{
    Idle = 0x40,
    BelowNormal = 0x4000,
    Normal = 0x20,
    AboveNormal = 0x8000,
    High = 0x80,
    RealTime = 0x100
}
