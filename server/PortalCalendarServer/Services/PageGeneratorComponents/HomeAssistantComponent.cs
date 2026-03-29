using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Models.DatabaseEntities;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortalCalendarServer.Services.PageGeneratorComponents;

/// <summary>
/// Component for reading entity states from a Home Assistant instance via its REST API.
/// </summary>
public class HomeAssistantComponent(
    IDisplayService displayService,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<HomeAssistantComponent>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>0
    /// Get all entity states from Home Assistant.
    /// Results are cached in memory for 1 minute to avoid excessive API calls during rendering.
    /// </summary>
    public async Task<IReadOnlyList<HomeAssistantEntity>> GetStatesAsync(Display display)
    {
        var (url, token) = GetConnectionSettings(display);
        if (url == null || token == null)
            return [];

        var cacheKey = $"ha_states_{display.Id}";
        if (memoryCache.TryGetValue(cacheKey, out IReadOnlyList<HomeAssistantEntity>? cached) && cached != null)
            return cached;

        try
        {
            var entities = await FetchStatesAsync(url, token);
            memoryCache.Set(cacheKey, entities, TimeSpan.FromMinutes(1));
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching states from Home Assistant at {Url}", url);
            return [];
        }
    }

    /// <summary>
    /// Get the state of a single entity by its entity_id.
    /// </summary>
    public async Task<HomeAssistantEntity?> GetEntityStateAsync(Display display, string entityId)
    {
        var states = await GetStatesAsync(display);
        return states.FirstOrDefault(e => e.EntityId == entityId);
    }

    /// <summary>
    /// Get all entities matching a domain (e.g. "sensor", "light", "switch").
    /// </summary>
    public async Task<IReadOnlyList<HomeAssistantEntity>> GetEntitiesByDomainAsync(Display display, string domain)
    {
        var states = await GetStatesAsync(display);
        var prefix = domain + ".";
        return states.Where(e => e.EntityId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private (string? url, string? token) GetConnectionSettings(Display display)
    {
        if (!displayService.GetConfigBool(display, "homeassistant"))
            return (null, null);

        var url = displayService.GetConfig(display, "homeassistant_url");
        var token = displayService.GetConfig(display, "homeassistant_token");

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Home Assistant enabled but URL or token not configured");
            return (null, null);
        }

        return (url.TrimEnd('/'), token);
    }

    private async Task<IReadOnlyList<HomeAssistantEntity>> FetchStatesAsync(string baseUrl, string token)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{baseUrl}/api/states");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var entities = JsonSerializer.Deserialize<List<HomeAssistantEntity>>(json, JsonOptions);
        return entities ?? [];
    }
}

/// <summary>
/// Represents a Home Assistant entity with its current state and attributes.
/// </summary>
public class HomeAssistantEntity
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("attributes")]
    public JsonElement Attributes { get; set; }

    [JsonPropertyName("last_changed")]
    public DateTime? LastChanged { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// The domain portion of the entity_id (e.g. "sensor" from "sensor.temperature").
    /// </summary>
    public string Domain => EntityId.Contains('.') ? EntityId[..EntityId.IndexOf('.')] : "";

    /// <summary>
    /// The friendly name from attributes, or the entity_id if not available.
    /// </summary>
    public string FriendlyName =>
        Attributes.ValueKind == JsonValueKind.Object
        && Attributes.TryGetProperty("friendly_name", out var name)
        && name.ValueKind == JsonValueKind.String
            ? name.GetString() ?? EntityId
            : EntityId;

    /// <summary>
    /// The unit of measurement from attributes, if available.
    /// </summary>
    public string? UnitOfMeasurement =>
        Attributes.ValueKind == JsonValueKind.Object
        && Attributes.TryGetProperty("unit_of_measurement", out var unit)
        && unit.ValueKind == JsonValueKind.String
            ? unit.GetString()
            : null;

    /// <summary>
    /// Get a typed attribute value. Returns null if the attribute doesn't exist.
    /// </summary>
    public string? GetAttribute(string name)
    {
        if (Attributes.ValueKind != JsonValueKind.Object)
            return null;
        if (!Attributes.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }
}
