namespace ConduitAI.Services;

/// <summary>
/// Strongly-typed Ollama configuration bound from the "Ollama" config section.
/// </summary>
public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5-coder:7b";
    public int TimeoutSeconds { get; set; } = 120;
}
