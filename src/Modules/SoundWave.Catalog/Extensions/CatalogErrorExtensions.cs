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
        CatalogError.GenreAlreadyExists => ApiErrorCode.ValidationFailed,
        CatalogError.GenreNotFound      => ApiErrorCode.ResourceNotFound,
        CatalogError.InternalError      => ApiErrorCode.InternalServerError,
        _                               => ApiErrorCode.InternalServerError,
    };
}
