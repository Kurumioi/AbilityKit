using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.GameFramework.Network;
using AbilityKit.Network.Runtime;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;

internal static class ShooterSmokeClientProcessRunner
{
    public static async Task<ShooterSmokeClientProcessResult> RunAsync(ShooterSmokeClientProcessOptions options)
    {
        if (options.Mode == ShooterSmokeClientProcessMode.Join && string.IsNullOrWhiteSpace(options.RoomId))
        {
            throw new ArgumentException("roomId is required for join client mode.", nameof(options));
        }

        await ShooterSmokeScenarioBase.WaitForTcpAsync(options.Host, options.Port, options.Timeout);

        using var replay = ShooterSmokeReplayRecordScope.CreateInputStateReplay(options.InputStateReplayOutputPath, in options);
        using var channel = new SmokeTcpGameFrameworkNetworkChannel($"ShooterSmokeGateway-{options.ClientId}", options.NetworkCondition.Normalize());
        using var connection = GameFrameworkGatewayConnectionFactory.Wrap(channel);
        using var launcher = new ShooterClientNetworkLauncher(connection);

        connection.Open(options.Host, options.Port);
        connection.Tick(0f);

        var login = await ShooterSmokeScenarioBase.LoginGuestAsync(connection);
        var presentationContext = ShooterSmokeScenarioBase.CreatePresentationContext();
        var runtime = presentationContext.Runtime;
        var presentation = presentationContext.Presentation;
        var session = presentationContext.Session;
        var start = CreateStartGame(options.Seed);

        var pushWait = new TaskCompletionSource<ShooterSnapshotPushSmokeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pushCount = 0;
        var lastPush = default(ShooterSnapshotPushSmokeResult);
        launcher.GatewayConnection.SnapshotPushDispatched += (opCode, payload, result) =>
        {
            try
            {
                if (TryCaptureSnapshotPush(opCode, payload, result, out var pushResult))
                {
                    replay?.RecordSnapshot(in pushResult, payload);
                    pushCount++;
                    lastPush = pushResult;
                    if (IsAppliedSnapshotResult(result))
                    {
                        pushWait.TrySetResult(pushResult);
                    }
                }
            }
            catch (Exception ex)
            {
                pushWait.TrySetException(ex);
            }
        };

        var launchSpec = ShooterRoomLaunchSpec.CreateDefault(options.ClientId);
        var launched = options.Mode == ShooterSmokeClientProcessMode.Create
            ? await launcher.CreateReadyStartAndSubscribeAsync(
                options.Host,
                options.Port,
                runtime,
                session,
                start,
                login.SessionToken,
                launchSpec,
                options.PlayerId,
                timeout: options.Timeout)
            : await launcher.JoinReadyStartAndSubscribeAsync(
                options.Host,
                options.Port,
                runtime,
                session,
                start,
                login.SessionToken,
                options.RoomId,
                launchSpec,
                options.PlayerId,
                timeout: options.Timeout);

        ValidateLaunch(launched);
        replay?.RecordLaunch(login.AccountId, launched);
        Console.WriteLine(FormatReady(options, login.AccountId, launched));

        var resultTimeout = CreateResultTimeout(options.Timeout);
        if (ShouldRequestInitialFullStateSync(launched.Flow.EntryKind))
        {
            await RequestInitialFullStateSyncWhileTickingAsync(launched, launcher, resultTimeout);
        }

        var push = await WaitForPushWhileTickingAsync(pushWait.Task, launcher, resultTimeout);
        ValidateAppliedSnapshot(push, runtime, presentation);
        var appliedSnapshotHashMatched = ValidateAppliedSnapshotHash(push, runtime);

        var inputResults = await SubmitInputsAsync(launched, options.InputCount, resultTimeout, replay);
        var reconnectResult = default(ShooterSmokeReconnectProcessResult);
        if (options.ReconnectOnce)
        {
            reconnectResult = await ReconnectOnceAsync(
                connection,
                launcher,
                runtime,
                session,
                start,
                login.SessionToken,
                launchSpec,
                options,
                () => pushCount,
                nextPushWait => pushWait = nextPushWait,
                resultTimeout);
            launched = reconnectResult.Launched;
        }

        replay?.RecordReconnect(in reconnectResult);

        if (options.WaitForMatchEnd)
        {
            await WaitForMatchEndAsync(launcher, launched, runtime, inputResults, options, () => lastPush, replay);
        }

        var hasInput = inputResults.Count > 0;
        var lastInput = hasInput ? inputResults[inputResults.Count - 1] : default;
        var matchResult = lastPush;

        connection.Tick(0f);
        var reconciliation = launched.Session.LastReconciliationResult;
        var snapshotHashMatched = ValidateLatestAuthoritativeSnapshot(push, reconciliation, appliedSnapshotHashMatched);

        var result = new ShooterSmokeClientProcessResult(
            options.Mode,
            options.ClientId,
            login.AccountId,
            launched.Flow.RoomId,
            launched.Flow.BattleId,
            launched.Flow.WorldId,
            launched.Flow.PlayerId,
            launched.Flow.EntryKind,
            launched.Flow.TargetFrame,
            runtime.CurrentFrame,
            presentation.ViewModel.Frame,
            runtime.ComputeStateHash(),
            push.ApplyResult,
            push.PackedFrame,
            push.PackedStateHash,
            push.PackedEntityCount,
            snapshotHashMatched,
            reconciliation.PredictedFrameBeforeCorrection,
            reconciliation.PredictedHashBeforeCorrection,
            reconciliation.AuthoritativeFrame,
            reconciliation.AuthoritativeStateHash,
            reconciliation.ImportedStateHash,
            reconciliation.AuthoritativeHashMatched,
            reconciliation.ReplayTicks,
            reconciliation.FinalFrame,
            reconciliation.FinalStateHash,
            reconciliation.PendingInputFramesBeforeCorrection,
            reconciliation.PendingInputFramesAfterTrim,
            reconciliation.PendingInputFramesAfterReplay,
            inputResults.Count,
            !hasInput || lastInput.Remote.Success,
            hasInput ? lastInput.Local.RequestedFrame : 0,
            hasInput ? lastInput.Remote.AcceptedFrame : 0,
            hasInput ? lastInput.Remote.CurrentFrame : 0,
            hasInput ? lastInput.Remote.Status : "none",
            hasInput ? lastInput.Remote.ServerTicks : 0L,
            hasInput && lastInput.Remote.ShouldResync,
            pushCount,
            lastPush.PackedFrame,
            matchResult.MatchState,
            matchResult.MatchFinal,
            matchResult.MatchVictory,
            matchResult.MatchCompletedFrame,
            matchResult.DefeatedEnemies,
            matchResult.VictoryTargetDefeats,
            matchResult.TimeLimitFrames,
            matchResult.RemainingTimeFrames,
            reconnectResult.ReconnectCount,
            reconnectResult.EntryKind,
            reconnectResult.TargetFrame,
            reconnectResult.PushesBefore,
            reconnectResult.PushesAfter,
            channel.NetworkCondition.InboundLatencyMs,
            channel.NetworkCondition.InboundJitterMs,
            channel.NetworkCondition.InboundPacketLossRate,
            channel.ConditionInboundReceived,
            channel.ConditionInboundDelayed,
            channel.ConditionInboundDropped,
            string.Empty,
            string.Empty,
            default);
        replay?.RecordResult(in result);
        var replayPath = replay?.Save() ?? string.Empty;
        var minimizedReplayPath = replay?.MinimizedOutputPath ?? string.Empty;
        var replayValidation = ShooterSmokeReplayValidation.ValidateReplay(minimizedReplayPath);
        return result with
        {
            InputStateReplayPath = replayPath,
            MinimizedInputStateReplayPath = minimizedReplayPath,
            InputStateReplayValidation = replayValidation,
        };
    }

    public static string FormatReady(
        ShooterSmokeClientProcessOptions options,
        string accountId,
        ShooterClientNetworkLaunchResult launched)
    {
        return "SHOOTER_MP_CLIENT_READY " +
            $"mode={options.Mode.ToString().ToLowerInvariant()} " +
            $"clientId=\"{Escape(options.ClientId)}\" " +
            $"accountId=\"{Escape(accountId)}\" " +
            $"roomId=\"{Escape(launched.Flow.RoomId)}\" " +
            $"battleId=\"{Escape(launched.Flow.BattleId)}\" " +
            $"worldId={launched.Flow.WorldId} " +
            $"playerId={launched.Flow.PlayerId} " +
            $"entryKind={launched.Flow.EntryKind} " +
            $"targetFrame={launched.Flow.TargetFrame}";
    }

    public static string FormatResult(in ShooterSmokeClientProcessResult result)
    {
        return "SHOOTER_MP_CLIENT_RESULT " +
            $"status=pass mode={result.Mode.ToString().ToLowerInvariant()} " +
            $"clientId=\"{Escape(result.ClientId)}\" " +
            $"accountId=\"{Escape(result.AccountId)}\" " +
            $"roomId=\"{Escape(result.RoomId)}\" " +
            $"battleId=\"{Escape(result.BattleId)}\" " +
            $"worldId={result.WorldId} " +
            $"playerId={result.PlayerId} " +
            $"entryKind={result.EntryKind} " +
            $"targetFrame={result.TargetFrame} " +
            $"runtimeFrame={result.RuntimeFrame} " +
            $"viewFrame={result.ViewFrame} " +
            $"stateHash=0x{result.StateHash:X8} " +
            $"snapshot={result.SnapshotApplyResult}@{result.SnapshotFrame} " +
            $"snapshotHash=0x{result.SnapshotStateHash:X8} " +
            $"snapshotHashMatched={result.SnapshotHashMatched} " +
            $"entities={result.SnapshotEntityCount} " +
            $"reconcilePredictedFrame={result.ReconcilePredictedFrame} " +
            $"reconcilePredictedHash=0x{result.ReconcilePredictedHash:X8} " +
            $"reconcileAuthoritativeFrame={result.ReconcileAuthoritativeFrame} " +
            $"reconcileAuthoritativeHash=0x{result.ReconcileAuthoritativeHash:X8} " +
            $"reconcileImportedHash=0x{result.ReconcileImportedHash:X8} " +
            $"reconcileAuthoritativeHashMatched={result.ReconcileAuthoritativeHashMatched} " +
            $"reconcileReplayTicks={result.ReconcileReplayTicks} " +
            $"reconcileFinalFrame={result.ReconcileFinalFrame} " +
            $"reconcileFinalHash=0x{result.ReconcileFinalHash:X8} " +
            $"reconcilePendingBefore={result.ReconcilePendingBefore} " +
            $"reconcilePendingAfterTrim={result.ReconcilePendingAfterTrim} " +
            $"reconcilePendingAfterReplay={result.ReconcilePendingAfterReplay} " +
            $"inputs={result.InputCount} " +
            $"lastInputSuccess={result.LastInputSuccess} " +
            $"lastRequestedFrame={result.LastRequestedFrame} " +
            $"lastAcceptedFrame={result.LastAcceptedFrame} " +
            $"lastCurrentFrame={result.LastCurrentFrame} " +
            $"lastInputStatus=\"{Escape(result.LastInputStatus)}\" " +
            $"lastServerTicks={result.LastServerTicks} " +
            $"shouldResync={result.ShouldResync} " +
            $"pushes={result.PushCount} " +
            $"lastPushFrame={result.LastPushFrame} " +
            $"matchState={result.MatchState} " +
            $"matchFinal={result.MatchFinal} " +
            $"matchVictory={result.MatchVictory} " +
            $"matchCompletedFrame={result.MatchCompletedFrame} " +
            $"defeatedEnemies={result.DefeatedEnemies} " +
            $"victoryTargetDefeats={result.VictoryTargetDefeats} " +
            $"timeLimitFrames={result.TimeLimitFrames} " +
            $"remainingTimeFrames={result.RemainingTimeFrames} " +
            $"reconnectCount={result.ReconnectCount} " +
            $"reconnectEntryKind={result.ReconnectEntryKind} " +
            $"reconnectTargetFrame={result.ReconnectTargetFrame} " +
            $"reconnectPushesBefore={result.ReconnectPushesBefore} " +
            $"reconnectPushesAfter={result.ReconnectPushesAfter} " +
            $"conditionLatencyMs={result.ConditionLatencyMs} " +
            $"conditionJitterMs={result.ConditionJitterMs} " +
            $"conditionPacketLossRate={result.ConditionPacketLossRate.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            $"conditionInboundReceived={result.ConditionInboundReceived} " +
            $"conditionInboundDelayed={result.ConditionInboundDelayed} " +
            $"conditionInboundDropped={result.ConditionInboundDropped} " +
            $"inputStateReplayPath=\"{Escape(result.InputStateReplayPath)}\" " +
            $"minimizedInputStateReplayPath=\"{Escape(result.MinimizedInputStateReplayPath)}\" " +
            $"inputStateReplayConsumed={result.InputStateReplayValidation.Consumed} " +
            $"inputStateReplayInputs={result.InputStateReplayValidation.InputCount} " +
            $"inputStateReplaySnapshots={result.InputStateReplayValidation.SnapshotCount} " +
            $"inputStateReplayHashes={result.InputStateReplayValidation.StateHashCount} " +
            $"clientStateReplayPath=\"{Escape(result.InputStateReplayPath)}\" " +
            $"minimizedClientStateReplayPath=\"{Escape(result.MinimizedInputStateReplayPath)}\" " +
            $"clientStateReplayConsumed={result.InputStateReplayValidation.Consumed} " +
            $"clientStateReplayInputs={result.InputStateReplayValidation.InputCount} " +
            $"clientStateReplaySnapshots={result.InputStateReplayValidation.SnapshotCount} " +
            $"clientStateReplayHashes={result.InputStateReplayValidation.StateHashCount}";
    }

    public static string FormatFailure(in ShooterSmokeClientProcessOptions options, Exception exception)
    {
        return "SHOOTER_MP_CLIENT_RESULT " +
            $"status=fail mode={options.Mode.ToString().ToLowerInvariant()} " +
            $"clientId=\"{Escape(options.ClientId)}\" " +
            $"error=\"{Escape(GetExceptionMessage(exception))}\"";
    }

    private static ShooterStartGamePayload CreateStartGame(int seed)
    {
        return new ShooterStartGamePayload(
            "shooter-multiprocess-client",
            ShooterGameplay.DefaultTickRate,
            seed,
            new[]
            {
                new ShooterStartPlayer(1, "P1", 0f, 0f),
                new ShooterStartPlayer(2, "P2", 5f, 0f),
                new ShooterStartPlayer(3, "P3", -5f, 0f),
                new ShooterStartPlayer(4, "P4", 0f, 5f)
            });
    }

    private static async Task WaitForMatchEndAsync(
        ShooterClientNetworkLauncher launcher,
        ShooterClientNetworkLaunchResult launched,
        ShooterBattleRuntimePort runtime,
        List<ShooterClientGatewayInputSubmitResult> inputResults,
        ShooterSmokeClientProcessOptions options,
        Func<ShooterSnapshotPushSmokeResult> getLastPush,
        ShooterSmokeReplayRecordScope? replay)
    {
        var deadline = DateTime.UtcNow + CreateResultTimeout(options.Timeout);
        var lastFrame = runtime.CurrentFrame;
        while (DateTime.UtcNow < deadline)
        {
            launcher.Tick(1f / ShooterGameplay.DefaultTickRate);
            var lastPush = getLastPush();
            if (lastPush.MatchFinal)
            {
                return;
            }

            if (options.Mode == ShooterSmokeClientProcessMode.Create)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var requestTimeout = remaining < TimeSpan.FromSeconds(2) ? remaining : TimeSpan.FromSeconds(2);
                var submit = await launched.Battle.SubmitLocalInputToGatewayAsync(
                    CreateGameplayLoopCommand(runtime.CurrentFrame),
                    timeout: requestTimeout);
                ValidateInput(submit);
                replay?.RecordInput(in submit);
                inputResults.Add(submit);
            }

            await Task.Delay(35);
            lastPush = getLastPush();
            if (lastPush.PackedFrame > lastFrame)
            {
                lastFrame = lastPush.PackedFrame;
            }
        }

        var current = getLastPush();
        throw new InvalidOperationException($"Shooter multiprocess client did not observe final match state. Mode={options.Mode}, ClientId={options.ClientId}, State={current.MatchState}, Final={current.MatchFinal}, RuntimeFrame={runtime.CurrentFrame}, LastPushFrame={lastFrame}, CompletedFrame={current.MatchCompletedFrame}");
    }

    private static TimeSpan CreateResultTimeout(TimeSpan timeout)
    {
        var safetyMargin = TimeSpan.FromSeconds(5);
        var minimum = TimeSpan.FromSeconds(1);
        return timeout > safetyMargin + minimum ? timeout - safetyMargin : minimum;
    }

    private static bool ShouldRequestInitialFullStateSync(ShooterRoomGatewayEntryKind entryKind)
    {
        return entryKind == ShooterRoomGatewayEntryKind.LateJoin
            || entryKind == ShooterRoomGatewayEntryKind.Reconnect;
    }

    private static async Task RequestInitialFullStateSyncWhileTickingAsync(
        ShooterClientNetworkLaunchResult launched,
        ShooterClientNetworkLauncher launcher,
        TimeSpan timeout)
    {
        var request = launched.Battle.RequestFullSnapshotBaselineAsync(timeout);
        var deadline = DateTime.UtcNow + timeout;
        while (!request.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Timed out requesting initial Shooter full-state sync.");
            }

            launcher.Tick(1f / ShooterGameplay.DefaultTickRate);
            await Task.Delay(10).ConfigureAwait(false);
        }

        var result = await request.ConfigureAwait(false);
        if (!result.Success || !result.Accepted)
        {
            throw new InvalidOperationException($"Initial Shooter full-state sync request was rejected. Success={result.Success}, Accepted={result.Accepted}, Message={result.Message}");
        }
    }

    private static async Task<ShooterSnapshotPushSmokeResult> WaitForPushWhileTickingAsync(
        Task<ShooterSnapshotPushSmokeResult> task,
        ShooterClientNetworkLauncher launcher,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!task.IsCompleted)
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Timed out waiting for Shooter snapshot push.");
            }

            launcher.Tick(1f / ShooterGameplay.DefaultTickRate);
            await Task.Delay(10).ConfigureAwait(false);
        }

        return await task.ConfigureAwait(false);
    }

    private static ShooterPlayerCommand CreateGameplayLoopCommand(int frame)
    {
        const float firstEnemyX = -0.12186934f;
        const float firstEnemyY = 0.99254614f;
        var moveX = frame % 20 < 10 ? 0.35f : -0.2f;
        var moveY = frame % 30 < 15 ? 0.15f : -0.1f;
        return new ShooterPlayerCommand(1, moveX, moveY, firstEnemyX, firstEnemyY, fire: true);
    }

    private static async Task<List<ShooterClientGatewayInputSubmitResult>> SubmitInputsAsync(
        ShooterClientNetworkLaunchResult launched,
        int inputCount,
        TimeSpan timeout,
        ShooterSmokeReplayRecordScope? replay)
    {
        var results = new List<ShooterClientGatewayInputSubmitResult>(Math.Max(0, inputCount));
        for (var i = 0; i < inputCount; i++)
        {
            var submit = await launched.Battle.SubmitLocalInputToGatewayAsync(
                i % 2 == 0 ? 1f : -0.35f,
                i % 3 == 0 ? 0.25f : 0f,
                1f,
                i % 2 == 0 ? 0f : 0.2f,
                i % 2 == 0,
                timeout: timeout);

            ValidateInput(submit);
            replay?.RecordInput(in submit);
            results.Add(submit);
            await Task.Delay(35);
        }

        return results;
    }

    private static async Task<ShooterSmokeReconnectProcessResult> ReconnectOnceAsync(
        AbilityKit.Network.Abstractions.IConnection connection,
        ShooterClientNetworkLauncher launcher,
        ShooterBattleRuntimePort runtime,
        ShooterPresentationSessionContext session,
        ShooterStartGamePayload start,
        string sessionToken,
        ShooterRoomLaunchSpec launchSpec,
        ShooterSmokeClientProcessOptions options,
        Func<int> getPushCount,
        Action<TaskCompletionSource<ShooterSnapshotPushSmokeResult>> replacePushWait,
        TimeSpan timeout)
    {
        if (options.Mode != ShooterSmokeClientProcessMode.Join)
        {
            return default;
        }

        var pushesBefore = getPushCount();
        connection.Close();
        await Task.Delay(Math.Max(0, options.ReconnectDelayMs));
        connection.Tick(0f);

        var reconnectPushWait = new TaskCompletionSource<ShooterSnapshotPushSmokeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        replacePushWait(reconnectPushWait);
        var reconnected = await launcher.JoinReadyStartAndSubscribeAsync(
            options.Host,
            options.Port,
            runtime,
            session,
            start,
            sessionToken,
            options.RoomId,
            launchSpec,
            options.PlayerId,
            timeout: timeout);

        ValidateLaunch(reconnected);
        if (reconnected.Flow.EntryKind != ShooterRoomGatewayEntryKind.Reconnect)
        {
            throw new InvalidOperationException($"Shooter multiprocess reconnect expected reconnect entry kind. Actual={reconnected.Flow.EntryKind}");
        }

        var reconnectPush = await WaitForPushWhileTickingAsync(reconnectPushWait.Task, launcher, timeout);
        if (!IsAppliedSnapshotResult(reconnectPush.ApplyResult))
        {
            throw new InvalidOperationException($"Shooter multiprocess reconnect snapshot was not applied. Result={reconnectPush.ApplyResult}");
        }

        return new ShooterSmokeReconnectProcessResult(
            reconnected,
            1,
            reconnected.Flow.EntryKind,
            reconnected.Flow.TargetFrame,
            pushesBefore,
            getPushCount());
    }

    private static bool TryCaptureSnapshotPush(
        uint opCode,
        ArraySegment<byte> payload,
        ShooterSnapshotApplyResult applyResult,
        out ShooterSnapshotPushSmokeResult result)
    {
        result = default;
        if (opCode != RoomGatewayOpCodes.SnapshotPushed)
        {
            return false;
        }

        var wire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(payload);
        if (wire.ServerTicks <= 0)
        {
            throw new InvalidOperationException("Shooter snapshot push returned invalid server ticks.");
        }

        if (wire.PayloadOpCode != ShooterOpCodes.Snapshot.PackedState)
        {
            result = new ShooterSnapshotPushSmokeResult(
                applyResult,
                wire.WorldId,
                wire.Frame,
                wire.ServerTicks,
                wire.PayloadOpCode,
                wire.WorldId,
                wire.Frame,
                wire.ServerTicks,
                0u,
                0,
                0,
                false,
                false,
                0,
                0,
                0,
                0,
                0);
            return true;
        }

        if (wire.Payload == null || wire.Payload.Length == 0)
        {
            throw new InvalidOperationException("Shooter packed snapshot push returned empty payload.");
        }

        var packed = ShooterPackedSnapshotCodec.Deserialize(wire.Payload);
        var metadata = CaptureMatchMetadata(in packed);
        result = new ShooterSnapshotPushSmokeResult(
            applyResult,
            wire.WorldId,
            wire.Frame,
            wire.ServerTicks,
            wire.PayloadOpCode,
            packed.WorldId,
            packed.Frame,
            packed.ServerTick,
            packed.StateHash,
            packed.EntityCount,
            metadata.MatchState,
            metadata.MatchFinal,
            metadata.MatchVictory,
            metadata.MatchCompletedFrame,
            metadata.DefeatedEnemies,
            metadata.VictoryTargetDefeats,
            metadata.TimeLimitFrames,
            metadata.RemainingTimeFrames);
        return true;
    }

    private static bool IsAppliedSnapshotResult(ShooterSnapshotApplyResult result)
    {
        return result == ShooterSnapshotApplyResult.AppliedPackedSnapshot
            || result == ShooterSnapshotApplyResult.AppliedActorSnapshot;
    }

    private static ShooterPackedMatchMetadata CaptureMatchMetadata(in ShooterPackedSnapshotPayload packed)
    {
        var chunks = packed.ComponentChunks ?? Array.Empty<ShooterPackedComponentChunk>();
        for (var i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            if (chunk.ComponentKind != ShooterPackedComponentKinds.RuntimeMetadata)
            {
                continue;
            }

            var values = chunk.IntValues;
            if (values == null || values.Length < 5)
            {
                break;
            }

            var matchState = values[0];
            var timeLimitFrames = Math.Max(0, values[4]);
            return new ShooterPackedMatchMetadata(
                matchState,
                IsFinalMatchState(matchState),
                matchState == (int)ShooterBattleMatchState.Victory,
                values[1],
                values[2],
                values[3],
                timeLimitFrames,
                timeLimitFrames == 0 ? 0 : Math.Max(0, timeLimitFrames - packed.Frame));
        }

        return default;
    }

    private static bool IsFinalMatchState(int matchState)
    {
        return matchState == (int)ShooterBattleMatchState.Victory
            || matchState == (int)ShooterBattleMatchState.Defeat
            || matchState == (int)ShooterBattleMatchState.Ended;
    }

    private readonly record struct ShooterPackedMatchMetadata(
        int MatchState,
        bool MatchFinal,
        bool MatchVictory,
        int MatchCompletedFrame,
        int DefeatedEnemies,
        int VictoryTargetDefeats,
        int TimeLimitFrames,
        int RemainingTimeFrames);

    private static void ValidateLaunch(ShooterClientNetworkLaunchResult launched)
    {
        if (string.IsNullOrWhiteSpace(launched.Flow.RoomId))
        {
            throw new InvalidOperationException("Shooter multiprocess client launch returned empty room id.");
        }

        if (string.IsNullOrWhiteSpace(launched.Flow.BattleId))
        {
            throw new InvalidOperationException("Shooter multiprocess client launch returned empty battle id.");
        }

        if (launched.Flow.WorldId == 0)
        {
            throw new InvalidOperationException("Shooter multiprocess client launch returned empty world id.");
        }

        if (!launched.Flow.Started || !launched.Flow.Subscribed)
        {
            throw new InvalidOperationException($"Shooter multiprocess client launch was incomplete. Started={launched.Flow.Started}, Subscribed={launched.Flow.Subscribed}, Message={launched.Flow.Message}");
        }
    }

    private static void ValidateAppliedSnapshot(
        in ShooterSnapshotPushSmokeResult push,
        ShooterBattleRuntimePort runtime,
        ShooterPresentationFacade presentation)
    {
        if (push.ApplyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot && push.ApplyResult != ShooterSnapshotApplyResult.AppliedActorSnapshot)
        {
            throw new InvalidOperationException($"Shooter multiprocess snapshot was not applied. Result={push.ApplyResult}");
        }

        if (runtime.CurrentFrame <= 0 || presentation.ViewModel.Frame <= 0)
        {
            throw new InvalidOperationException("Shooter multiprocess client runtime/presentation did not advance after snapshot push.");
        }
    }

    private static bool ValidateAppliedSnapshotHash(
        in ShooterSnapshotPushSmokeResult push,
        ShooterBattleRuntimePort runtime)
    {
        return push.ApplyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot
            || push.PackedStateHash == 0u
            || runtime.ComputeStateHash() == push.PackedStateHash;
    }

    private static bool ValidateLatestAuthoritativeSnapshot(
        in ShooterSnapshotPushSmokeResult firstAppliedPush,
        in ShooterClientReconciliationResult reconciliation,
        bool appliedSnapshotHashMatched)
    {
        if (firstAppliedPush.ApplyResult != ShooterSnapshotApplyResult.AppliedPackedSnapshot || firstAppliedPush.PackedStateHash == 0u)
        {
            return true;
        }

        if (reconciliation.ApplyResult == ShooterSnapshotApplyResult.AppliedPackedSnapshot)
        {
            return reconciliation.AuthoritativeHashMatched
                && reconciliation.AuthoritativeStateHash != 0u
                && reconciliation.ImportedStateHash == reconciliation.AuthoritativeStateHash;
        }

        return appliedSnapshotHashMatched;
    }

    private static void ValidateInput(in ShooterClientGatewayInputSubmitResult submit)
    {
        if (!submit.Remote.Success)
        {
            throw new InvalidOperationException($"Shooter multiprocess gateway input was rejected. RequestedFrame={submit.Local.RequestedFrame}, Status={submit.Remote.Status}, Message={submit.Remote.Message}");
        }

        if (submit.Remote.AcceptedFrame < submit.Local.RequestedFrame)
        {
            throw new InvalidOperationException($"Shooter multiprocess gateway accepted frame regressed. RequestedFrame={submit.Local.RequestedFrame}, AcceptedFrame={submit.Remote.AcceptedFrame}");
        }

        if (submit.Remote.CurrentFrame < 0)
        {
            throw new InvalidOperationException("Shooter multiprocess gateway input response returned invalid current frame.");
        }

        if (submit.Remote.ServerTicks <= 0)
        {
            throw new InvalidOperationException("Shooter multiprocess gateway input response returned invalid server ticks.");
        }
    }

    private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string GetExceptionMessage(Exception exception)
    {
        return exception is AggregateException aggregate && aggregate.InnerException is not null
            ? GetExceptionMessage(aggregate.InnerException)
            : exception.Message;
    }
}

internal enum ShooterSmokeClientProcessMode
{
    Create,
    Join
}

internal readonly record struct ShooterSmokeClientProcessOptions(
    ShooterSmokeClientProcessMode Mode,
    string Host,
    int Port,
    string RoomId,
    uint PlayerId,
    string ClientId,
    int InputCount,
    int Seed,
    TimeSpan Timeout,
    bool WaitForMatchEnd,
    bool ReconnectOnce,
    int ReconnectDelayMs,
    SmokeNetworkConditionOptions NetworkCondition,
    string InputStateReplayOutputPath);

internal readonly record struct ShooterSmokeReconnectProcessResult(
    ShooterClientNetworkLaunchResult Launched,
    int ReconnectCount,
    ShooterRoomGatewayEntryKind EntryKind,
    int TargetFrame,
    int PushesBefore,
    int PushesAfter);

internal readonly record struct ShooterSmokeClientProcessResult(
    ShooterSmokeClientProcessMode Mode,
    string ClientId,
    string AccountId,
    string RoomId,
    string BattleId,
    ulong WorldId,
    uint PlayerId,
    ShooterRoomGatewayEntryKind EntryKind,
    int TargetFrame,
    int RuntimeFrame,
    int ViewFrame,
    uint StateHash,
    ShooterSnapshotApplyResult SnapshotApplyResult,
    int SnapshotFrame,
    uint SnapshotStateHash,
    int SnapshotEntityCount,
    bool SnapshotHashMatched,
    int ReconcilePredictedFrame,
    uint ReconcilePredictedHash,
    int ReconcileAuthoritativeFrame,
    uint ReconcileAuthoritativeHash,
    uint ReconcileImportedHash,
    bool ReconcileAuthoritativeHashMatched,
    int ReconcileReplayTicks,
    int ReconcileFinalFrame,
    uint ReconcileFinalHash,
    int ReconcilePendingBefore,
    int ReconcilePendingAfterTrim,
    int ReconcilePendingAfterReplay,
    int InputCount,
    bool LastInputSuccess,
    int LastRequestedFrame,
    int LastAcceptedFrame,
    int LastCurrentFrame,
    string LastInputStatus,
    long LastServerTicks,
    bool ShouldResync,
    int PushCount,
    int LastPushFrame,
    int MatchState,
    bool MatchFinal,
    bool MatchVictory,
    int MatchCompletedFrame,
    int DefeatedEnemies,
    int VictoryTargetDefeats,
    int TimeLimitFrames,
    int RemainingTimeFrames,
    int ReconnectCount,
    ShooterRoomGatewayEntryKind ReconnectEntryKind,
    int ReconnectTargetFrame,
    int ReconnectPushesBefore,
    int ReconnectPushesAfter,
    int ConditionLatencyMs,
    int ConditionJitterMs,
    double ConditionPacketLossRate,
    int ConditionInboundReceived,
    int ConditionInboundDelayed,
    int ConditionInboundDropped,
    string InputStateReplayPath,
    string MinimizedInputStateReplayPath,
    ShooterSmokeReplayValidationResult InputStateReplayValidation);
