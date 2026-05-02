namespace HookMgr.Core.Models;

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string CommandLine { get; set; } = string.Empty;
    public int ParentProcessId { get; set; }
    public int SessionId { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public long WorkingSet64 { get; set; }
    public long PrivateMemorySize64 { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSuspicious { get; set; }
    public string DetectionSource { get; set; } = string.Empty;
    public List<ModuleInfo> Modules { get; set; } = new();
    public List<ThreadInfo> Threads { get; set; } = new();
}

public class ModuleInfo
{
    public string ModuleName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public IntPtr BaseAddress { get; set; }
    public int ModuleMemorySize { get; set; }
    public string FileVersion { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
    public string SignerInfo { get; set; } = string.Empty;
}

public class ThreadInfo
{
    public int ThreadId { get; set; }
    public int ProcessId { get; set; }
    public IntPtr StartAddress { get; set; }
    public ThreadState ThreadState { get; set; }
    public ThreadWaitReason WaitReason { get; set; }
    public int Priority { get; set; }
    public DateTime StartTime { get; set; }
    public long UserProcessorTime { get; set; }
    public long KernelProcessorTime { get; set; }
    public bool IsInjected { get; set; }
    public bool IsSuspicious { get; set; }
}

public enum ThreadState
{
    Initialized,
    Ready,
    Running,
    Standby,
    Terminated,
    Wait,
    Transition,
    Unknown
}

public enum ThreadWaitReason
{
    Executive,
    FreePage,
    PageIn,
    PoolAllocation,
    DelayExecution,
    Suspended,
    UserRequest,
    WrExecutive,
    WrFreePage,
    WrPageIn,
    WrPoolAllocation,
    WrDelayExecution,
    WrSuspended,
    WrUserRequest,
    WrEventPair,
    WrQueue,
    WrLpcReceive,
    WrLpcReply,
    WrVirtualMemory,
    WrPageOut,
    WrRendezvous,
    WrKeyedEvent,
    WrTerminated,
    WrProcessInSwap,
    WrCpuRateControl,
    WrCalloutStack,
    WrKernel,
    WrResource,
    WrPushLock,
    WrMutex,
    WrQuantumEnd,
    WrDispatchInt,
    WrPreempted,
    WrYieldExecution,
    WrFastMutex,
    WrGuardedMutex,
    WrRundown,
    WrAlertByThreadId,
    WrDeferredPreempt,
    WrPhysicalFault,
    WrIoRing,
    WrMdlCache,
    Unknown
}
