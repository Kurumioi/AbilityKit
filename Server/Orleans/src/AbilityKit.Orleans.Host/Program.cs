using System;
using System.IO;
using System.Text.Json;
using AbilityKit.Orleans.Grains.Battle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ServerMobaWorldManager>(sp =>
    new ServerMobaWorldManager(sp.GetRequiredService<ILogger<ServerMobaWorldManager>>()));

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.Configure<ClusterOptions>(options =>
    { 
        options.ClusterId = "abilitykit-dev";
        options.ServiceId = "abilitykit-orleans";
    });
});

var host = builder.Build();
await host.RunAsync();
