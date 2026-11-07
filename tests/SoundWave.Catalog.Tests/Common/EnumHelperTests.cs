using FluentAssertions;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;

namespace SoundWave.Catalog.Tests.Common;

public class EnumHelperTests
{
    private enum ByteEnum : byte
    {
        First = 0,
        Second = 1,
        Max = 255
    }

    private enum SByteEnum : sbyte
    {
        Negative = -1,
        Zero = 0,
        Positive = 127
    }

    private enum ShortEnum : short
    {
        Low = -32768,
        Mid = 0,
        High = 32767
    }

    private enum IntEnum : int
    {
        A = 0,
        B = 10,
        C = 20
    }

    private enum LongEnum : long
    {
        Min = -9223372036854775808L,
        Zero = 0L,
        Max = 9223372036854775807L
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleByteEnum_IncludingGenreType()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<GenreType>();

        // Assert
        result.Should().Be("Genre (0), Mood (1)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleAlbumType_Correctly()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<AlbumType>();

        // Assert
        result.Should().Be("Album (0), EP (1), Single (2)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleCustomByteEnum()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<ByteEnum>();

        // Assert
        result.Should().Be("First (0), Second (1), Max (255)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleSByteEnum()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<SByteEnum>();

        // Assert
        result.Should().Be("Zero (0), Positive (127), Negative (-1)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleShortEnum()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<ShortEnum>();

        // Assert
        result.Should().Be("Mid (0), High (32767), Low (-32768)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleIntEnum()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<IntEnum>();

        // Assert
        result.Should().Be("A (0), B (10), C (20)");
    }

    [Fact]
    public void ToAllowedValuesString_ShouldHandleLongEnum()
    {
        // Act
        var result = EnumHelper.ToAllowedValuesString<LongEnum>();

        // Assert
        result.Should().Be("Zero (0), Max (9223372036854775807), Min (-9223372036854775808)");
    }

    [Fact]
    public void FormatAllowedValues_ShouldJoinValuesWithCommaSeparator()
    {
        // Act
        var result = EnumHelper.FormatAllowedValues("Name", "Type", "CreatedAt");

        // Assert
        result.Should().Be("Name, Type, CreatedAt");
    }
}
