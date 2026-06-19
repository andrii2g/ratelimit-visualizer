using RateLimitVisualizer.Core.Models;

namespace RateLimitVisualizer.Api.Storage;

public interface IObservationRepository
{
    Task InsertAsync(ObservedApiCall observation, DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default);

    Task<ObservedApiCall?> GetLatestAsync(string provider, string consumer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObservedApiCall>> GetObservationsSinceAsync(string provider, string consumer, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObservedApiCall>> GetRecentAsync(string provider, string consumer, int minutes, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
