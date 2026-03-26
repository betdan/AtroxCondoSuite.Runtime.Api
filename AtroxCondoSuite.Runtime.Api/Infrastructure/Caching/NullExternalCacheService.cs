namespace AtroxCondoSuite.Runtime.Api.Infrastructure.Caching
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching;

    public sealed class NullExternalCacheService : IExternalCacheService
    {
        public Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

