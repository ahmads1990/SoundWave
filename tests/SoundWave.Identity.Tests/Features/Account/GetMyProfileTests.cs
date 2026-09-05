using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Features.Account.GetMyProfile;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Identity.Tests.Features.Account;

public class GetMyProfileTests : IdentityIntegrationTestBase
{
    private readonly Mock<ILogger<GetMyProfileQueryHandler>> _loggerMock = new();

    private GetMyProfileQueryHandler BuildHandler()
    {
        var repo = new IdentityRepository<UserProfile>(DbContext);
        return new GetMyProfileQueryHandler(repo, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldReturnUserNotFound()
    {
        var handler = BuildHandler();
        var query = new GetMyProfileQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.UserNotFound);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ShouldReturnUserProfileDto()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "getmyprofile@example.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = UserRole.Listener
        };
        await SeedAsync(user);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Alice",
            LastName = "Smith",
            DisplayName = "alicesmith",
            ProfilePicUrl = "https://example.com/alice.png",
            CoverImageUrl = "https://example.com/banner.png",
            PhoneNumber = "+123456789",
            DateOfBirth = new DateOnly(1995, 5, 20),
            Language = "en",
            Gender = Gender.Female
        };
        await SeedAsync(profile);

        var handler = BuildHandler();
        var query = new GetMyProfileQuery(userId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.Email.Should().Be("getmyprofile@example.com");
        result.Data.FirstName.Should().Be("Alice");
        result.Data.LastName.Should().Be("Smith");
        result.Data.DisplayName.Should().Be("alicesmith");
        result.Data.ProfilePicUrl.Should().Be("https://example.com/alice.png");
        result.Data.CoverImageUrl.Should().Be("https://example.com/banner.png");
        result.Data.PhoneNumber.Should().Be("+123456789");
        result.Data.DateOfBirth.Should().Be(new DateOnly(1995, 5, 20));
        result.Data.Language.Should().Be("en");
        result.Data.Gender.Should().Be("Female");
    }
}
