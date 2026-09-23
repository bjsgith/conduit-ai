using System.Threading.RateLimiting;

namespace ConduitAI.Services;

/// <summary>Limits local model work globally and per caller, including retries issued by one user action.</summary>
public sealed class AiRequestLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<string, string>(clientKey =>
            RateLimitPartition.GetTokenBucketLimiter(clientKey, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                TokensPerPeriod = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })),
        PartitionedRateLimiter.Create<string, string>(_ =>
            RateLimitPartition.GetConcurrencyLimiter("ollama-global", _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })),
        PartitionedRateLimiter.Create<string, string>(clientKey =>
            RateLimitPartition.GetConcurrencyLimiter(clientKey, _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            })));

    public ValueTask<RateLimitLease> AcquireAsync(string clientKey, CancellationToken cancellationToken = default) =>
        _limiter.AcquireAsync(clientKey, 1, cancellationToken);

    public void Dispose() => _limiter.Dispose();
}

public static class OllamaHttpClientPolicy
{
    public static HttpMessageHandler CreatePrimaryHandler() => new HttpClientHandler { AllowAutoRedirect = false };
}
