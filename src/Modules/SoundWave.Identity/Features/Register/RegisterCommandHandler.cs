using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SoundWave.Identity.Data;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Handles the registration of a new user within the identity module.
/// </summary>
/// <param name="dbContext">The database context for identity data.</param>
internal class RegisterCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<RegisterCommand, BaseApiResponse<Guid>>
{
    /// <summary>
    /// Handles the registration process, including email validation, user creation, and profile setup.
    /// </summary>
    /// <param name="request">The registration command details.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A base API response containing the new user's unique identifier if successful.</returns>
    public async Task<BaseApiResponse<Guid>> Handle(RegisterCommand request, CancellationToken ct = default)
    {
        if (await CheckIfEmailExistsAsync(request.Email, ct))
            return new FailureResponse<Guid>(ApiErrorCode.EmailAlreadyExists);

        var userId = Guid.CreateVersion7();
        var user = CreateUser(request, userId);
        var profile = CreateUserProfile(request, userId);

        dbContext.Users.Add(user);
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(ct);

        return new SuccessResponse<Guid>(userId);
    }

    #region Helper Methods

    /// <summary>
    /// Checks the database for any existing user with the provided email address.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="ct">Cancellation token for the database query.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean result indicating existence.</returns>
    private async Task<bool> CheckIfEmailExistsAsync(string email, CancellationToken ct)
    {
        return await dbContext.Users.AnyAsync(u => u.Email == email, ct);
    }

    /// <summary>
    /// Maps the registration command to a new User entity and hashes the password.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <param name="userId">The pre-generated unique identifier for the user.</param>
    /// <returns>A populated User entity.</returns>
    private static User CreateUser(RegisterCommand request, Guid userId)
    {
        var user = request.Adapt<User>();
        user.Id = userId;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        return user;
    }

    /// <summary>
    /// Maps the registration command to a new UserProfile entity.
    /// </summary>
    /// <param name="request">The source registration command.</param>
    /// <param name="userId">The unique identifier of the associated user.</param>
    /// <returns>A populated UserProfile entity.</returns>
    private static UserProfile CreateUserProfile(RegisterCommand request, Guid userId)
    {
        var profile = request.Adapt<UserProfile>();
        profile.Id = Guid.CreateVersion7();
        profile.UserId = userId;
        return profile;
    }

    #endregion
}
