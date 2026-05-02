using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public class FilterService : IFilterService
{
    private readonly HashSet<string> _includeFilters = new();
    private readonly HashSet<string> _excludeFilters = new();
    private readonly object _lock = new();

    public event EventHandler<FilterChangedEventArgs>? FilterChanged;

    public void AddIncludeFilter(string apiName)
    {
        lock (_lock)
        {
            if (_includeFilters.Add(apiName))
            {
                OnFilterChanged(new FilterChangedEventArgs
                {
                    FilterName = apiName,
                    Action = FilterAction.Added
                });
            }
        }
    }

    public void AddExcludeFilter(string apiName)
    {
        lock (_lock)
        {
            if (_excludeFilters.Add(apiName))
            {
                OnFilterChanged(new FilterChangedEventArgs
                {
                    FilterName = apiName,
                    Action = FilterAction.Added
                });
            }
        }
    }

    public void RemoveIncludeFilter(string apiName)
    {
        lock (_lock)
        {
            if (_includeFilters.Remove(apiName))
            {
                OnFilterChanged(new FilterChangedEventArgs
                {
                    FilterName = apiName,
                    Action = FilterAction.Removed
                });
            }
        }
    }

    public void RemoveExcludeFilter(string apiName)
    {
        lock (_lock)
        {
            if (_excludeFilters.Remove(apiName))
            {
                OnFilterChanged(new FilterChangedEventArgs
                {
                    FilterName = apiName,
                    Action = FilterAction.Removed
                });
            }
        }
    }

    public void ClearFilters()
    {
        lock (_lock)
        {
            _includeFilters.Clear();
            _excludeFilters.Clear();
            OnFilterChanged(new FilterChangedEventArgs
            {
                FilterName = string.Empty,
                Action = FilterAction.Cleared
            });
        }
    }

    public bool ShouldCapture(ApiCallInfo callInfo)
    {
        lock (_lock)
        {
            if (_excludeFilters.Contains(callInfo.ApiName))
            {
                return false;
            }

            if (_includeFilters.Count > 0)
            {
                return _includeFilters.Contains(callInfo.ApiName);
            }

            return true;
        }
    }

    public List<string> GetIncludeFilters()
    {
        lock (_lock)
        {
            return new List<string>(_includeFilters);
        }
    }

    public List<string> GetExcludeFilters()
    {
        lock (_lock)
        {
            return new List<string>(_excludeFilters);
        }
    }

    protected virtual void OnFilterChanged(FilterChangedEventArgs e)
    {
        FilterChanged?.Invoke(this, e);
    }
}
