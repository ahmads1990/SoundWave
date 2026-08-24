using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Contracts.IntegrationEvents;
using SoundWave.Playlist.Common;
using SoundWave.Playlist.Common.Enums;
using SoundWave.Playlist.Data;
using SoundWave.Playlist.Data.Entities;

namespace SoundWave.Playlist.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="UserRegisteredEvent"/> from the Identity module to automatically provision
/// the system "Liked Songs" playlist for every new user upon registration.
/// </summary>
internal sealed class UserRegisteredConsumer(
    PlaylistDbContext dbContext,
    ILogger<UserRegisteredConsumer> logger)
    : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var userId = context.Message.UserId;
        var cancellationToken = context.CancellationToken;

        var alreadyExists = await dbContext.Playlists
            .AnyAsync(p => p.OwnerId == userId && p.IsSystem, cancellationToken);

        if (alreadyExists)
        {
            logger.LogInformation("Liked Songs playlist already exists for user {UserId} — skipping", userId);
            return;
        }

        await ProvisionLikedSongsPlaylistAsync(userId, cancellationToken);
    }

    #region Private Methods

    private async Task ProvisionLikedSongsPlaylistAsync(Guid userId, CancellationToken cancellationToken)
    {
        var playlist = new Data.Entities.Playlist
        {
            OwnerId = userId,
            Title = Constants.LikedSongsPlaylistTitle,
            Visibility = PlaylistVisibility.Private,
            IsSystem = true,
            TrackCount = 0
        };

        dbContext.Playlists.Add(playlist);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Liked Songs playlist {PlaylistId} provisioned for user {UserId}", playlist.Id, userId);
    }

    #endregion
}
