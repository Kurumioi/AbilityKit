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

        Assert.Contains("state.CoordinatorInputBridge.SubmitAcceptedInputAsync(local, timeout)", playModeHost);
        Assert.Contains("ShooterCoordinatorInputBridge.Create(", playModeHost);
        Assert.Contains("state.CoordinatorInputBridge.Tick(deltaSeconds)", playModeHost);
        Assert.Contains("CoordinatorInputBridge.Dispose();", playModeHost);
        Assert.DoesNotContain("state.Launch.Battle.SubmitAcceptedInputToGatewayAsync(local, timeout)", playModeHost);
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

        Assert.Contains("public sealed class ShooterGatewayCoordinatorInputTransport : IRemoteBattleSyncTransport", transport);
        Assert.Contains("public bool SubmitInput(PlayerInput input)", transport);
        Assert.Contains("var frameLocal = local.WithRequestedFrame(input.Frame);", transport);
        Assert.Contains("_submitAsync(frameLocal, timeout, cancellationToken)", transport);
        Assert.Contains("var input = new PlayerInput(", transport);
        Assert.Contains("local.Packet.Command.PlayerId", transport);
        Assert.Contains("local.Packet.OpCode", transport);
        Assert.Contains("local.Packet.Payload ?? Array.Empty<byte>()", transport);
        Assert.Contains("coordinator.SubmitLocalInput(input);", transport);
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
