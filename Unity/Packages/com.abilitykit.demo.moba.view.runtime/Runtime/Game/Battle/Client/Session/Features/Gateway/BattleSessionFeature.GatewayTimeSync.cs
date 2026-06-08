using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Core.Common.Log;

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
                var alpha = _plan.TimeSyncAlpha;
                if (alpha < 0) alpha = 0;
                if (alpha > 1) alpha = 1;

                var intervalMs = _plan.TimeSyncIntervalMs;
                if (intervalMs <= 0) intervalMs = 1000;

                var opCode = _plan.TimeSyncOpCode;
                var timeoutMs = _plan.TimeSyncTimeoutMs;
                if (timeoutMs <= 0) timeoutMs = 2000;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var t0 = Stopwatch.GetTimestamp();
                        var res = await _gatewayClient.TimeSyncAsync(timeSyncOpCode: opCode, clientSendTicks: t0, timeout: TimeSpan.FromMilliseconds(timeoutMs), cancellationToken: token);
                        var t2 = Stopwatch.GetTimestamp();

                        var localFreq = (double)Stopwatch.Frequency;
                        var rtt = (t2 - t0) / localFreq;
                        if (rtt < 0) rtt = 0;

                        var serverNowSeconds = res.ServerNowTicks / (double)res.ServerTickFrequency;
                        var localNowSeconds = t2 / localFreq;
                        var serverNowEstimatedAtReceive = serverNowSeconds + (rtt * 0.5);
                        var offsetSeconds = localNowSeconds - serverNowEstimatedAtReceive;

                        if (!_state.GatewayRoomTimeSync.HasClockSync)
                        {
                            _state.GatewayRoomTimeSync.HasClockSync = true;
                            _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma = offsetSeconds;
                            _state.GatewayRoomTimeSync.RttSecondsEwma = rtt;
                            _state.GatewayRoomTimeSync.Samples = 1;
                        }
                        else
                        {
                            _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma = (alpha * offsetSeconds) + ((1.0 - alpha) * _state.GatewayRoomTimeSync.ClockOffsetSecondsEwma);
                            _state.GatewayRoomTimeSync.RttSecondsEwma = (alpha * rtt) + ((1.0 - alpha) * _state.GatewayRoomTimeSync.RttSecondsEwma);
                            _state.GatewayRoomTimeSync.Samples++;
                        }

                        BattleFlowDebugProvider.TimeSyncStats = BuildCurrentTimeSyncStats(opCode, intervalMs, alpha, timeoutMs);
                        UpdateTimeSyncStatsByWorld(opCode, intervalMs, alpha, timeoutMs);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "[BattleSessionFeature] TimeSync loop error");
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
