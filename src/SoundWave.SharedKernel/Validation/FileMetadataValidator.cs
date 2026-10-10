using MimeDetective;
using SoundWave.SharedKernel.Common;

namespace SoundWave.SharedKernel.Validation;

/// <summary>
/// Encapsulates verified metadata for an uploaded file after stream and signature inspection.
/// </summary>
/// <param name="ContentType">The verified MIME content type (e.g. "image/webp", "image/png").</param>
/// <param name="Extension">The verified normalized file extension with leading period (e.g. ".webp", ".png").</param>
/// <param name="SizeBytes">The exact payload size in bytes.</param>
/// <param name="Format">The matched strongly-typed media format.</param>
public record ValidatedFileMetadata(
    string ContentType,
    string Extension,
    long SizeBytes,
    SupportedMediaFormat Format);

/// <summary>
/// Unified file metadata and signature validation engine.
/// Evaluates stream properties, size limits, and inspects binary headers via Mime-Detective
/// mapped against caller-specified <see cref="SupportedMediaFormat"/> flags.
/// </summary>
public class FileMetadataValidator
{
    private readonly IContentInspector _inspector;

    public const long DefaultMaxImageSizeBytes = 5 * 1024 * 1024;  // 5 MB
    public const long DefaultMaxAudioSizeBytes = 50 * 1024 * 1024; // 50 MB

    public FileMetadataValidator(IContentInspector inspector)
    {
        _inspector = inspector;
    }

    /// <summary>
    /// Validates an input stream against caller-specified <see cref="SupportedMediaFormat"/> flags and size constraints.
    /// </summary>
    public Result<SharedKernelError, ValidatedFileMetadata> Validate(
        Stream stream,
        string fileName,
        long maxSizeBytes,
        SupportedMediaFormat allowedFormats)
    {
        var streamValidation = ValidateStream(stream, maxSizeBytes);
        if (!streamValidation.IsSuccess)
            return Result<SharedKernelError, ValidatedFileMetadata>.Failure(streamValidation.Error, streamValidation.ErrorMessage);

        var extensionValidation = ValidateClientExtension(fileName, allowedFormats);
        if (!extensionValidation.IsSuccess)
            return Result<SharedKernelError, ValidatedFileMetadata>.Failure(extensionValidation.Error, extensionValidation.ErrorMessage);

        var inspectionValidation = InspectBinaryContent(stream, allowedFormats);
        if (!inspectionValidation.IsSuccess)
            return Result<SharedKernelError, ValidatedFileMetadata>.Failure(inspectionValidation.Error, inspectionValidation.ErrorMessage);

        var (detectedMime, detectedExt, matchedFormat) = inspectionValidation.Data;
        return Result<SharedKernelError, ValidatedFileMetadata>.Success(
            new ValidatedFileMetadata(detectedMime, detectedExt, stream.Length, matchedFormat));
    }

    /// <summary>
    /// Convenience helper for image files (defaulting to Jpeg, Png, Webp with 5 MB limit).
    /// </summary>
    public Result<SharedKernelError, ValidatedFileMetadata> ValidateImage(
        Stream stream,
        string fileName,
        long maxSizeBytes = DefaultMaxImageSizeBytes,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllImages)
    {
        return Validate(stream, fileName, maxSizeBytes, allowedFormats);
    }

    /// <summary>
    /// Convenience helper for audio files (defaulting to Mp3, Flac, Wav, Aac with 50 MB limit).
    /// </summary>
    public Result<SharedKernelError, ValidatedFileMetadata> ValidateAudio(
        Stream stream,
        string fileName,
        long maxSizeBytes = DefaultMaxAudioSizeBytes,
        SupportedMediaFormat allowedFormats = SupportedMediaFormat.AllAudio)
    {
        return Validate(stream, fileName, maxSizeBytes, allowedFormats);
    }

    #region Private Validation Helpers

    private static Result<SharedKernelError, bool> ValidateStream(Stream? stream, long maxSizeBytes)
    {
        if (stream == null || stream.Length == 0)
            return Result<SharedKernelError, bool>.Failure(SharedKernelError.EmptyFile, "The uploaded file is empty.");

        if (stream.Length > maxSizeBytes)
        {
            var maxMb = maxSizeBytes / (1024 * 1024);
            return Result<SharedKernelError, bool>.Failure(SharedKernelError.FileExceedsMaxSize, $"File exceeds the maximum allowed size of {maxMb} MB.");
        }

        return Result<SharedKernelError, bool>.Success(true);
    }

    private static Result<SharedKernelError, string> ValidateClientExtension(string fileName, SupportedMediaFormat allowedFormats)
    {
        var clientExtension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(clientExtension))
            return Result<SharedKernelError, string>.Failure(SharedKernelError.UnsupportedFileExtension, "File must have a valid extension.");

        var normalizedClientExt = clientExtension.Trim().ToLowerInvariant();
        if (!IsExtensionAllowedForFormats(normalizedClientExt, allowedFormats))
            return Result<SharedKernelError, string>.Failure(SharedKernelError.UnsupportedFileExtension, $"The extension '{normalizedClientExt}' is not allowed for the requested operation.");

        return Result<SharedKernelError, string>.Success(normalizedClientExt);
    }

    private Result<SharedKernelError, (string Mime, string Extension, SupportedMediaFormat Format)> InspectBinaryContent(
        Stream stream,
        SupportedMediaFormat allowedFormats)
    {
        var originalPosition = stream.CanSeek ? stream.Position : 0;
        if (stream.CanSeek)
            stream.Position = 0;

        var results = _inspector.Inspect(stream);

        if (stream.CanSeek)
            stream.Position = originalPosition;

        var mimeMatch = results.ByMimeType().FirstOrDefault();
        var extMatch = results.ByFileExtension().FirstOrDefault();

        if (mimeMatch == null || string.IsNullOrWhiteSpace(mimeMatch.MimeType) ||
            extMatch == null || string.IsNullOrWhiteSpace(extMatch.Extension))
        {
            return Result<SharedKernelError, (string, string, SupportedMediaFormat)>.Failure(
                SharedKernelError.InvalidFileHeader, "File content does not match a valid binary signature for the allowed formats.");
        }

        var detectedMime = mimeMatch.MimeType.Trim().ToLowerInvariant();
        var matchedFormat = ResolveFormatFromMime(detectedMime);

        if (matchedFormat == SupportedMediaFormat.None || !allowedFormats.HasFlag(matchedFormat))
        {
            return Result<SharedKernelError, (string, string, SupportedMediaFormat)>.Failure(
                SharedKernelError.InvalidFileHeader, $"Detected MIME type '{detectedMime}' is not permitted for the allowed formats.");
        }

        var normalizedDetectedExt = extMatch.Extension.StartsWith('.') 
            ? extMatch.Extension.ToLowerInvariant() 
            : $".{extMatch.Extension.ToLowerInvariant()}";

        return Result<SharedKernelError, (string, string, SupportedMediaFormat)>.Success(
            (detectedMime, normalizedDetectedExt, matchedFormat));
    }

    private static bool IsExtensionAllowedForFormats(string extension, SupportedMediaFormat formats)
    {
        if (formats.HasFlag(SupportedMediaFormat.Jpeg) && (extension == ".jpg" || extension == ".jpeg"))
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Png) && extension == ".png")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Webp) && extension == ".webp")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Mp3) && extension == ".mp3")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Flac) && extension == ".flac")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Wav) && extension == ".wav")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Aac) && (extension == ".aac" || extension == ".m4a"))
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Mp4) && extension == ".mp4")
            return true;
        if (formats.HasFlag(SupportedMediaFormat.Webm) && extension == ".webm")
            return true;

        return false;
    }

    private static SupportedMediaFormat ResolveFormatFromMime(string mimeType) => mimeType switch
    {
        "image/jpeg" or "image/jpg" => SupportedMediaFormat.Jpeg,
        "image/png" => SupportedMediaFormat.Png,
        "image/webp" => SupportedMediaFormat.Webp,
        "audio/mpeg" or "audio/mp3" => SupportedMediaFormat.Mp3,
        "audio/flac" or "audio/x-flac" => SupportedMediaFormat.Flac,
        "audio/wav" or "audio/x-wav" or "audio/wave" => SupportedMediaFormat.Wav,
        "audio/aac" or "audio/x-aac" or "audio/mp4" or "audio/m4a" => SupportedMediaFormat.Aac,
        "video/mp4" => SupportedMediaFormat.Mp4,
        "video/webm" => SupportedMediaFormat.Webm,
        _ => SupportedMediaFormat.None
    };

    #endregion
}
