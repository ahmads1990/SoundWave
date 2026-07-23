using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.CreateGenre;

namespace SoundWave.Catalog.Tests.Features;

public class CreateGenreTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<CreateGenreCommandHandler>> _loggerMock = new();
    private readonly CreateGenreRequestValidator _validator = new();

    private CreateGenreCommandHandler BuildHandler()
    {
        return new CreateGenreCommandHandler(DbContext, _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnGenreAlreadyExists_WhenNameAndTypeAlreadyExist()
    {
        // Arrange
        await SeedAsync(new Genre
        {
            Name = "Rock",
            Type = GenreType.Genre
        });

        var command = new CreateGenreCommand("rock", GenreType.Genre); // Case-insensitive collision check
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.GenreAlreadyExists);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenGenreIsUnique()
    {
        // Arrange
        var command = new CreateGenreCommand("Pop", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);

        var savedGenre = await DbContext.Genres.FirstOrDefaultAsync(g => g.Id == result.Data);
        savedGenre.Should().NotBeNull();
        savedGenre!.Name.Should().Be("Pop");
        savedGenre.Type.Should().Be(GenreType.Genre);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenNameIsEmpty()
    {
        var request = new CreateGenreRequest("", GenreType.Genre);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenNameExceeds50Characters()
    {
        var longName = new string('A', 51);
        var request = new CreateGenreRequest(longName, GenreType.Genre);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("exceed 50"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTypeIsInvalid()
    {
        var request = new CreateGenreRequest("Metal", (GenreType)99);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type" && e.ErrorMessage.Contains("genre type"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreateGenreRequest("Lo-Fi", GenreType.Mood);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
