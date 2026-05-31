using FluentAssertions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Identity.Common;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.Repository;
using SoundWave.Identity.Events.Notifications.UserRegistered;
using SoundWave.Identity.Features.Register;
using SoundWave.Identity.Helpers;
using SoundWave.SharedKernel.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

public class RegisterTests : IdentityIntegrationTestBase
{
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ITokenHelper> _tokenHelperMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock = new();
    private readonly RegisterRequestValidator _validator = new();

    public RegisterTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IdentityModule).Assembly);
    }

    private RegisterCommandHandler BuildHandler()
    {
        var userRepository = new IdentityRepository<User>(DbContext);
        var userProfileRepository = new IdentityRepository<UserProfile>(DbContext);
        return new RegisterCommandHandler(
            userRepository,
            userProfileRepository,
            _cachingServiceMock.Object,
            _tokenHelperMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmailAlreadyExists_WhenEmailIsTaken()
    {
        await SeedAsync(new User
        {
            Id = Guid.CreateVersion7(),
            Email = "taken@example.com",
            PasswordHash = "hash",
            Role = UserRole.Listener
        });

        var command = new RegisterCommand(
            Email: "taken@example.com",
            Password: "SecurePassword123!",
            FirstName: "John",
            LastName: "Doe",
            DisplayName: "johndoe",
            DateOfBirth: new DateOnly(1990, 1, 1),
            Gender: Common.Gender.Male,
            CountryId: 1
        );

        var handler = BuildHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyExists);
    }

    [Fact]
    public async Task Handle_ShouldRegisterSuccessfully_WhenRequestIsValid()
    {
        var command = new RegisterCommand(
            Email: "unique@example.com",
            Password: "SecurePassword123!",
            FirstName: "Jane",
            LastName: "Doe",
            DisplayName: "janedoe",
            DateOfBirth: new DateOnly(1992, 5, 15),
            Gender: Common.Gender.Female,
            CountryId: 2
        );

        _tokenHelperMock
            .Setup(t => t.GenerateOTP(It.IsAny<int>()))
            .Returns("123456");

        var handler = BuildHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var savedUser = await DbContext.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Email == command.Email);
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Listener);
        BCrypt.Net.BCrypt.Verify(command.Password, savedUser.PasswordHash).Should().BeTrue();

        savedUser.UserProfile.Should().NotBeNull();
        savedUser.UserProfile!.FirstName.Should().Be(command.FirstName);
        savedUser.UserProfile.LastName.Should().Be(command.LastName);
        savedUser.UserProfile.DisplayName.Should().Be(command.DisplayName);

        _cachingServiceMock.Verify(
            c => c.AddAsync(
                It.Is<string>(k => k == Constants.Caching.UserEmailVerification + savedUser.Id.ToString()),
                "123456",
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisherMock.Verify(
            p => p.Publish(
                It.Is<UserRegisteredNotification>(n => n.UserId == savedUser.Id && n.Email == command.Email && n.FullName == command.DisplayName && n.Otp == "123456"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksUppercase()
    {
        var request = new RegisterRequest("test@example.com", "nocaps123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksLowercase()
    {
        var request = new RegisterRequest("test@example.com", "ALLCAPS123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksDigit()
    {
        var request = new RegisterRequest("test@example.com", "NoDigitsHere!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("digit"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksSpecialCharacter()
    {
        var request = new RegisterRequest("test@example.com", "NoSpecial123", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("special character"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenPasswordIsValid()
    {
        var request = new RegisterRequest("test@example.com", "ValidPass123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
