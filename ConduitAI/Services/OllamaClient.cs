using System.Net.Http.Json;
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

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public string ModelName => _options.Model;

    public async Task<OllamaResult> GenerateAsync(string prompt, CancellationToken ct = default)
    {
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

            return OllamaResult.Ok(body.Response);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama request timed out after {Seconds}s.", _options.TimeoutSeconds);
            return OllamaResult.Fail("The local model timed out. It may still be loading; please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not connect to Ollama at {BaseUrl}.", _options.BaseUrl);
            return OllamaResult.Fail(
                "Ollama is not available. Start it with 'ollama serve' and confirm the model is installed.");
        }
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
