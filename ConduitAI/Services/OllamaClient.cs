using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConduitAI.Services.Ai;
using ConduitAI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ConduitAI.Services;

/// <summary>
/// Thin HTTP wrapper over the local Ollama generate API. Connection and timeout
/// failures are converted into a failed <see cref="OllamaResult"/> with a
/// user-safe message; no internal details are leaked.
/// </summary>
public class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient> _logger;
    private readonly bool _hasSafeLocalBaseUrl;
    private readonly string _baseUrlDescription;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 600));

        _hasSafeLocalBaseUrl = TryCreateSafeLocalBaseUri(_options.BaseUrl, out var baseUri, out _baseUrlDescription);
        if (_hasSafeLocalBaseUrl)
        {
            _http.BaseAddress = baseUri;
        }
    }

    public string ModelName => _options.Model;

    public async Task<OllamaResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (!_hasSafeLocalBaseUrl)
        {
            _logger.LogWarning("Refusing to call Ollama because the configured BaseUrl is not local. Host: {Host}.", _baseUrlDescription);
            return OllamaResult.Fail(
                "AI features are unavailable because Ollama must be configured on this computer. " +
                "Use a local URL such as http://localhost:11434.");
        }

        var request = new GenerateRequest
        {
            Model = _options.Model,
            Prompt = prompt,
            Stream = false,
            Format = "json"
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("/api/generate", request, JsonOpts, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned status {Status} for model {Model}.", (int)response.StatusCode, _options.Model);
                return OllamaResult.Fail(
                    $"The local model '{_options.Model}' could not be reached (HTTP {(int)response.StatusCode}). " +
                    "Confirm Ollama is running and the model is installed.");
            }

            var body = await response.Content.ReadFromJsonAsync<GenerateResponse>(JsonOpts, ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Response))
            {
                return OllamaResult.Fail("The local model returned an empty response.");
            }

            if (!body.Done)
            {
                return OllamaResult.Fail("The local model returned an incomplete response. Please try again.");
            }

            return OllamaResult.Ok(body.Response);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama request timed out after {Seconds}s.", _options.TimeoutSeconds);
            return OllamaResult.Fail("The local model timed out. It may still be loading; please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not connect to Ollama host {Host}.", _baseUrlDescription);
            return OllamaResult.Fail(
                "Ollama is not available. Start it with 'ollama serve' and confirm the model is installed.");
        }
        catch (JsonException)
        {
            return OllamaResult.Fail("The local model returned an invalid response.");
        }
        catch (NotSupportedException)
        {
            return OllamaResult.Fail("The local model returned an unsupported response.");
        }
    }

    private static bool TryCreateSafeLocalBaseUri(string rawBaseUrl, out Uri? baseUri, out string hostDescription)
    {
        baseUri = null;
        hostDescription = "(invalid)";

        if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        hostDescription = string.IsNullOrWhiteSpace(uri.Host) ? "(missing)" : uri.Host;
        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (!IsLoopbackHost(uri.Host))
        {
            return false;
        }

        baseUri = uri;
        return true;
    }

    private static bool IsLoopbackHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
    }

    private sealed class GenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }
    }

    private sealed class GenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
