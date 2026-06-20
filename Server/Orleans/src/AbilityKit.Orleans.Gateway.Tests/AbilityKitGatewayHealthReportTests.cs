using System;
using System.Collections.Generic;
using AbilityKit.Orleans.Gateway.HttpApi;
using AbilityKit.Orleans.Hosting;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class AbilityKitGatewayHealthReportTests
{
    [Fact]
    public void Ready_should_include_deployment_diagnostics()
    {
        var runtime = new AbilityKitGatewayRuntimeDiagnostics(
            "127.0.0.1:3000",
            "127.0.0.1:3001",
            true,
            true,
            12,
            4);

        var gameplay = new AbilityKitGatewayGameplayDiagnostics(
            3,
            2,
            1,
            5,
            4,
            1,
            new List<string> { "SessionGrain", "RoomGrain" });

        var deployment = new AbilityKitGatewayDeploymentDiagnostics(
            "Shared",
            true,
            false,
            new List<string> { "session", "room" },
            new List<string> { "room" });

        var report = AbilityKitGatewayHealthReport.Ready(
            "127.0.0.1:3000",
            "127.0.0.1:3001",
            runtime,
            gameplay,
            deployment);

        Assert.Equal("Ready", report.Status);
        Assert.Equal("AbilityKit.Orleans.Gateway", report.Service);
        Assert.Equal(deployment, report.Deployment);
        Assert.Equal(runtime, report.Runtime);
        Assert.Equal(gameplay, report.Gameplay);
    }

    [Fact]
    public void Ready_should_report_utc_timestamp_and_urls()
    {
        var runtime = new AbilityKitGatewayRuntimeDiagnostics(
            "http://localhost:5000",
            "127.0.0.1:3001",
            false,
            false,
            0,
            0);

        var gameplay = new AbilityKitGatewayGameplayDiagnostics(
            0,
            0,
            0,
            0,
            0,
            0,
            Array.Empty<string>());

        var deployment = new AbilityKitGatewayDeploymentDiagnostics(
            "Dedicated",
            true,
            true,
            new List<string> { "battle" },
            new List<string> { "battle" });

        var report = AbilityKitGatewayHealthReport.Ready(
            "http://localhost:5000",
            "127.0.0.1:3001",
            runtime,
            gameplay,
            deployment);

        Assert.Equal("http://localhost:5000", report.HttpUrl);
        Assert.Equal("127.0.0.1:3001", report.TcpEndpoint);
        Assert.True(report.CheckedAtUtc <= DateTimeOffset.UtcNow);
        Assert.True(report.CheckedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}
