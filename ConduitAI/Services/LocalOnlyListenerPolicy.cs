using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitAI.Services;

public static class LocalOnlyListenerPolicy
{
    private static readonly HashSet<string> AllowedLoopbackHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "[::1]"
    };

    public static void ValidateAllowedHosts(string? allowedHosts)
    {
        var hosts = allowedHosts?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();
        if (hosts.Length == 0 || hosts.Any(host => !AllowedLoopbackHosts.Contains(host)))
        {
            throw new InvalidOperationException(
                "ConduitAI only accepts the localhost, IPv4 loopback, and IPv6 loopback Host names.");
        }
    }

    public static void ValidateConfiguredListeners(
        IConfiguration configuration,
        IWebHostBuilder webHostBuilder) =>
        ValidateConfiguredListeners(
            configuration,
            webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey),
            webHostBuilder.GetSetting(WebHostDefaults.HttpPortsKey),
            webHostBuilder.GetSetting(WebHostDefaults.HttpsPortsKey));

    public static async Task StartAndValidateAsync(
        WebApplication app,
        IConfiguration configuration,
        IWebHostBuilder webHostBuilder,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguredListeners(configuration, webHostBuilder);
        await app.StartAsync(cancellationToken);

        var server = app.Services.GetRequiredService<IServer>();
        var boundAddresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? Array.Empty<string>();
        ValidateBoundListeners(boundAddresses);
    }

    public static void ValidateConfiguredListeners(
        IConfiguration configuration,
        string? hostUrls,
        string? hostHttpPorts = null,
        string? hostHttpsPorts = null)
    {
        var configuredUrls = hostUrls ?? configuration["urls"];
        if (!string.IsNullOrWhiteSpace(configuredUrls))
        {
            ValidateBoundListeners(configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var endpointUrls = configuration.GetSection("Kestrel:Endpoints").GetChildren()
            .Select(endpoint => endpoint["Url"])
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!);
        var configuredEndpoints = endpointUrls.ToArray();
        ValidateBoundListeners(configuredEndpoints);

        var httpPorts = hostHttpPorts ?? configuration["http_ports"];
        var httpsPorts = hostHttpsPorts ?? configuration["https_ports"];
        var hasExplicitListener = !string.IsNullOrWhiteSpace(configuredUrls) || configuredEndpoints.Length > 0;
        if (!hasExplicitListener
            && (!string.IsNullOrWhiteSpace(httpPorts) || !string.IsNullOrWhiteSpace(httpsPorts)))
        {
            throw new InvalidOperationException(
                "ConduitAI only supports HTTP or HTTPS listeners bound to localhost or a loopback IP address. Configure an explicit loopback URL instead of HTTP_PORTS or HTTPS_PORTS.");
        }
    }

    public static void ValidateBoundListeners(IEnumerable<string> addresses)
    {
        foreach (var address in addresses)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
                || uri.Scheme is not ("http" or "https")
                || !IsLoopback(uri.Host))
            {
                throw new InvalidOperationException(
                    "ConduitAI only supports HTTP or HTTPS listeners bound to localhost or a loopback IP address.");
            }
        }
    }

    private static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
}
