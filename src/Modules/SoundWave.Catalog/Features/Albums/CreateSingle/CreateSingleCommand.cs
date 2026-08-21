using MediatR;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Features.Albums.CreateSingle;

/// <summary>
/// Command to create a single release (Album of type Single + Track #1) atomically.
/// </summary>
public record CreateSingleCommand(
    string Title,
    DateTime? ReleaseDate = null,
    string? CoverImageUrl = null,
    string? Description = null,
    int DurationSeconds = 0,
    List<int>? GenreIds = null,
    List<Guid>? FeaturedArtistIds = null
) : IRequest<Result<CatalogError, CreateSingleResponse>>;
