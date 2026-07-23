using System.Text.Json.Serialization;

namespace SoundWave.SharedKernel.Common;

public enum UserRole
{
    Listener,
    Artist,
    Admin
}

public enum SortingDirection
{
    [JsonPropertyName("asc")]
    Ascending,

    [JsonPropertyName("desc")]
    Descending
}