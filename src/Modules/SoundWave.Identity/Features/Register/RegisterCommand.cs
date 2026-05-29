using Mapster;
using MediatR;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.SharedKernel.Models.Responses;

namespace SoundWave.Identity.Features.Register;

/// <summary>
/// Command for registering a new user within the system.
/// </summary>
/// <param name="Email">The unique email address of the user.</param>
/// <param name="Password">The plain-text password chosen by the user.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="DisplayName">The user's preferred display name.</param>
/// <param name="DateOfBirth">The user's date of birth, if provided.</param>
/// <param name="Gender">The user's gender identification.</param>
/// <param name="CountryId">The identifier for the user's country of residence.</param>
internal record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string DisplayName,
    DateOnly? DateOfBirth,
    Gender Gender,
    int CountryId) : IRequest<IdentityResult<Guid>>;

/// <summary>
/// Defines the mapping rules between <see cref="RegisterCommand"/> and domain entities using Mapster.
/// </summary>
internal class RegisterCommandMappingConfig : IRegister
{
    /// <summary>
    /// Configures the specific mapping logic for registration entities.
    /// </summary>
    /// <param name="config">The global Mapster configuration instance.</param>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterCommand, User>()
            .Map(dest => dest.Role, _ => UserRole.Listener)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PasswordHash);

        config.NewConfig<RegisterCommand, UserProfile>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId);
    }
}
