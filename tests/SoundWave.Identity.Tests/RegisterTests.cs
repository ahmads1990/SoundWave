using FluentAssertions;
using Mapster;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using SoundWave.Identity.Data.Entites;
using SoundWave.Identity.Data.IRepository;
using SoundWave.Identity.Events.Notifications.UserRegistered;
using SoundWave.Identity.Features.Register;
using SoundWave.SharedKernel.Models.Responses;
using SoundWave.SharedKernel.Interfaces;
using SoundWave.Identity.Helpers;
using SoundWave.Identity.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoundWave.Identity.Tests;

/// <summary>
/// Contains unit tests for the <see cref="RegisterCommandHandler"/> class and its validator.
/// </summary>
public class RegisterTests
{
    #region Fields

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IIdentityRepository<UserProfile>> _userProfileRepositoryMock = new();
    private readonly Mock<ICachingService> _cachingServiceMock = new();
    private readonly Mock<ITokenHelper> _tokenHelperMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock = new();
    private readonly RegisterCommandHandler _handler;
    private readonly RegisterRequestValidator _validator = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTests"/> class.
    /// Sets up the mocks, configures Mapster, and initializes the handler under test.
    /// </summary>
    public RegisterTests()
    {
        // Scan mappings for Mapster
        TypeAdapterConfig.GlobalSettings.Scan(typeof(IdentityModule).Assembly);

        _handler = new RegisterCommandHandler(
            _userRepositoryMock.Object,
            _userProfileRepositoryMock.Object,
            _cachingServiceMock.Object,
            _tokenHelperMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    #endregion

    #region Unit Tests - Handler

    /// <summary>
    /// Verifies that registration fails with EmailAlreadyExists when the email is already registered.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnEmailAlreadyExists_WhenEmailIsTaken()
    {
        // Arrange
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

        _userRepositoryMock
            .Setup(r => r.CheckIfEmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(IdentityError.EmailAlreadyExists);

        _userRepositoryMock.Verify(r => r.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _userProfileRepositoryMock.Verify(r => r.Add(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<UserRegisteredNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that registration succeeds when a valid request is provided and the email is unique.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldRegisterSuccessfully_WhenRequestIsValid()
    {
        // Arrange
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

        _userRepositoryMock
            .Setup(r => r.CheckIfEmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        User? savedUser = null;
        _userRepositoryMock
            .Setup(r => r.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => savedUser = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        UserProfile? savedProfile = null;
        _userProfileRepositoryMock
            .Setup(r => r.Add(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((p, _) => savedProfile = p)
            .ReturnsAsync((UserProfile p, CancellationToken _) => p);

        _tokenHelperMock
            .Setup(t => t.GenerateOTP(It.IsAny<int>()))
            .Returns("123456");

        _cachingServiceMock
            .Setup(c => c.AddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(command.Email);
        savedUser.Role.Should().Be(Common.UserRole.Listener);
        BCrypt.Net.BCrypt.Verify(command.Password, savedUser.PasswordHash).Should().BeTrue();

        savedProfile.Should().NotBeNull();
        savedProfile!.UserId.Should().Be(savedUser.Id);
        savedProfile.FirstName.Should().Be(command.FirstName);
        savedProfile.LastName.Should().Be(command.LastName);
        savedProfile.DisplayName.Should().Be(command.DisplayName);

        _userRepositoryMock.Verify(r => r.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);

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

    #endregion

    #region Unit Tests - Validator

    /// <summary>
    /// Verifies that password validation fails when the password does not contain an uppercase letter.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksUppercase()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "nocaps123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("uppercase"));
    }

    /// <summary>
    /// Verifies that password validation fails when the password does not contain a lowercase letter.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksLowercase()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "ALLCAPS123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("lowercase"));
    }

    /// <summary>
    /// Verifies that password validation fails when the password does not contain a digit.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksDigit()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "NoDigitsHere!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("digit"));
    }

    /// <summary>
    /// Verifies that password validation fails when the password does not contain a special character.
    /// </summary>
    [Fact]
    public void Validator_ShouldFail_WhenPasswordLacksSpecialCharacter()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "NoSpecial123", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("special character"));
    }

    /// <summary>
    /// Verifies that password validation succeeds when the password meets all complexity rules.
    /// </summary>
    [Fact]
    public void Validator_ShouldSucceed_WhenPasswordIsValid()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "ValidPass123!", "John", "Doe", "johndoe", new DateOnly(1990, 1, 1), Common.Gender.Male, 1);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
