using System;
using System.IO;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.Client;

public sealed class ShooterRemoteCoordinatorInputContractTests
{
    [Fact]
    public void RemotePlayModeInputSubmissionUsesCoordinatorBridge()
    {
        var playModeHost = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterRemoteStateSyncPlayModeHost.cs");
        var inputSubmitStrategy = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterRemoteInputSubmitStrategy.cs");

        Assert.Contains("_inputSubmitStrategy = ShooterRemoteInputSubmitStrategy.Create(state.CoordinatorInputBridge, launchOptions.Timeout);", playModeHost);
        Assert.Contains("inputSubmitStrategy?.SubmitOrQueue(in submitResult);", ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterRemoteInputPump.cs"));
        Assert.Contains("inputBridge.SubmitAcceptedInputAsync(local, requestTimeout)", inputSubmitStrategy);
        Assert.Contains("RemoteClientInputSubmitQueue<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult>", inputSubmitStrategy);
        Assert.Contains("ShooterCoordinatorInputBridge.Create(", playModeHost);
        Assert.Contains("state.CoordinatorInputBridge.Tick(deltaSeconds)", playModeHost);
        Assert.Contains("CoordinatorInputBridge.Dispose();", playModeHost);
        Assert.DoesNotContain("state.CoordinatorInputBridge.SubmitAcceptedInputAsync(local, timeout)", playModeHost);
        Assert.DoesNotContain("state.Launch.Battle.SubmitAcceptedInputToGatewayAsync(local, timeout)", playModeHost);
    }

    [Fact]
    public void RemotePlayModePauseStopsPumpAndResumeUsesRestoreOnlyReconnect()
    {
        var playModeHost = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterRemoteStateSyncPlayModeHost.cs");
        var reconnectLaunchOptionsBuilder = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterReconnectLaunchOptionsBuilder.cs");

        var initialFullStateSync = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterInitialFullStateSyncCoordinator.cs");
        var connectionFlow = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "PlayMode", "ShooterRemoteStateSyncConnectionFlow.cs");

        Assert.Contains("public static bool IsPaused => _isPaused;", playModeHost);
        Assert.Contains("public static bool IsAutoReconnecting => _isAutoReconnecting;", playModeHost);
        Assert.Contains("public static void PauseForReconnectValidation()", playModeHost);
        Assert.Contains("state.Launcher.Close();", playModeHost);
        Assert.Contains("_inputSubmitStrategy?.Reset();", playModeHost);
        Assert.Contains("if (state == null || _isPaused || _isAutoReconnecting)", playModeHost);
        Assert.Contains("public static Task<ShooterClientNetworkLaunchResult> ResumeFromPauseAsync()", playModeHost);
        Assert.Contains("TryBeginAutoReconnectAfterSocketLoss(state)", playModeHost);
        Assert.Contains("connection.State == ConnectionState.Connected", playModeHost);
        Assert.Contains("connection.State == ConnectionState.Connecting", playModeHost);
        Assert.Contains("_ = ResumeAfterSocketLossAsync(_pausedResumeOptions, _lifecycleGeneration);", playModeHost);
        Assert.Contains("if (!IsCurrentLifecycle(sourceGeneration))", playModeHost);
        Assert.Contains("ThrowIfStaleLifecycle(generation);", playModeHost);
        Assert.Contains("ShooterReconnectLaunchOptionsBuilder.RestoreOnly(_options", playModeHost);
        Assert.Contains("ShooterRemoteStateSyncLaunchMode.RestoreOnly", reconnectLaunchOptionsBuilder);
        Assert.Contains("new ShooterInitialFullStateSyncCoordinator(", playModeHost);
        Assert.Contains("NotifyStateChangedIfCurrent(generation)).RequestIfNeededAsync(", playModeHost);
        Assert.Contains("RequiresInitialFullStateSync => EntryKind == ShooterRoomGatewayEntryKind.LateJoin", connectionFlow);
        Assert.Contains("SnapshotPushDispatched += OnSnapshotPushDispatched", initialFullStateSync);
        Assert.Contains("while (!snapshotApplied)", initialFullStateSync);
        Assert.Contains("IsApplied(result, session)", initialFullStateSync);
        Assert.Contains("LastInitialFullStateSyncApplyResult", playModeHost);
    }

    [Fact]
    public void PlayModeMenuExposesRemotePauseAndResumeControls()
    {
        var playModeMenu = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Unity", "PlayMode", "ShooterPlayModeMenu.cs");

        Assert.Contains("Pause Remote", playModeMenu);
        Assert.Contains("ShooterRemoteStateSyncPlayModeHost.PauseForReconnectValidation();", playModeMenu);
        Assert.Contains("Resume Remote", playModeMenu);
        Assert.Contains("RunAsync(\"resume remote\", ResumeRemoteAsync);", playModeMenu);
        Assert.Contains("ShooterRemoteStateSyncPlayModeHost.ResumeFromPauseAsync()", playModeMenu);
        Assert.Contains("IsAutoReconnecting", playModeMenu);
        Assert.Contains("IsWaitingForInitialFullStateSync", playModeMenu);
        Assert.Contains("LastInitialFullStateSyncApplyResult", playModeMenu);
        Assert.Contains("return \"Syncing Latest State\";", playModeMenu);
        Assert.Contains("return \"Auto Reconnecting\";", playModeMenu);
        Assert.Contains("return \"Paused\";", playModeMenu);
    }

    [Fact]
    public void CoordinatorBridgeOwnsSessionCoordinatorAndConnectsRemoteAdapter()
    {
        var bridge = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Hosting", "ShooterCoordinatorInputBridge.cs");

        Assert.Contains("var transport = new ShooterGatewayCoordinatorInputTransport(", bridge);
        Assert.Contains("var host = new ShooterCoordinatorSessionHost(world, transport);", bridge);
        Assert.Contains("var coordinator = new SessionCoordinator();", bridge);
        Assert.Contains("coordinator.Initialize(config, host);", bridge);
        Assert.Contains("coordinator.Start();", bridge);
        Assert.Contains("coordinator.SyncAdapter is IRemoteSyncAdapter remote", bridge);
        Assert.Contains("remote.Connect(config.ServerEndpoint, config.RoomId, config.LocalPlayerId);", bridge);
        Assert.Contains("SubmitAcceptedInputViaCoordinatorAsync(_coordinator, local, timeout, cancellationToken)", bridge);
        Assert.Contains("_coordinator.Tick(deltaTime);", bridge);
        Assert.Contains("_coordinator.Destroy();", bridge);
    }

    [Fact]
    public void GatewayTransportImplementsFrameworkTransportAndPreservesShooterInputPayload()
    {
        var transport = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Hosting", "ShooterGatewayCoordinatorInputTransport.cs");

        var frameworkBridge = ReadUnityPackageSource(
            "com.abilitykit.coordinator",
            "Runtime", "Transport", "CoordinatorInputSubmitBridge.cs");

        Assert.Contains("public sealed class ShooterGatewayCoordinatorInputTransport : IRemoteBattleSyncTransport", transport);
        Assert.Contains("CoordinatorInputSubmitBridge<ShooterClientInputSubmitResult, ShooterClientGatewayInputSubmitResult>", transport);
        Assert.Contains("public bool SubmitInput(PlayerInput input)", transport);
        Assert.Contains("return _submitBridge.TrySubmit(input);", transport);
        Assert.Contains("CreateCoordinatorInput", transport);
        Assert.Contains("BindCoordinatorInput", transport);
        Assert.Contains("local.Packet.Command.PlayerId", transport);
        Assert.Contains("local.Packet.OpCode", transport);
        Assert.Contains("local.Packet.Payload ?? Array.Empty<byte>()", transport);
        Assert.DoesNotContain("_pendingLocal", transport);
        Assert.DoesNotContain("_pendingTask", transport);
        Assert.Contains("public sealed class CoordinatorInputSubmitBridge<TLocalSubmitResult, TRemoteSubmitResult>", frameworkBridge);
        Assert.Contains("coordinator.SubmitLocalInput(input);", frameworkBridge);
        Assert.Contains("public bool TrySubmit(PlayerInput input)", frameworkBridge);
    }

    [Fact]
    public void RestoreFirstConnectionUsesFrameworkPolicy()
    {
        var connectionFlow = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "PlayMode", "ShooterRemoteStateSyncConnectionFlow.cs");
        var restoreFirstPolicy = ReadUnityPackageSource(
            "com.abilitykit.host.extension",
            "Runtime", "Session", "RoomGatewayRestoreFirstConnectionPolicy.cs");

        Assert.Contains("RoomGatewayRestoreFirstConnectionPolicy.ConnectAsync", connectionFlow);
        Assert.Contains("RestoreRoomAsLaunchAsync", connectionFlow);
        Assert.DoesNotContain("catch (Exception ex) when (launchOptions.LaunchMode == ShooterRemoteStateSyncLaunchMode.RestoreFirst)", connectionFlow);
        Assert.Contains("public static class RoomGatewayRestoreFirstConnectionPolicy", restoreFirstPolicy);
        Assert.Contains("allowFallbackCreate", restoreFirstPolicy);
        Assert.Contains("UsedFallbackCreate", restoreFirstPolicy);
        Assert.Contains("RestoreFailure", restoreFirstPolicy);
    }

    [Fact]
    public void InputCoordinatorUsesFrameSyncControllerAsFormalDependency()
    {
        var inputCoordinator = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Client", "Session", "ShooterClientInputCoordinator.cs");
        var syncCore = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Client", "Synchronization", "ShooterClientSyncCore.cs");
        var predictRollback = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Client", "Synchronization", "ShooterClientPredictRollbackSyncController.cs");
        var authoritativeInterpolation = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Client", "Synchronization", "ShooterClientAuthoritativeInterpolationSyncController.cs");

        Assert.Contains("private readonly ShooterClientFrameSyncController _frameSync;", inputCoordinator);
        Assert.Contains("public ShooterClientInputCoordinator(ShooterClientFrameSyncController frameSync", inputCoordinator);
        Assert.DoesNotContain("public ShooterClientInputCoordinator(ShooterClientFrameSyncCoordinator", inputCoordinator);
        Assert.Contains("new ShooterClientInputCoordinator(_frameSync.Controller, gateway)", syncCore);
        Assert.Contains("private readonly ShooterClientSyncCore _core;", predictRollback);
        Assert.Contains("private readonly ShooterClientSyncCore _core;", authoritativeInterpolation);
        Assert.DoesNotContain("new ShooterClientInputCoordinator(_frameSync.Controller, gateway)", predictRollback);
        Assert.DoesNotContain("new ShooterClientInputCoordinator(_frameSync.Controller, gateway)", authoritativeInterpolation);
    }

    [Fact]
    public void CoordinatorSessionHostExposesTransportThroughExistingShooterWorld()
    {
        var sessionHost = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "Hosting", "ShooterCoordinatorSessionHost.cs");
        var existingWorldHost = ReadUnityPackageSource(
            "com.abilitykit.coordinator",
            "Runtime", "Core", "ExistingWorldSessionCoordinatorHost.cs");
        var asmdef = ReadUnityPackageSource(
            "com.abilitykit.demo.shooter.view.runtime",
            "Runtime", "com.abilitykit.demo.shooter.view.runtime.asmdef");

        Assert.Contains("public sealed class ShooterCoordinatorSessionHost : ISessionCoordinatorHost, ISessionCoordinatorConfigPolicy", sessionHost);
        Assert.Contains("private readonly ExistingWorldSessionCoordinatorHost _host;", sessionHost);
        Assert.Contains("serviceOverrides: new object[] { transport }", sessionHost);
        Assert.Contains("configureSession: ConfigureShooterSession", sessionHost);
        Assert.Contains("_host.CreateWorldHost(config)", sessionHost);
        Assert.Contains("config.SyncMode = SyncMode.StateSync;", sessionHost);
        Assert.Contains("config.HostMode = HostMode.Client;", sessionHost);
        Assert.Contains("config.UseCoordinatorSpawnService = false;", sessionHost);
        Assert.Contains("public sealed class ExistingWorldSessionCoordinatorHost : ISessionCoordinatorHost, ISessionCoordinatorConfigPolicy", existingWorldHost);
        Assert.Contains("serviceType.IsInstanceOfType(candidate)", existingWorldHost);
        Assert.Contains("public bool DestroyWorld(WorldId id)", existingWorldHost);
        Assert.Contains("return false;", existingWorldHost);
        Assert.Contains("\"AbilityKit.Coordinator\"", asmdef);
    }

    private static string ReadUnityPackageSource(string packageName, params string[] relativeParts)
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        var path = Path.Combine(root, "Unity", "Packages", packageName, Path.Combine(relativeParts));
        Assert.True(File.Exists(path), $"Expected Unity package source file to exist: {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))
                && Directory.Exists(Path.Combine(directory.FullName, "Unity"))
                && Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate repository root from {startDirectory}.");
    }
}
