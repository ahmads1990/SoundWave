using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.GetAlbum;

/// <summary>
/// Query for retrieving a single album with its ordered tracklist.
/// </summary>
internal record GetAlbumQuery(Guid AlbumId)
    : IRequest<Result<CatalogError, AlbumDetailsDto>>;
