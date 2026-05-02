namespace HookMgr.Core.Models;

public class ApiCallInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string ApiName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string OriginalReturnValue { get; set; } = string.Empty;
    public string ModifiedReturnValue { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public bool IsFiltered { get; set; }
    public bool IsRedirected { get; set; }
    public string RedirectTarget { get; set; } = string.Empty;
    public string CallStack { get; set; } = string.Empty;
}
