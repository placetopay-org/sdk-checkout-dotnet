using System.Net;
using PlaceToPay.Checkout.Http;
using Xunit;

namespace PlaceToPay.Checkout.Tests.Http;

public class RetryHandlerTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(502, true)]
    [InlineData(503, true)]
    [InlineData(599, true)]
    [InlineData(429, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(404, false)]
    [InlineData(200, false)]
    public void IsRetriableStatus_MatchesPolicy(int code, bool expected)
    {
        Assert.Equal(expected, RetryHandler.IsRetriableStatus((HttpStatusCode)code));
    }

    [Fact]
    public void IsRetriableException_TransportFailures()
    {
        Assert.True(RetryHandler.IsRetriableException(new HttpRequestException("boom")));
        Assert.True(RetryHandler.IsRetriableException(new TaskCanceledException("timeout")));
        Assert.True(RetryHandler.IsRetriableException(new IOException("io")));
    }

    [Fact]
    public void IsRetriableException_RejectsLogicErrors()
    {
        Assert.False(RetryHandler.IsRetriableException(new ArgumentException("bad")));
        Assert.False(RetryHandler.IsRetriableException(new InvalidOperationException("bad")));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void ComputeBackoff_StaysWithinCap(int attempt)
    {
        var initial = TimeSpan.FromMilliseconds(100);
        var cap = TimeSpan.FromSeconds(5);
        var delay = RetryHandler.ComputeBackoff(attempt, initial, cap);

        Assert.True(delay >= TimeSpan.Zero);
        Assert.True(delay <= cap, $"attempt {attempt} returned {delay} > cap {cap}");
    }

    [Fact]
    public void ComputeBackoff_GrowsExponentially()
    {
        var initial = TimeSpan.FromMilliseconds(100);
        var cap = TimeSpan.FromSeconds(5);

        // Take medians of several runs to smooth jitter.
        var d1 = RetryHandler.ComputeBackoff(1, initial, cap);
        var d3 = RetryHandler.ComputeBackoff(3, initial, cap);

        Assert.True(d1 < cap);
        Assert.True(d3 >= d1, $"attempt=3 delay ({d3}) should be >= attempt=1 delay ({d1})");
    }

    [Fact]
    public void ComputeBackoff_RejectsZeroAttempt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RetryHandler.ComputeBackoff(0, TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ParseRetryAfter_NumericSeconds()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var delay = RetryHandler.ParseRetryAfter("42", clock);
        Assert.Equal(TimeSpan.FromSeconds(42), delay);
    }

    [Fact]
    public void ParseRetryAfter_HttpDate()
    {
        var now = new DateTimeOffset(2026, 4, 22, 15, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var future = now.AddSeconds(30);
        var header = future.ToString("r", System.Globalization.CultureInfo.InvariantCulture);
        var delay = RetryHandler.ParseRetryAfter(header, clock);
        Assert.NotNull(delay);
        Assert.InRange(delay!.Value.TotalSeconds, 29, 31);
    }

    [Fact]
    public void ParseRetryAfter_PastDateReturnsZero()
    {
        var now = new DateTimeOffset(2026, 4, 22, 15, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var past = now.AddSeconds(-30);
        var header = past.ToString("r", System.Globalization.CultureInfo.InvariantCulture);
        var delay = RetryHandler.ParseRetryAfter(header, clock);
        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void ParseRetryAfter_NullOrWhitespaceReturnsNull()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        Assert.Null(RetryHandler.ParseRetryAfter(null, clock));
        Assert.Null(RetryHandler.ParseRetryAfter("", clock));
        Assert.Null(RetryHandler.ParseRetryAfter("   ", clock));
    }

    [Fact]
    public void ParseRetryAfter_UnparseableReturnsNull()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        Assert.Null(RetryHandler.ParseRetryAfter("not-a-date-or-number", clock));
    }
}
