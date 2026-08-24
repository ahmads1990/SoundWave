using FluentValidation;

namespace SoundWave.Playlist.Features.Tracks.AddTrackToPlaylist;

public class AddTrackToPlaylistRequestValidator : AbstractValidator<AddTrackToPlaylistRequest>
{
    public AddTrackToPlaylistRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty()
            .WithMessage("Track ID is required.");
    }
}
