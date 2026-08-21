using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.UpdateGenre;

namespace SoundWave.Catalog.Tests.Features;

public class UpdateGenreTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<UpdateGenreCommandHandler>> _loggerMock = new();
    private readonly UpdateGenreRequestValidator _validator = new();

    private UpdateGenreCommandHandler BuildHandler()
    {
        return new UpdateGenreCommandHandler(DbContext, _loggerMock.Object);
    }

    #region Handler Tests

    [Fact]
    public async Task Handle_ShouldReturnGenreNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var command = new UpdateGenreCommand(999, "Synthwave", GenreType.Mood);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.GenreNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnGenreAlreadyExists_WhenNameAndTypeCollideWithAnotherGenre()
    {
        // Arrange
        var genre1 = new Genre { Name = "Rock", Type = GenreType.Genre };
        var genre2 = new Genre { Name = "Jazz", Type = GenreType.Genre };
        await SeedAsync(genre1, genre2);

        // Try updating genre2's name to "rock" (same name & type as genre1)
        var command = new UpdateGenreCommand(genre2.Id, "rock", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.GenreAlreadyExists);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUpdatingSelfWithSameNameAndType()
    {
        // Arrange
        var genre = new Genre { Name = "Indie", Type = GenreType.Genre };
        await SeedAsync(genre);

        var command = new UpdateGenreCommand(genre.Id, "Indie", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(genre.Id);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUpdatingWithNewUniqueValues()
    {
        // Arrange
        var genre = new Genre { Name = "Old Name", Type = GenreType.Genre };
        await SeedAsync(genre);

        var command = new UpdateGenreCommand(genre.Id, "Progressive Rock", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(genre.Id);

        var updated = await DbContext.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Progressive Rock");
        updated.Type.Should().Be(GenreType.Genre);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldFail_WhenNameIsEmpty()
    {
        var request = new UpdateGenreRequest("", GenreType.Genre);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenNameExceeds50Characters()
    {
        var longName = new string('A', 51);
        var request = new UpdateGenreRequest(longName, GenreType.Genre);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("exceed 50"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTypeIsInvalid()
    {
        var request = new UpdateGenreRequest("Classical", (GenreType)99);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type" && e.ErrorMessage.Contains("genre type"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new UpdateGenreRequest("Chill", GenreType.Mood);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
