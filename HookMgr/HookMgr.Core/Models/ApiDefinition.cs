namespace HookMgr.Core.Models;

public class ApiDefinition
{
    public string Name { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public ApiCategory Category { get; set; } = ApiCategory.Other;
}

public enum ApiCategory
{
    FileSystem,
    Registry,
    Network,
    Process,
    Thread,
    Memory,
    Window,
    Other
}
