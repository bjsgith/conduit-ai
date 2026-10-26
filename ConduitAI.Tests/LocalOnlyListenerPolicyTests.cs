using ConduitAI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ConduitAI.Tests;

public class LocalOnlyListenerPolicyTests
{
    [Fact]
    public void ValidateAllowedHosts_AcceptsTheExplicitLoopbackNames() =>
        LocalOnlyListenerPolicy.ValidateAllowedHosts("localhost;127.0.0.1;[::1]");

    [Theory]
    [InlineData("*")]
    [InlineData("localhost;example.com")]
    [InlineData("")]
    public void ValidateAllowedHosts_RejectsWildcardAndRebindingHosts(string hosts) =>
        Assert.Throws<InvalidOperationException>(() => LocalOnlyListenerPolicy.ValidateAllowedHosts(hosts));

    [Theory]
    [InlineData("http://localhost:5229")]
    [InlineData("https://127.0.0.1:7199")]
    [InlineData("http://[::1]:5229")]
    public void ValidateBoundListeners_AcceptsLoopback(string address) =>
        LocalOnlyListenerPolicy.ValidateBoundListeners(new[] { address });

    [Theory]
    [InlineData("http://0.0.0.0:5229")]
    [InlineData("http://[::]:5229")]
    [InlineData("http://+:5229")]
    [InlineData("http://example.com:5229")]
    public void ValidateBoundListeners_RejectsWildcardAndNonLoopback(string address) =>
        Assert.Throws<InvalidOperationException>(() => LocalOnlyListenerPolicy.ValidateBoundListeners(new[] { address }));

    [Fact]
    public void ValidateConfiguredListeners_RejectsKestrelEndpointEvenWhenUrlsAreLocal()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Public:Url"] = "http://0.0.0.0:8080"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.ValidateConfiguredListeners(configuration, "http://localhost:5229"));
    }

    [Fact]
    public void ValidateConfiguredListeners_RejectsWildcardHostUrls()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.ValidateConfiguredListeners(configuration, "http://0.0.0.0:5229"));
    }

    [Theory]
    [InlineData("http_ports", "8080")]
    [InlineData("https_ports", "8443")]
    public void ValidateConfiguredListeners_RejectsEffectivePortOnlySettings(string key, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.ValidateConfiguredListeners(configuration, string.Empty));
    }

    [Theory]
    [InlineData("8080", null)]
    [InlineData(null, "8443")]
    public void ValidateConfiguredListeners_RejectsHostPortOnlySettings(string? httpPorts, string? httpsPorts)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.ValidateConfiguredListeners(
                configuration,
                string.Empty,
                httpPorts,
                httpsPorts));
    }

    [Fact]
    public void ValidateConfiguredListeners_AllowsPortSettingsOverriddenByExplicitLoopbackUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        LocalOnlyListenerPolicy.ValidateConfiguredListeners(
            configuration,
            "http://localhost:5229",
            "8080",
            "8443");
    }

    [Fact]
    public void ValidateConfiguredListeners_AllowsPortSettingsOverriddenByLoopbackKestrelEndpoint()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Local:Url"] = "http://localhost:5229"
            })
            .Build();

        LocalOnlyListenerPolicy.ValidateConfiguredListeners(
            configuration,
            string.Empty,
            "8080",
            "8443");
    }

    [Fact]
    public void ValidateConfiguredListeners_RejectsEffectiveCommandLineWildcardUrl()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { "--urls=http://0.0.0.0:5229" }
        });

        Assert.Throws<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.ValidateConfiguredListeners(builder.Configuration, builder.WebHost));
    }

    [Fact]
    public async Task StartAndValidateAsync_RejectsPortOnlySettingBeforeKestrelAcceptsRequests()
    {
        var port = GetAvailableLoopbackPort();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { "--urls=", $"--http_ports={port}" }
        });
        await using var app = builder.Build();
        app.MapGet("/", () => "started");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LocalOnlyListenerPolicy.StartAndValidateAsync(app, builder.Configuration, builder.WebHost));

        using var client = new HttpClient(new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromMilliseconds(500) });
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{port}"));
    }

    [Fact]
    public async Task StartAndValidateAsync_AcceptsAnExplicitLoopbackListener()
    {
        var port = GetAvailableLoopbackPort();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = new[] { $"--urls=http://127.0.0.1:{port}" }
        });
        await using var app = builder.Build();
        app.MapGet("/", () => "started");

        await LocalOnlyListenerPolicy.StartAndValidateAsync(app, builder.Configuration, builder.WebHost);

        using var client = new HttpClient();
        using var response = await client.GetAsync($"http://127.0.0.1:{port}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    private static int GetAvailableLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
