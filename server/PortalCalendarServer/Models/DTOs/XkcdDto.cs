using System.Text.Json.Serialization;

namespace PortalCalendarServer.Models.DTOs;

/// <summary>
/// Data Transfer Object for XKCD API JSON response.
/// See documentation at https://xkcd.com/json.html
/// </summary>
public class XkcdApiResponse
{
    /// <summary>
    /// Comic number
    /// </summary>
    [JsonPropertyName("num")]
    public int Number { get; set; }

    /// <summary>
    /// Comic title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Safe version of the title (URL-friendly)
    /// </summary>
    [JsonPropertyName("safe_title")]
    public string SafeTitle { get; set; } = string.Empty;

    /// <summary>
    /// Alt text / tooltip text for the comic
    /// </summary>
    [JsonPropertyName("alt")]
    public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// URL to the comic image
    /// </summary>
    [JsonPropertyName("img")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Transcript of the comic (often empty)
    /// </summary>
    [JsonPropertyName("transcript")]
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Year the comic was published
    /// </summary>
    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    /// <summary>
    /// Month the comic was published
    /// </summary>
    [JsonPropertyName("month")]
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Day the comic was published
    /// </summary>
    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    /// <summary>
    /// Link to related content (often empty)
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// News or additional information (often empty)
    /// </summary>
    [JsonPropertyName("news")]
    public string News { get; set; } = string.Empty;
}
