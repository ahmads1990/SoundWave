using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Catalog.Extensions;

/// <summary>
/// Maps <see cref="CatalogError"/> values to <see cref="ApiErrorCode"/> values
/// for use in endpoint response mapping.
/// </summary>
internal static class CatalogErrorExtensions
{
    /// <summary>
    /// Converts a <see cref="CatalogError"/> to the corresponding <see cref="ApiErrorCode"/>.
    /// </summary>
    public static ApiErrorCode ToApiErrorCode(this CatalogError error) => error switch
    {
        CatalogError.GenreAlreadyExists             => ApiErrorCode.ValidationFailed,
        CatalogError.GenreNotFound                  => ApiErrorCode.ResourceNotFound,
        CatalogError.ArtistApplicationAlreadyExists  => ApiErrorCode.ValidationFailed,
        CatalogError.ArtistApplicationNotFound       => ApiErrorCode.ResourceNotFound,
        CatalogError.ArtistApplicationAlreadyProcessed => ApiErrorCode.ValidationFailed,
        CatalogError.ArtistNotFound                 => ApiErrorCode.ResourceNotFound,
        CatalogError.UserNotAuthenticated           => ApiErrorCode.Unauthorized,
        CatalogError.AlbumNotFound                  => ApiErrorCode.ResourceNotFound,
        CatalogError.TrackNotFound                  => ApiErrorCode.ResourceNotFound,
        CatalogError.AlbumAlreadyPublished          => ApiErrorCode.ValidationFailed,
        CatalogError.CannotPublishEmptyAlbum        => ApiErrorCode.ValidationFailed,
        CatalogError.UnauthorizedAlbumAccess        => ApiErrorCode.Forbidden,
        CatalogError.UnauthorizedTrackAccess        => ApiErrorCode.Forbidden,
        CatalogError.InvalidGenreId                 => ApiErrorCode.ValidationFailed,
        CatalogError.InternalError                  => ApiErrorCode.InternalServerError,
        _                                           => ApiErrorCode.InternalServerError,
    };
}
