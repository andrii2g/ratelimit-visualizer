namespace RateLimitVisualizer.Core.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
