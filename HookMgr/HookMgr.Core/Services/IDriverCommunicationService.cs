using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface IDriverCommunicationService
{
    event EventHandler<DriverEventArgs>? DriverLoaded;
    event EventHandler<DriverEventArgs>? DriverUnloaded;
    event EventHandler<DriverHookEventArgs>? HookInstalled;
    event EventHandler<DriverHookEventArgs>? HookRemoved;
    
    bool IsDriverLoaded { get; }
    string DriverName { get; }
    string DriverPath { get; }
    
    bool LoadDriver(string driverPath, string driverName);
    bool UnloadDriver(string driverName);
    bool StartDriver(string driverName);
    bool StopDriver(string driverName);
    
    List<DriverInfo> GetLoadedDrivers();
    List<DriverInfo> GetAllDrivers();
    DriverInfo? GetDriverByName(string driverName);
    
    bool InstallKernelHook(KernelHookInfo hookInfo);
    bool RemoveKernelHook(string hookId);
    bool EnableKernelHook(string hookId);
    bool DisableKernelHook(string hookId);
    List<KernelHookInfo> GetInstalledHooks();
    
    bool InstallSsdtHook(int index, IntPtr hookAddress, out IntPtr originalAddress);
    bool RemoveSsdtHook(int index);
    List<SsdtEntryInfo> GetSsdtEntries();
    List<SsdtEntryInfo> GetShadowSsdtEntries();
    
    bool InstallIrpHook(string driverName, byte majorFunction, IntPtr hookHandler, out IntPtr originalHandler);
    bool RemoveIrpHook(string driverName, byte majorFunction);
    List<IrpHookInfo> GetIrpHooks(string driverName);
    
    bool SendIoControl(uint ioControlCode, byte[]? inputBuffer, out byte[]? outputBuffer);
    bool RegisterCallback(CallbackType callbackType, Delegate callback);
    bool UnregisterCallback(CallbackType callbackType);
    
    bool HideProcess(int processId);
    bool UnhideProcess(int processId);
    List<int> GetHiddenProcesses();
    
    bool HideFile(string filePath);
    bool UnhideFile(string filePath);
    
    bool HideRegistryKey(string keyPath);
    bool UnhideRegistryKey(string keyPath);
}

public class DriverEventArgs : EventArgs
{
    public DriverInfo DriverInfo { get; set; } = new();
    public DateTime EventTime { get; set; } = DateTime.Now;
}

public class DriverHookEventArgs : EventArgs
{
    public KernelHookInfo HookInfo { get; set; } = new();
    public HookAction Action { get; set; }
    public DateTime EventTime { get; set; } = DateTime.Now;
}

public enum HookAction
{
    Installed,
    Removed,
    Enabled,
    Disabled
}

public enum CallbackType
{
    ProcessCreate,
    ProcessExit,
    ThreadCreate,
    ThreadExit,
    ImageLoad,
    RegistryCallback,
    FileIoCallback,
    NetworkCallback
}
