using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.ApplyForArtistAccount;

/// <summary>
/// Command for submitting an artist account application.
/// </summary>
/// <param name="StageName">The desired artist stage name.</param>
/// <param name="Bio">Optional biography or details provided by the applicant.</param>
internal record ApplyForArtistAccountCommand(string StageName, string? Bio) : IRequest<Result<CatalogError, Guid>>;
