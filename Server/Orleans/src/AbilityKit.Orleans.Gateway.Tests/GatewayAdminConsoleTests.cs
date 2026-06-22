using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class GatewayAdminConsoleTests
{
    [Fact]
    public void Admin_console_should_be_independent_vite_vue_project()
    {
        var packageJson = File.ReadAllText(GetAdminConsoleProjectPath("package.json"));
        var viteConfig = File.ReadAllText(GetAdminConsoleProjectPath("vite.config.ts"));
        var main = File.ReadAllText(GetAdminConsoleProjectPath("src", "main.ts"));

        Assert.Contains("abilitykit-admin-console", packageJson);
        Assert.Contains("\"build\": \"vue-tsc --noEmit && vite build\"", packageJson);
        Assert.Contains("@vitejs/plugin-vue", packageJson);
        Assert.Contains("base: '/admin/'", viteConfig);
        Assert.Contains("outDir: '../Orleans/src/AbilityKit.Orleans.Gateway/wwwroot/admin'", viteConfig);
        Assert.Contains("createApp(App).mount('#app')", main);
    }

    [Fact]
    public void Admin_console_source_should_wrap_api_and_dashboard_calls()
    {
        var apiClient = File.ReadAllText(GetAdminConsoleProjectPath("src", "services", "adminApiClient.ts"));
        var app = File.ReadAllText(GetAdminConsoleProjectPath("src", "App.vue"));

        Assert.Contains("export class AdminApiClient", apiClient);
        Assert.Contains("VITE_ABILITYKIT_GATEWAY_URL", apiClient);
        Assert.Contains("/api/admin/dashboard", app);
        Assert.Contains("refreshDashboard", app);
        Assert.Contains("adminStorage", app);
    }

    [Fact]
    public void Admin_console_source_should_default_to_shooter_operations()
    {
        var app = File.ReadAllText(GetAdminConsoleProjectPath("src", "App.vue"));

        Assert.Contains("selectedRoomType = ref('shooter')", app);
        Assert.Contains("roomType: 'shooter'", app);
        Assert.Contains("gameplayId: 2", app);
        Assert.Contains("worldType: 'shooter_battle'", app);
        Assert.Contains("syncTemplateId: 'pure-state-authority'", app);
        Assert.Contains("startShooterRoomQuick", app);
        Assert.Contains("/api/shooter-sandbox/start", app);
    }

    [Fact]
    public void Admin_console_source_should_have_production_style_layout_css()
    {
        var css = File.ReadAllText(GetAdminConsoleProjectPath("src", "styles.css"));

        Assert.Contains(".shell", css);
        Assert.Contains(".metrics", css);
        Assert.Contains(".grid", css);
        Assert.Contains(".card", css);
        Assert.Contains("@media", css);
    }

    [Fact]
    public void Admin_console_build_output_should_be_served_by_gateway_wwwroot()
    {
        var index = File.ReadAllText(GetGatewaySourcePath("wwwroot", "admin", "index.html"));
        var assetsDirectory = GetGatewayDirectoryPath("wwwroot", "admin", "assets");

        Assert.Contains("AbilityKit Admin Console", index);
        Assert.True(Directory.Exists(assetsDirectory), "Expected Vite build assets under Gateway wwwroot/admin/assets.");
        Assert.True(Directory.EnumerateFiles(assetsDirectory, "*.js").Any(), "Expected at least one built JavaScript asset.");
        Assert.True(Directory.EnumerateFiles(assetsDirectory, "*.css").Any(), "Expected at least one built CSS asset.");
    }

    [Fact]
    public void Gateway_pipeline_should_expose_admin_console_redirect()
    {
        var source = File.ReadAllText(GetGatewaySourcePath("HttpApi", "GatewayModuleExtensions.cs"));

        Assert.Contains("/admin", source);
        Assert.Contains("/admin/index.html", source);
        Assert.Contains("Gateway.AdminConsole", source);
        Assert.Contains("/debug/index.html", source);
    }

    [Fact]
    public void Gateway_api_should_expose_admin_dashboard_endpoint()
    {
        var source = File.ReadAllText(GetGatewaySourcePath("HttpApi", "GatewayHttpApi.cs"));

        Assert.Contains("MapGatewayAdminEndpoints", source);
        Assert.Contains("/api/admin", source);
        Assert.Contains("/dashboard", source);
        Assert.Contains("BuildAdminDashboardAsync", source);
        Assert.Contains("AdminDashboardHttpResponse", source);
        Assert.Contains("GatewayGameplayCatalog.All", source);
    }

    private static string GetAdminConsoleProjectPath(params string[] segments)
    {
        return FindWorkspacePath(Prepend(new[] { "Server", "AdminConsole" }, segments), File.Exists);
    }

    private static string GetGatewaySourcePath(params string[] segments)
    {
        return FindWorkspacePath(Prepend(new[] { "Server", "Orleans", "src", "AbilityKit.Orleans.Gateway" }, segments), File.Exists);
    }

    private static string GetGatewayDirectoryPath(params string[] segments)
    {
        return FindWorkspacePath(Prepend(new[] { "Server", "Orleans", "src", "AbilityKit.Orleans.Gateway" }, segments), Directory.Exists);
    }

    private static string FindWorkspacePath(string[] segments, Func<string, bool> exists)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var parts = new string[1 + segments.Length];
            parts[0] = directory.FullName;
            Array.Copy(segments, 0, parts, 1, segments.Length);
            var candidate = Path.Combine(parts);
            if (exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate workspace path: {Path.Combine(segments)}.");
    }

    private static string[] Prepend(string[] prefix, string[] suffix)
    {
        var result = new string[prefix.Length + suffix.Length];
        Array.Copy(prefix, 0, result, 0, prefix.Length);
        Array.Copy(suffix, 0, result, prefix.Length, suffix.Length);
        return result;
    }
}
