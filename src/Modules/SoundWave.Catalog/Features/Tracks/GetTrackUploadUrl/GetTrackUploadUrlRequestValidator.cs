using FluentValidation;
using SoundWave.SharedKernel.Storage;
using SoundWave.SharedKernel.Validation;

namespace SoundWave.Catalog.Features.Tracks.GetTrackUploadUrl;

/// <summary>
/// Validator for <see cref="GetTrackUploadUrlRequest"/>.
/// </summary>
internal class GetTrackUploadUrlRequestValidator : AbstractValidator<GetTrackUploadUrlRequest>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".aac", ".m4a"
    };

    public GetTrackUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.")
            .Must(HaveAllowedExtension).WithMessage("Only .mp3, .flac, .wav, .aac, and .m4a audio formats are allowed.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("File size must be greater than 0 bytes.")
            .LessThanOrEqualTo(FileMetadataValidator.DefaultMaxAudioSizeBytes)
            .WithMessage($"File exceeds the maximum allowed size of {FileMetadataValidator.DefaultMaxAudioSizeBytes / (1024 * 1024)} MB.");
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        var ext = StorageKeyGenerator.NormalizeExtension(Path.GetExtension(fileName));
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }
}
