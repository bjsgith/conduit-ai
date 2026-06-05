namespace ConduitAI.Services.Ai;

/// <summary>
/// Outcome of an AI-backed operation. Carries a user-safe error message on
/// failure so controllers never surface raw exceptions or prompts.
/// </summary>
public class AiOperationResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    public static AiOperationResult<T> Ok(T value) => new() { Success = true, Value = value };
    public static AiOperationResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}
