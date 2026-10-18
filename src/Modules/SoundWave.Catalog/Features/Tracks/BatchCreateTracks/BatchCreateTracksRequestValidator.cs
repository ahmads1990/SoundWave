using FluentValidation;
using SoundWave.SharedKernel.Storage;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.Catalog.Features.Tracks.BatchCreateTracks;

/// <summary>
/// Validator for <see cref="BatchCreateTracksRequest"/>.
/// </summary>
internal class BatchCreateTracksRequestValidator : AbstractValidator<BatchCreateTracksRequest>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".aac", ".m4a"
    };

    public const int MaxBatchSize = 50;
    public const int MaxGenresPerTrack = 10;
    public const int MaxFeaturedArtistsPerTrack = 10;

    public BatchCreateTracksRequestValidator()
    {
        RuleFor(x => x.Tracks)
            .NotEmpty().WithMessage("At least one track must be provided.")
            .Must(t => t == null || t.Count <= MaxBatchSize)
            .WithMessage($"Cannot create more than {MaxBatchSize} tracks in a single batch.");

        RuleForEach(x => x.Tracks).ChildRules(track =>
        {
            track.RuleFor(t => t.Title)
                .NotEmpty().WithMessage("Track title is required.")
                .MaximumLength(200).WithMessage("Track title must not exceed 200 characters.");

            track.RuleFor(t => t.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.")
                .Must(HaveAllowedExtension).WithMessage("Only .mp3, .flac, .wav, .aac, and .m4a audio formats are allowed.");

            track.RuleFor(t => t.FileSizeBytes)
                .GreaterThan(0).WithMessage("File size must be greater than 0 bytes.")
                .LessThanOrEqualTo(FileMetadataValidator.DefaultMaxAudioSizeBytes)
                .WithMessage($"File exceeds the maximum allowed size of {FileMetadataValidator.DefaultMaxAudioSizeBytes / (1024 * 1024)} MB.");

            track.RuleFor(t => t.ContentType)
                .MaximumLength(100).WithMessage("Content type cannot exceed 100 characters.")
                .Must(BeAudioContentType).WithMessage("Content type must be an audio MIME type (audio/*).")
                .When(t => !string.IsNullOrEmpty(t.ContentType));

            track.RuleFor(t => t.GenreIds)
                .Must(g => g!.Count <= MaxGenresPerTrack)
                .WithMessage($"A track cannot have more than {MaxGenresPerTrack} genres.")
                .When(t => t.GenreIds is not null);

            track.RuleFor(t => t.FeaturedArtistIds)
                .Must(a => a!.Count <= MaxFeaturedArtistsPerTrack)
                .WithMessage($"A track cannot have more than {MaxFeaturedArtistsPerTrack} featured artists.")
                .When(t => t.FeaturedArtistIds is not null);
        });
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var ext = StorageKeyGenerator.NormalizeExtension(Path.GetExtension(fileName));
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    private static bool BeAudioContentType(string? contentType)
        => contentType is not null && contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
}
