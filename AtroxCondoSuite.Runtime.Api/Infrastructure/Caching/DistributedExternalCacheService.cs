namespace AtroxCondoSuite.Runtime.Api.Infrastructure.Caching
{
    using AtroxCondoSuite.Runtime.Api.Application.Contracts.Caching;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    public sealed class DistributedExternalCacheService(
        IDistributedCache cache,
        ILogger<DistributedExternalCacheService> logger) : IExternalCacheService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        private readonly ILogger<DistributedExternalCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = await _cache.GetStringAsync(key, cancellationToken);
                return string.IsNullOrWhiteSpace(payload)
                    ? default
                    : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External cache read failed for key {Key}. Continuing without cache.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(value, SerializerOptions);
                await _cache.SetStringAsync(key, payload, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External cache write failed for key {Key}. Continuing without cache.", key);
            }
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                return _cache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External cache remove failed for key {Key}. Continuing without cache.", key);
                return Task.CompletedTask;
            }
        }
    }
}

