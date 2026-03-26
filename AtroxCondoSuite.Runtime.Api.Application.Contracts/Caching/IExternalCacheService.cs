namespace AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching
{
    public interface IExternalCacheService
    {
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}

