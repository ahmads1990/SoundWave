using FluentValidation;

namespace SoundWave.Catalog.Features.Albums.EditAlbum;

/// <summary>
/// Validator for <see cref="EditAlbumRequest"/>.
/// </summary>
internal class EditAlbumRequestValidator : AbstractValidator<EditAlbumRequest>
{
    public EditAlbumRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(500).WithMessage("Cover image URL cannot exceed 500 characters.")
            .When(x => x.CoverImageUrl != null);
    }
}
