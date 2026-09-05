using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.Account.UpdateProfileImages;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Tests.Features.Account;

public class UpdateProfileImagesTests : IdentityIntegrationTestBase
{
    private readonly Mock<ILogger<UpdateProfileImagesCommandHandler>> _loggerMock = new();

    private UpdateProfileImagesCommandHandler BuildHandler()
    {
        var repo = new IdentityRepository<UserProfile>(DbContext);
        return new UpdateProfileImagesCommandHandler(repo, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldReturnUserNotFound()
    {
        var handler = BuildHandler();
        var command = new UpdateProfileImagesCommand(Guid.NewGuid(), "http://img.jpg", "http://cover.jpg");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ShouldUpdateProfileAndCoverImages()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "profileuser@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        };
        await SeedAsync(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "johndoe",
            ProfilePicUrl = "http://old-pic.jpg",
            CoverImageUrl = "http://old-cover.jpg",
            Gender = Gender.Male
        };
        await SeedAsync(profile);

        var handler = BuildHandler();
        var command = new UpdateProfileImagesCommand(userId, "http://new-pic.jpg", "http://new-cover.jpg");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await DbContext.UserProfiles.FindAsync(profile.Id);
        updated.Should().NotBeNull();
        updated!.ProfilePicUrl.Should().Be("http://new-pic.jpg");
        updated.CoverImageUrl.Should().Be("http://new-cover.jpg");
    }

    [Fact]
    public void Validator_WhenUrlsExceedMaxLength_ShouldHaveValidationErrors()
    {
        var validator = new UpdateProfileImagesRequestValidator();
        var tooLongUrl = new string('a', 501);
        var request = new UpdateProfileImagesRequest(tooLongUrl, tooLongUrl);

        var validationResult = validator.Validate(request);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileImagesRequest.ProfilePicUrl));
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileImagesRequest.CoverImageUrl));
    }
}
