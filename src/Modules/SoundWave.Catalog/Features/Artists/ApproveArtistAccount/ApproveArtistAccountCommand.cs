using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Artists.ApproveArtistAccount;

/// <summary>
/// Command for approving an artist account application and creating an artist profile.
/// </summary>
/// <param name="ApplicationId">The unique ID of the artist account application.</param>
internal record ApproveArtistAccountCommand(Guid ApplicationId) : IRequest<Result<CatalogError, Guid>>;
