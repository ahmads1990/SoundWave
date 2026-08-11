using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.GetArtistProfile;

internal record GetArtistProfileQuery(Guid ArtistId)
    : IRequest<Result<CatalogError, ArtistProfileDto>>;
