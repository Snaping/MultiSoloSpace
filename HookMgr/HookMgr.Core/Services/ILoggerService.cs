using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface ILoggerService
{
    string LogFilePath { get; }
    
    void LogApiCall(ApiCallInfo callInfo);
    void LogMessage(string message, LogLevel level = LogLevel.Info);
    List<ApiCallInfo> GetLoggedCalls(DateTime? from = null, DateTime? to = null);
    void ClearLog();
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
