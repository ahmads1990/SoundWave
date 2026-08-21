using FluentValidation;

namespace SoundWave.Catalog.Features.CreateAlbum;

/// <summary>
/// Validator for <see cref="CreateAlbumRequest"/>.
/// </summary>
internal class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
{
    public CreateAlbumRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Album title is required.")
            .MaximumLength(200).WithMessage("Album title must not exceed 200 characters.");

        RuleFor(x => x.AlbumType)
            .IsInEnum().WithMessage("Invalid album type specified.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Album description must not exceed 1000 characters.");
    }
}
