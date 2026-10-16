using FluentValidation;

namespace SoundWave.Catalog.Features.Tracks.UpdateTrack;

/// <summary>
/// Validator for <see cref="UpdateTrackRequest"/>.
/// </summary>
internal class UpdateTrackRequestValidator : AbstractValidator<UpdateTrackRequest>
{
    public UpdateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Track title is required.")
            .MaximumLength(200).WithMessage("Track title cannot exceed 200 characters.");
    }
}
