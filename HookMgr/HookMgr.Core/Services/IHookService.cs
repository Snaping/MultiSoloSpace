using HookMgr.Core.Models;

namespace HookMgr.Core.Services;

public interface IHookService
{
    event EventHandler<ApiCallInfo>? ApiCallCaptured;
    
    bool IsHooking { get; }
    int TargetProcessId { get; }
    
    void StartHooking(int processId);
    void StopHooking();
    void EnableApiHook(string apiName);
    void DisableApiHook(string apiName);
    List<ApiDefinition> GetAvailableApis();
    void ModifyReturnValue(Guid callId, string newValue);
    void SetRedirect(string apiName, string targetFunction);
    void RemoveRedirect(string apiName);
    void SimulateApiCall(string apiName, string parameters, string returnValue);
}
