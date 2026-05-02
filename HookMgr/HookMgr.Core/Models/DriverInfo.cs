namespace HookMgr.Core.Models;

public class DriverInfo
{
    public string DriverName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public IntPtr DriverBase { get; set; }
    public int DriverSize { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DriverType DriverType { get; set; }
    public DriverStartType StartType { get; set; }
    public DriverState State { get; set; }
    public bool IsLoaded { get; set; }
    public bool IsSigned { get; set; }
    public string SignerInfo { get; set; } = string.Empty;
    public string FileVersion { get; set; } = string.Empty;
    public DateTime LoadTime { get; set; }
    public bool IsHooked { get; set; }
    public List<HookedFunctionInfo> HookedFunctions { get; set; } = new();
}

public class HookedFunctionInfo
{
    public string FunctionName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public IntPtr OriginalAddress { get; set; }
    public IntPtr HookedAddress { get; set; }
    public HookType HookType { get; set; }
    public bool IsActive { get; set; }
    public DateTime HookTime { get; set; }
    public string HookOwner { get; set; } = string.Empty;
}

public enum DriverType
{
    KernelDriver,
    FileSystemDriver,
    LegacyDriver,
    MinifilterDriver,
    NetworkDriver,
    DisplayDriver,
    Unknown
}

public enum DriverStartType
{
    Boot,
    System,
    Auto,
    Demand,
    Disabled
}

public enum DriverState
{
    Stopped,
    StartPending,
    StopPending,
    Running,
    ContinuePending,
    PausePending,
    Paused
}

public enum HookType
{
    InlineHook,
    IatHook,
    EatHook,
    SsdtHook,
    ShadowSsdtHook,
    IrpHook,
    MinifilterHook,
    Unknown
}

public class KernelHookInfo
{
    public HookType HookType { get; set; }
    public string TargetModule { get; set; } = string.Empty;
    public string TargetFunction { get; set; } = string.Empty;
    public IntPtr TargetAddress { get; set; }
    public IntPtr HookAddress { get; set; }
    public IntPtr OriginalAddress { get; set; }
    public bool IsEnabled { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> MonitoredProcesses { get; set; } = new();
    public bool IsSystemWide { get; set; }
}

public class SsdtEntryInfo
{
    public int Index { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public IntPtr Address { get; set; }
    public IntPtr CurrentAddress { get; set; }
    public bool IsHooked { get; set; }
    public string HookOwner { get; set; } = string.Empty;
    public int CallCount { get; set; }
}

public class IrpHookInfo
{
    public byte MajorFunction { get; set; }
    public string MajorFunctionName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public IntPtr OriginalHandler { get; set; }
    public IntPtr HookedHandler { get; set; }
    public bool IsHooked { get; set; }
    public int IrpCount { get; set; }
}
