using FluentValidation;

namespace SoundWave.Playlist.Features.Playlists.EditPlaylist;

/// <summary>
/// Validator for <see cref="EditPlaylistRequest"/>.
/// </summary>
public class EditPlaylistRequestValidator : AbstractValidator<EditPlaylistRequest>
{
    public EditPlaylistRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Playlist title is required.")
            .MaximumLength(100).WithMessage("Playlist title cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Playlist description cannot exceed 1000 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum().WithMessage("Invalid playlist visibility setting.");
    }
}
