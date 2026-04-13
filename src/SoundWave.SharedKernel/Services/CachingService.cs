using StackExchange.Redis;
using SoundWave.SharedKernel.Interfaces;

namespace SoundWave.SharedKernel.Services;

public class CachingService : ICachingService
{
    #region Fields

    private readonly IDatabase _database;

    #endregion

    #region Constructors

    public CachingService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.StringGetAsync(key);
    }

    /// <inheritdoc />
    public async Task AddAsync(string key, string value, TimeSpan? timeToLive, CancellationToken cancellationToken = default)
    {
        if (timeToLive.HasValue)
        {
            await _database.StringSetAsync(key, value, (StackExchange.Redis.Expiration)timeToLive.Value);
        }
        else
        {
            await _database.StringSetAsync(key, value);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }

    #endregion
}
