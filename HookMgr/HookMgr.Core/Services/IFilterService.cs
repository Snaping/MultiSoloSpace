using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface IFilterService
{
    event EventHandler<FilterChangedEventArgs>? FilterChanged;
    
    void AddIncludeFilter(string apiName);
    void AddExcludeFilter(string apiName);
    void RemoveIncludeFilter(string apiName);
    void RemoveExcludeFilter(string apiName);
    void ClearFilters();
    bool ShouldCapture(ApiCallInfo callInfo);
    List<string> GetIncludeFilters();
    List<string> GetExcludeFilters();
}

public class FilterChangedEventArgs : EventArgs
{
    public string FilterName { get; set; } = string.Empty;
    public FilterAction Action { get; set; }
}

public enum FilterAction
{
    Added,
    Removed,
    Cleared
}
