using Microsoft.Extensions.Caching.Memory;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Services;

/// <summary>
/// <see cref="ICachingService"/> implementation backed by <see cref="IMemoryCache"/>.
/// Used as a drop-in replacement for <see cref="CachingService"/> (Redis) when running
/// in demo/dev environments without a Redis instance.
/// </summary>
public class MemoryCachingService : ICachingService
{
    #region Fields

    private readonly IMemoryCache _memoryCache;

    #endregion

    #region Constructors

    public MemoryCachingService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(key, out string? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task AddAsync(string key, string value, TimeSpan? timeToLive, CancellationToken cancellationToken = default)
    {
        if (timeToLive.HasValue)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive
            };
            _memoryCache.Set(key, value, options);
        }
        else
        {
            _memoryCache.Set(key, value);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    #endregion
}
