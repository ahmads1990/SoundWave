using FluentValidation;

namespace SoundWave.Playlist.Features.Tracks.ReorderPlaylistTracks;

public class ReorderPlaylistTracksRequestValidator : AbstractValidator<ReorderPlaylistTracksRequest>
{
    public ReorderPlaylistTracksRequestValidator()
    {
        RuleFor(x => x.TrackId)
            .NotEmpty()
            .WithMessage("Track ID is required.");

        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(1)
            .WithMessage("New position must be at least 1.");
    }
}
