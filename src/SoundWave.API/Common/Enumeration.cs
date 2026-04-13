using System.Text.Json.Serialization;

public enum SortingDirection
{
    [JsonPropertyName("asc")]
    Ascending,

    [JsonPropertyName("desc")]
    Descending
}