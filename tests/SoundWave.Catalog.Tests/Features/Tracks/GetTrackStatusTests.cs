using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SoundWave.Catalog.Common;
using SoundWave.SharedKernel.Common;
using SoundWave.Catalog.Data.Entities;
using SoundWave.Catalog.Features.Tracks.GetTrackStatus;

namespace SoundWave.Catalog.Tests.Features.Tracks;

public class GetTrackStatusTests : CatalogIntegrationTestBase
{
    private readonly Mock<ILogger<GetTrackStatusQueryHandler>> _loggerMock = new();

    private GetTrackStatusQueryHandler BuildHandler()
    {
        return new GetTrackStatusQueryHandler(
            CreateReadDbContext(),
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrackNotFound_WhenTrackDoesNotExist()
    {
        var query = new GetTrackStatusQuery(Guid.NewGuid());

        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(CatalogError.TrackNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnStatusAndMetadata_WhenTrackExistsWithTrackFile()
    {
        var album = new Album { Title = "Status Album", AlbumType = AlbumType.Album };
        await SeedAsync(album);

        var startedAt = DateTime.UtcNow.AddMinutes(-5);
        var track = new Track
        {
            AlbumId = album.Id,
            Title = "Processed Track",
            DurationSeconds = 180,
            TrackNumber = 1,
            TrackFile = new TrackFile
            {
                Status = FileStatus.Available,
                HlsPlaylistPath = "audio/hls/master.m3u8",
                PreviewPlaylistPath = "audio/hls/preview.m3u8",
                ProcessingStartedAt = startedAt
            }
        };
        await SeedAsync(track);

        var query = new GetTrackStatusQuery(track.Id);

        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TrackId.Should().Be(track.Id);
        result.Data.Status.Should().Be(FileStatus.Available);
        result.Data.HlsPlaylistPath.Should().Be("audio/hls/master.m3u8");
        result.Data.PreviewPlaylistPath.Should().Be("audio/hls/preview.m3u8");
        result.Data.ProcessingStartedAt.Should().BeCloseTo(startedAt, TimeSpan.FromSeconds(1));
        result.Data.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedStatusAndReason_WhenProcessingFailed()
    {
        var album = new Album { Title = "Failed Album", AlbumType = AlbumType.Album };
        await SeedAsync(album);

        var track = new Track
        {
            AlbumId = album.Id,
            Title = "Failed Track",
            DurationSeconds = 180,
            TrackNumber = 1,
            TrackFile = new TrackFile
            {
                Status = FileStatus.Failed,
                FailureReason = "FFmpeg exit code 1: Invalid audio stream header"
            }
        };
        await SeedAsync(track);

        var query = new GetTrackStatusQuery(track.Id);

        var result = await BuildHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(FileStatus.Failed);
        result.Data.FailureReason.Should().Be("FFmpeg exit code 1: Invalid audio stream header");
    }

    [Fact]
    public async Task Handle_ShouldReturnNullStatus_WhenNoAudioUploadedYet()
    {
        var album = new Album { Title = "Draft Album", AlbumType = AlbumType.Album };
        await SeedAsync(album);

        var track = new Track { AlbumId = album.Id, Title = "No Audio Track", TrackNumber = 1 };
        await SeedAsync(track);

        var result = await BuildHandler().Handle(new GetTrackStatusQuery(track.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().BeNull();
        result.Data.HlsPlaylistPath.Should().BeNull();
    }
}
