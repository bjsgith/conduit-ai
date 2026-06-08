using ConduitAI.Services.Ai;

namespace ConduitAI.Services.Interfaces;

public interface IOllamaClient
{
    /// <summary>
    /// Sends a prompt to the local Ollama model and returns the raw text
    /// response. Network/availability failures are returned as a failed
    /// <see cref="OllamaResult"/> rather than thrown.
    /// </summary>
    Task<OllamaResult> GenerateAsync(string prompt, CancellationToken ct = default);

    string ModelName { get; }
}
