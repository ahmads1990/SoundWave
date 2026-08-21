using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Albums.PublishAlbum;

/// <summary>
/// Command for publishing an album, making it publicly visible to listeners.
/// </summary>
internal record PublishAlbumCommand(Guid AlbumId)
    : IRequest<Result<CatalogError, Guid>>;
