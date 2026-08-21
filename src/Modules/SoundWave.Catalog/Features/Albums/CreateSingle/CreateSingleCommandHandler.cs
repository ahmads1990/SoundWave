using MediatR;
using Microsoft.Extensions.Logging;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Features.Albums.CreateAlbum;
using SoundWave.Catalog.Features.Tracks.CreateTrack;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Albums.CreateSingle;

/// <summary>
/// Handles 1-step single release creation by orchestrating CreateAlbumCommand and CreateTrackCommand.
/// </summary>
internal class CreateSingleCommandHandler(
    ISender sender,
    ILogger<CreateSingleCommandHandler> logger)
    : IRequestHandler<CreateSingleCommand, Result<CatalogError, CreateSingleResponse>>
{
    public async Task<Result<CatalogError, CreateSingleResponse>> Handle(
        CreateSingleCommand request,
        CancellationToken cancellationToken)
    {
        var createAlbumCommand = new CreateAlbumCommand(
            Title: request.Title,
            AlbumType: AlbumType.Single,
            ReleaseDate: request.ReleaseDate,
            CoverImageUrl: request.CoverImageUrl,
            Description: request.Description,
            GenreIds: request.GenreIds,
            FeaturedArtistIds: request.FeaturedArtistIds);

        var albumResult = await sender.Send(createAlbumCommand, cancellationToken);
        if (!albumResult.IsSuccess)
            return Result<CatalogError, CreateSingleResponse>.Failure(albumResult.Error, albumResult.ErrorMessage);

        var albumId = albumResult.Data;

        var createTrackCommand = new CreateTrackCommand(
            AlbumId: albumId,
            Title: request.Title,
            DurationSeconds: request.DurationSeconds,
            GenreIds: request.GenreIds,
            FeaturedArtistIds: request.FeaturedArtistIds);

        var trackResult = await sender.Send(createTrackCommand, cancellationToken);
        if (!trackResult.IsSuccess)
            return Result<CatalogError, CreateSingleResponse>.Failure(trackResult.Error, trackResult.ErrorMessage);

        var trackId = trackResult.Data;

        logger.LogInformation("Single release '{Title}' created with Album {AlbumId} and Track {TrackId}", request.Title, albumId, trackId);

        return Result<CatalogError, CreateSingleResponse>.Success(new CreateSingleResponse(albumId, trackId));
    }
}
