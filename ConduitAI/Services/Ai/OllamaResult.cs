namespace ConduitAI.Services.Ai;

/// <summary>
/// Result of a single Ollama generation call. Failures are represented as data
/// rather than exceptions so callers can fail gracefully.
/// </summary>
public class OllamaResult
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    public static OllamaResult Ok(string content) => new() { Success = true, Content = content };
    public static OllamaResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
