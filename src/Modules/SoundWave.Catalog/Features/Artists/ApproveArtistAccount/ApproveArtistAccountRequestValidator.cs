using FluentValidation;

namespace SoundWave.Catalog.Features.Artists.ApproveArtistAccount;

/// <summary>
/// Provides validation rules for the <see cref="ApproveArtistAccountRequest"/>.
/// </summary>
internal class ApproveArtistAccountRequestValidator : AbstractValidator<ApproveArtistAccountRequest>
{
    public ApproveArtistAccountRequestValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");
    }
}
