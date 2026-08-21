using FluentValidation;

namespace SoundWave.Catalog.Features.Tracks.EditTrackMetadata;

/// <summary>
/// Validator for <see cref="EditTrackMetadataRequest"/>.
/// </summary>
internal class EditTrackMetadataRequestValidator : AbstractValidator<EditTrackMetadataRequest>
{
    public EditTrackMetadataRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Track title is required.")
            .MaximumLength(200).WithMessage("Track title must not exceed 200 characters.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0).WithMessage("Track duration must be greater than 0 seconds.");
    }
}
