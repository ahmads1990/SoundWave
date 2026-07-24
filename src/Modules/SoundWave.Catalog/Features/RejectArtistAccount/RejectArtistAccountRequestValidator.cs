using FluentValidation;

namespace SoundWave.Catalog.Features.RejectArtistAccount;

/// <summary>
/// Provides validation rules for <see cref="RejectArtistAccountRequest"/>.
/// </summary>
internal class RejectArtistAccountRequestValidator : AbstractValidator<RejectArtistAccountRequest>
{
    public RejectArtistAccountRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.");
    }
}
