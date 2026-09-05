using FluentValidation;

namespace SoundWave.Identity.Features.Account.UpdateProfileImages;

/// <summary>
/// Validates input for the <see cref="UpdateProfileImagesRequest"/>.
/// </summary>
public class UpdateProfileImagesRequestValidator : AbstractValidator<UpdateProfileImagesRequest>
{
    public UpdateProfileImagesRequestValidator()
    {
        RuleFor(x => x.ProfilePicUrl)
            .MaximumLength(500)
            .WithMessage("Profile picture URL cannot exceed 500 characters.");

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(500)
            .WithMessage("Cover image URL cannot exceed 500 characters.");
    }
}
