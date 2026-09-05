using MediatR;
using SoundWave.Identity.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Features.Account.UpdateProfileImages;

/// <summary>
/// Command to update a user's profile and cover image URLs.
/// </summary>
/// <param name="UserId">The identifier of the user to update.</param>
/// <param name="ProfilePicUrl">The new profile picture URL (or placeholder string).</param>
/// <param name="CoverImageUrl">The new cover image URL (or placeholder string).</param>
internal record UpdateProfileImagesCommand(
    Guid UserId,
    string? ProfilePicUrl,
    string? CoverImageUrl) : IRequest<Result<IdentityError, Unit>>;
