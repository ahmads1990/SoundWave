using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.RejectArtistAccount;

/// <summary>
/// Command for rejecting an artist account application with a reason.
/// </summary>
/// <param name="ApplicationId">The unique ID of the application.</param>
/// <param name="Reason">The rejection reason.</param>
internal record RejectArtistAccountCommand(Guid ApplicationId, string Reason) : IRequest<Result<CatalogError, Guid>>;
