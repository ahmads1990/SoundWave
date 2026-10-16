using SoundWave.SharedKernel.Storage;

namespace SoundWave.Identity.Tests.Services;

public class StorageKeyGeneratorTests
{
    [Fact]
    public void ProfilePicture_ShouldBeUnderPublicNamespace()
    {
        var fileId = Guid.NewGuid();
        var key = StorageKeyGenerator.ProfilePicture(fileId);

        Assert.Equal($"public/images/avatars/{fileId:N}", key);
        Assert.StartsWith("public/", key);
    }

    [Fact]
    public void ProfileBanner_ShouldBeUnderPublicNamespace()
    {
        var fileId = Guid.NewGuid();
        var key = StorageKeyGenerator.ProfileBanner(fileId);

        Assert.Equal($"public/images/banners/profiles/{fileId:N}", key);
        Assert.StartsWith("public/", key);
    }

    [Fact]
    public void AlbumCover_ShouldBeUnderPublicNamespace()
    {
        var fileId = Guid.NewGuid();
        var key = StorageKeyGenerator.AlbumCover(fileId);

        Assert.Equal($"public/images/covers/albums/{fileId:N}", key);
        Assert.StartsWith("public/", key);
    }

    [Fact]
    public void PlaylistCover_ShouldBeUnderPublicNamespace()
    {
        var fileId = Guid.NewGuid();
        var key = StorageKeyGenerator.PlaylistCover(fileId);

        Assert.Equal($"public/images/covers/playlists/{fileId:N}", key);
        Assert.StartsWith("public/", key);
    }

    [Fact]
    public void RawAudio_ShouldBePrivate()
    {
        var trackId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var key = StorageKeyGenerator.RawAudio(trackId, fileId, "flac");

        Assert.Equal($"audio/raw/{trackId:N}/{fileId:N}.flac", key);
        Assert.False(key.StartsWith("public/"));
    }

    [Fact]
    public void HlsPlaylist_ShouldFormatCorrectly()
    {
        var trackId = Guid.NewGuid();
        var key = StorageKeyGenerator.HlsPlaylist(trackId);

        Assert.Equal($"audio/hls/{trackId:N}/playlist.m3u8", key);
    }

    [Fact]
    public void HlsSegment_ShouldFormatCorrectly()
    {
        var trackId = Guid.NewGuid();
        var key = StorageKeyGenerator.HlsSegment(trackId, 42);

        Assert.Equal($"audio/hls/{trackId:N}/seg_42.ts", key);
    }

    [Fact]
    public void NormalizeExtension_ShouldHandleLeadingDotAndCasing()
    {
        Assert.Equal(".mp3", StorageKeyGenerator.NormalizeExtension("mp3"));
        Assert.Equal(".mp3", StorageKeyGenerator.NormalizeExtension(".MP3"));
        Assert.Equal(".flac", StorageKeyGenerator.NormalizeExtension("  FLAC "));
        Assert.Equal(string.Empty, StorageKeyGenerator.NormalizeExtension(""));
    }
}
