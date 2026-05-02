using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public class FileLoggerService : ILoggerService
{
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly ConcurrentBag<ApiCallInfo> _loggedCalls;
    private readonly object _fileLock = new();

    public string LogFilePath => Path.Combine(_logDirectory, _logFileName);

    public FileLoggerService()
    {
        _logDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _logFileName = $"api_calls_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        _loggedCalls = new ConcurrentBag<ApiCallInfo>();
    }

    public void LogApiCall(ApiCallInfo callInfo)
    {
        _loggedCalls.Add(callInfo);
        
        var logEntry = new
        {
            Timestamp = callInfo.Timestamp.ToString("o"),
            ApiName = callInfo.ApiName,
            ModuleName = callInfo.ModuleName,
            ProcessId = callInfo.ProcessId,
            ProcessName = callInfo.ProcessName,
            Parameters = callInfo.Parameters,
            OriginalReturnValue = callInfo.OriginalReturnValue,
            ModifiedReturnValue = callInfo.ModifiedReturnValue,
            IsModified = callInfo.IsModified,
            IsFiltered = callInfo.IsFiltered,
            IsRedirected = callInfo.IsRedirected
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
        
        lock (_fileLock)
        {
            File.AppendAllText(LogFilePath, json + Environment.NewLine);
        }
    }

    public void LogMessage(string message, LogLevel level = LogLevel.Info)
    {
        var logEntry = new
        {
            Timestamp = DateTime.Now.ToString("o"),
            Level = level.ToString(),
            Message = message
        };

        var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = false });
        
        lock (_fileLock)
        {
            File.AppendAllText(LogFilePath, json + Environment.NewLine);
        }
    }

    public List<ApiCallInfo> GetLoggedCalls(DateTime? from = null, DateTime? to = null)
    {
        var query = _loggedCalls.AsEnumerable();
        
        if (from.HasValue)
            query = query.Where(c => c.Timestamp >= from.Value);
        
        if (to.HasValue)
            query = query.Where(c => c.Timestamp <= to.Value);
        
        return query.OrderByDescending(c => c.Timestamp).ToList();
    }

    public void ClearLog()
    {
        _loggedCalls.Clear();
        lock (_fileLock)
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }
        }
    }
}
