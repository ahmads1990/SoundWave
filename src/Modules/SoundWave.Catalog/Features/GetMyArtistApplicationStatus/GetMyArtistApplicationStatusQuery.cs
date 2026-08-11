using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.GetMyArtistApplicationStatus;

internal record GetMyArtistApplicationStatusQuery : IRequest<Result<CatalogError, ArtistApplicationStatusDto>>;
