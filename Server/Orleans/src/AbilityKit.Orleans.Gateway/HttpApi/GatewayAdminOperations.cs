namespace AbilityKit.Orleans.Gateway.HttpApi;

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;

internal static class GatewayAdminOperations
{
    private static readonly object SyncRoot = new();
    private static bool maintenanceMode;
    private static bool drainMode;
    private static bool restartRequested;
    private static string? lastOperationId;
    private static string? lastOperationReason;
    private static string? lastOperationRequestedBy;
    private static long? lastOperationRequestedAtTicks;

    public static AdminServerStatusHttpResponse GetStatus(IWebHostEnvironment environment)
    {
        var process = Process.GetCurrentProcess();
        var now = DateTime.UtcNow;
        bool maintenance;
        bool drain;
        bool restart;
        string? operationId;
        string? reason;
        string? requestedBy;
        long? requestedAt;

        lock (SyncRoot)
        {
            maintenance = maintenanceMode;
            drain = drainMode;
            restart = restartRequested;
            operationId = lastOperationId;
            reason = lastOperationReason;
            requestedBy = lastOperationRequestedBy;
            requestedAt = lastOperationRequestedAtTicks;
        }

        var startTimeUtc = process.StartTime.ToUniversalTime();
        return new AdminServerStatusHttpResponse(
            environment.EnvironmentName,
            environment.ApplicationName,
            Environment.MachineName,
            Environment.ProcessId,
            process.ProcessName,
            startTimeUtc.Ticks,
            Math.Max(0, (long)(now - startTimeUtc).TotalSeconds),
            process.WorkingSet64,
            GC.GetTotalMemory(forceFullCollection: false),
            process.Threads.Count,
            maintenance,
            drain,
            restart,
            operationId,
            reason,
            requestedBy,
            requestedAt,
            now.Ticks);
    }

    public static AdminServerOperationHttpResponse SetMaintenanceMode(AdminServerOperationHttpRequest request, string requestedBy, IWebHostEnvironment environment)
    {
        return Apply("maintenance", request, requestedBy, environment, () => maintenanceMode = request.Enabled);
    }

    public static AdminServerOperationHttpResponse SetDrainMode(AdminServerOperationHttpRequest request, string requestedBy, IWebHostEnvironment environment)
    {
        return Apply("drain", request, requestedBy, environment, () => drainMode = request.Enabled);
    }

    public static AdminServerOperationHttpResponse RequestRestart(AdminServerOperationHttpRequest request, string requestedBy, IWebHostEnvironment environment)
    {
        return Apply("restart-request", request, requestedBy, environment, () =>
        {
            restartRequested = true;
            maintenanceMode = true;
            drainMode = true;
        });
    }

    private static AdminServerOperationHttpResponse Apply(string operation, AdminServerOperationHttpRequest request, string requestedBy, IWebHostEnvironment environment, Action mutate)
    {
        var operationId = string.IsNullOrWhiteSpace(request.OperationId) ? Guid.NewGuid().ToString("N") : request.OperationId;
        lock (SyncRoot)
        {
            mutate();
            lastOperationId = operationId;
            lastOperationReason = request.Reason;
            lastOperationRequestedBy = requestedBy;
            lastOperationRequestedAtTicks = DateTime.UtcNow.Ticks;
        }

        return new AdminServerOperationHttpResponse(
            true,
            operation,
            operationId,
            requestedBy,
            request.Reason,
            GetStatus(environment));
    }
}
