using FluentValidation;

namespace SoundWave.Catalog.Features.UpdateGenre;

/// <summary>
/// Provides validation rules for the <see cref="UpdateGenreRequest"/>.
/// </summary>
internal class UpdateGenreRequestValidator : AbstractValidator<UpdateGenreRequest>
{
    public UpdateGenreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Genre name is required.")
            .MaximumLength(50).WithMessage("Genre name must not exceed 50 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid genre type. Allowed values are 0 (Genre) or 1 (Mood).");
    }
}
