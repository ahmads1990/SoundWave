using FluentValidation;

namespace SoundWave.Catalog.Features.CreateTrack;

/// <summary>
/// Validator for <see cref="CreateTrackRequest"/>.
/// </summary>
internal class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Track title is required.")
            .MaximumLength(200).WithMessage("Track title must not exceed 200 characters.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("Track duration must be 0 or greater.");
    }
}
