using FluentValidation;

namespace SoundWave.Catalog.Features.Tracks.MoveTrackToAlbum;

/// <summary>
/// Validator for <see cref="MoveTrackToAlbumRequest"/>.
/// </summary>
internal class MoveTrackToAlbumRequestValidator : AbstractValidator<MoveTrackToAlbumRequest>
{
    public MoveTrackToAlbumRequestValidator()
    {
        RuleFor(x => x.TargetAlbumId)
            .NotEmpty().WithMessage("Target album ID is required.");
    }
}
