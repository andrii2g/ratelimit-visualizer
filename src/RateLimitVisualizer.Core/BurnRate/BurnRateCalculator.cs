using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Core.BurnRate;

public sealed class BurnRateCalculator
{
    public double CalculateCurrentRatePerMinute(IEnumerable<ObservedApiCall> observations, DateTimeOffset nowUtc, int windowMinutes = 5)
    {
        if (windowMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMinutes), "Window minutes must be positive.");
        }

        var start = nowUtc.AddMinutes(-windowMinutes);
        var count = observations.Count(observation => observation.ObservedAtUtc >= start && observation.ObservedAtUtc <= nowUtc);
        return count / (double)windowMinutes;
    }
}
