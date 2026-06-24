using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Core.Logging;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void StopTimeSyncLoop()
        {
            var cts = _gatewayTimeSyncCts;
            if (cts != null)
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }

                cts.Dispose();
                _gatewayTimeSyncCts = null;
            }

            _gatewayTimeSyncTask = null;
            _state.GatewayRoomTimeSync.Reset();

            BattleFlowDebugProvider.TimeSyncStats = null;
            BattleFlowDebugProvider.TimeSyncStatsByWorld = null;
        }

        private void StartTimeSyncLoop()
        {
            if (_gatewayClient == null) return;
            if (_gatewayTimeSyncTask != null && !_gatewayTimeSyncTask.IsCompleted) return;

            _gatewayTimeSyncCts = new CancellationTokenSource();
            var token = _gatewayTimeSyncCts.Token;

            _gatewayTimeSyncTask = Task.Run(async () =>
            {
                var timeSync = GatewayTimeSyncHelper.ResolveRuntimeOptions(_plan.TimeSync);
                var alpha = timeSync.Alpha;
                var intervalMs = timeSync.IntervalMs;
                var opCode = timeSync.OpCode;
                var timeoutMs = timeSync.TimeoutMs;
                var failureCount = 0;
                const int NotifyThreshold = 3;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var t0 = Stopwatch.GetTimestamp();
                        var res = await _gatewayClient.TimeSyncAsync(timeSyncOpCode: opCode, clientSendTicks: t0, timeout: TimeSpan.FromMilliseconds(timeoutMs), cancellationToken: token);
                        var t2 = Stopwatch.GetTimestamp();

                        var sample = GatewayTimeSyncHelper.CalculateSample(
                            clientSendTicks: t0,
                            clientReceiveTicks: t2,
                            serverNowTicks: res.ServerNowTicks,
                            serverTickFrequency: res.ServerTickFrequency,
                            localTickFrequency: Stopwatch.Frequency);
                        var timeSyncState = _state.GatewayRoomTimeSync;
                        var ewma = GatewayTimeSyncHelper.ApplySample(
                            hasClockSync: timeSyncState.HasClockSync,
                            currentClockOffsetSecondsEwma: timeSyncState.ClockOffsetSecondsEwma,
                            currentRttSecondsEwma: timeSyncState.RttSecondsEwma,
                            currentSamples: timeSyncState.Samples,
                            sample: in sample,
                            alpha: alpha);

                        timeSyncState.HasClockSync = ewma.HasClockSync;
                        timeSyncState.ClockOffsetSecondsEwma = ewma.ClockOffsetSecondsEwma;
                        timeSyncState.RttSecondsEwma = ewma.RttSecondsEwma;
                        timeSyncState.Samples = ewma.Samples;

                        failureCount = 0;
                        BattleFlowDebugProvider.TimeSyncStats = BuildCurrentTimeSyncStats(opCode, intervalMs, alpha, timeoutMs);
                        UpdateTimeSyncStatsByWorld(opCode, intervalMs, alpha, timeoutMs);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        GatewaySessionFailurePolicy.LogTimeSyncFailure(ex, failureCount);
                        if (GatewaySessionFailurePolicy.ShouldNotifyTimeSyncFailure(ex, failureCount, NotifyThreshold))
                        {
                            _eventsCtrl.NotifySessionFailed(this, ex);
                        }
                    }

                    try
                    {
                        await Task.Delay(intervalMs, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

    }
}
