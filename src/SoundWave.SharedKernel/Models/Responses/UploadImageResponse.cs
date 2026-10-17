namespace SoundWave.SharedKernel.Models.Responses;

/// <summary>
/// Standard response payload returned after a successful image upload.
/// </summary>
/// <param name="FileId">The unique FileMetadata identifier.</param>
/// <param name="StorageKey">The canonical storage key stored in the database.</param>
/// <param name="Url">The public access URL of the uploaded image.</param>
/// <param name="SizeBytes">The verified payload size in bytes.</param>
public record UploadImageResponse(
    Guid FileId,
    string StorageKey,
    string Url,
    long SizeBytes);
