using ConduitAI.Services;
using Xunit;

namespace ConduitAI.Tests;

public class AiRequestLimiterTests
{
    [Fact]
    public async Task AcquireAsync_AllowsOneActiveAndOneQueuedRequest()
    {
        using var limiter = new AiRequestLimiter();
        using var first = await limiter.AcquireAsync("127.0.0.1");
        Assert.True(first.IsAcquired);

        var queuedTask = limiter.AcquireAsync("127.0.0.1").AsTask();
        await Task.Delay(30);
        using var overflow = await limiter.AcquireAsync("127.0.0.1");
        Assert.False(overflow.IsAcquired);

        first.Dispose();
        using var queued = await queuedTask;
        Assert.True(queued.IsAcquired);
    }

    [Fact]
    public async Task AcquireAsync_UsesAnIndependentQuotaPerClient()
    {
        using var limiter = new AiRequestLimiter();
        for (var i = 0; i < 5; i++)
        {
            using var lease = await limiter.AcquireAsync("client-one");
            Assert.True(lease.IsAcquired);
        }

        using var limited = await limiter.AcquireAsync("client-one");
        using var anotherClient = await limiter.AcquireAsync("client-two");

        Assert.False(limited.IsAcquired);
        Assert.True(anotherClient.IsAcquired);
    }

    [Fact]
    public async Task AcquireAsync_BoundsActiveAndQueuedRequestsAcrossClients()
    {
        using var limiter = new AiRequestLimiter();
        using var first = await limiter.AcquireAsync("client-one");
        Assert.True(first.IsAcquired);

        var queuedTask = limiter.AcquireAsync("client-two").AsTask();
        Assert.False(queuedTask.IsCompleted);

        using var overflow = await limiter.AcquireAsync("client-three");
        Assert.False(overflow.IsAcquired);

        first.Dispose();
        using var queued = await queuedTask;
        Assert.True(queued.IsAcquired);
    }
}
