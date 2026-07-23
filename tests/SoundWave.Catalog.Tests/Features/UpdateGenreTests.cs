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
        var command = new UpdateGenreCommand(999, "Jazz", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.GenreNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnGenreAlreadyExists_WhenNameConflictsWithAnotherRecord()
    {
        // Arrange
        var genre1 = new Genre { Name = "Electronic", Type = GenreType.Genre };
        var genre2 = new Genre { Name = "Classical", Type = GenreType.Genre };
        await SeedAsync(genre1, genre2);

        // Try to update genre2 to have the name "electronic" (case-insensitive clash)
        var command = new UpdateGenreCommand(genre2.Id, "electronic", GenreType.Genre);
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
        var genre = new Genre { Name = "Chill", Type = GenreType.Mood };
        await SeedAsync(genre);

        var command = new UpdateGenreCommand(genre.Id, "Chill", GenreType.Mood);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(genre.Id);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUpdatingToUniqueValues()
    {
        // Arrange
        var genre = new Genre { Name = "Rap", Type = GenreType.Genre };
        await SeedAsync(genre);

        var command = new UpdateGenreCommand(genre.Id, "Hip Hop", GenreType.Genre);
        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(genre.Id);

        var updatedGenre = await DbContext.Genres.FirstOrDefaultAsync(g => g.Id == genre.Id);
        updatedGenre.Should().NotBeNull();
        updatedGenre!.Name.Should().Be("Hip Hop");
        updatedGenre.Type.Should().Be(GenreType.Genre);
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
        var longName = new string('B', 51);
        var request = new UpdateGenreRequest(longName, GenreType.Genre);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("exceed 50"));
    }

    [Fact]
    public void Validator_ShouldFail_WhenTypeIsInvalid()
    {
        var request = new UpdateGenreRequest("Synthwave", (GenreType)88);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type" && e.ErrorMessage.Contains("genre type"));
    }

    [Fact]
    public void Validator_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new UpdateGenreRequest("Workout", GenreType.Mood);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
