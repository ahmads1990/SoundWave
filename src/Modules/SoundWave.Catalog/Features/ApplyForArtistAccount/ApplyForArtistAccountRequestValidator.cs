using FluentValidation;

namespace SoundWave.Catalog.Features.ApplyForArtistAccount;

/// <summary>
/// Provides validation rules for the <see cref="ApplyForArtistAccountRequest"/>.
/// </summary>
internal class ApplyForArtistAccountRequestValidator : AbstractValidator<ApplyForArtistAccountRequest>
{
    public ApplyForArtistAccountRequestValidator()
    {
        RuleFor(x => x.StageName)
            .NotEmpty().WithMessage("Stage name is required.")
            .MaximumLength(100).WithMessage("Stage name must not exceed 100 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Bio));
    }
}
