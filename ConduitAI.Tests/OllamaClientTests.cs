using System.Net;
using System.Text;
using ConduitAI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConduitAI.Tests;

public class OllamaClientTests
{
    [Fact]
    public async Task GenerateAsync_NonLoopbackBaseUrl_FailsWithoutHttpCall()
    {
        var handler = new CannedHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = NewClient(handler, new OllamaOptions { BaseUrl = "https://api.example.com", Model = "test-model" });

        var result = await client.GenerateAsync("private lead prompt");

        Assert.False(result.Success);
        Assert.Contains("Ollama must be configured on this computer", result.ErrorMessage);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_MalformedWrapperJson_FailsSafely()
    {
        var handler = new CannedHandler(JsonResponse("{ not json"));
        var client = NewClient(handler);

        var result = await client.GenerateAsync("prompt");

        Assert.False(result.Success);
        Assert.Contains("invalid response", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_DoneFalse_FailsAsIncomplete()
    {
        var handler = new CannedHandler(JsonResponse("""
        {"response":"{\"summary\":\"partial\"}","done":false}
        """));
        var client = NewClient(handler);

        var result = await client.GenerateAsync("prompt");

        Assert.False(result.Success);
        Assert.Contains("incomplete", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_EmptyModelResponse_Fails()
    {
        var handler = new CannedHandler(JsonResponse("""
        {"response":"   ","done":true}
        """));
        var client = NewClient(handler);

        var result = await client.GenerateAsync("prompt");

        Assert.False(result.Success);
        Assert.Contains("empty response", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_NonSuccessStatus_FailsSafely()
    {
        var handler = new CannedHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = NewClient(handler);

        var result = await client.GenerateAsync("prompt");

        Assert.False(result.Success);
        Assert.Contains("could not be reached", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_Timeout_FailsSafely()
    {
        var handler = new DelayedHandler();
        var client = NewClient(handler, new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            Model = "test-model",
            TimeoutSeconds = 1
        });

        var result = await client.GenerateAsync("prompt");

        Assert.False(result.Success);
        Assert.Contains("timed out", result.ErrorMessage);
    }

    private static OllamaClient NewClient(HttpMessageHandler handler, OllamaOptions? options = null)
    {
        var http = new HttpClient(handler);
        return new OllamaClient(
            http,
            Options.Create(options ?? new OllamaOptions { BaseUrl = "http://localhost:11434", Model = "test-model" }),
            NullLogger<OllamaClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class CannedHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public CannedHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class DelayedHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return JsonResponse("""{"response":"{}","done":true}""");
        }
    }
}
