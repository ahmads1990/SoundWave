namespace SoundWave.Catalog.Common;

internal enum CatalogError
{
    None = 0,
    GenreAlreadyExists,
    GenreNotFound,
    ArtistApplicationAlreadyExists,
    ArtistApplicationNotFound,
    ArtistApplicationAlreadyProcessed,
    ArtistNotFound,
    UserNotAuthenticated,
    AlbumNotFound,
    TrackNotFound,
    AlbumAlreadyPublished,
    CannotPublishEmptyAlbum,
    UnauthorizedAlbumAccess,
    UnauthorizedTrackAccess,
    InvalidGenreId,
    InternalError,
}

internal enum AlbumType : byte
{
    Album = 0,
    EP = 1,
    Single = 2,
}

internal enum TrackFileStatus : byte
{
    Pending = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3,
}

internal enum GenreType : byte
{
    Genre = 0,
    Mood = 1,
}

internal enum ArtistApprovalStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
