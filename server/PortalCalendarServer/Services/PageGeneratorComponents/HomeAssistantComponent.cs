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
            memoryCache.Set(cacheKey, entities, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                Size = entities.Count * 512 // Rough estimate
            });
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

    /// <summary>
    /// Render a Jinja2 template on the Home Assistant server and return the result as a string.
    /// This enables access to HA functions like device_attr(), area_name(), etc.
    /// </summary>
    public async Task<string?> RenderTemplateAsync(Display display, string template)
    {
        var (url, token) = GetConnectionSettings(display);
        if (url == null || token == null)
            return null;

        try
        {
            return await FetchTemplateAsync(url, token, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering HA template");
            return null;
        }
    }

    /// <summary>
    /// Get enriched details for a list of entity IDs, including device name and area name
    /// resolved via the HA template API. Results are cached for 1 minute.
    /// </summary>
    public async Task<IReadOnlyList<HomeAssistantEntityDetails>> GetEntityDetailsAsync(Display display, IReadOnlyList<string> entityIds)
    {
        if (entityIds.Count == 0)
            return [];

        var (url, token) = GetConnectionSettings(display);
        if (url == null || token == null)
            return [];

        var cacheKey = $"ha_details_{display.Id}_{string.Join(",", entityIds)}";
        if (memoryCache.TryGetValue(cacheKey, out IReadOnlyList<HomeAssistantEntityDetails>? cached) && cached != null)
            return cached;

        try
        {
            // Fetch states
            var states = await GetStatesAsync(display);
            var statesDict = states.ToDictionary(e => e.EntityId);

            // Build a Jinja2 template that returns JSON with device/area info for all entities.
            // Using $$$ so that {{{ }}} is C# interpolation while {{ }} passes through as Jinja2.
            var entityList = string.Join(",", entityIds.Select(id => $"\"{id}\""));
            var template = $$$"""
                {%- set ids = [{{{entityList}}}] -%}
                [
                {%- for eid in ids -%}
                  {%- set did = device_id(eid) -%}
                  {
                    "entity_id": "{{ eid }}",
                    "device_name": "{{ device_attr(did, 'name_by_user') or device_attr(did, 'name') if did else '' }}",
                    "area_name": "{{ area_name(area_id(eid)) if area_id(eid) else '' }}"
                  }{%- if not loop.last -%},{%- endif -%}
                {%- endfor -%}
                ]
                """;

            var templateResult = await FetchTemplateAsync(url, token, template);
            var detailEntries = JsonSerializer.Deserialize<List<EntityDetailEntry>>(templateResult, JsonOptions) ?? [];

            var results = new List<HomeAssistantEntityDetails>();
            foreach (var entry in detailEntries)
            {
                statesDict.TryGetValue(entry.EntityId, out var entity);
                results.Add(new HomeAssistantEntityDetails
                {
                    Entity = entity,
                    EntityId = entry.EntityId,
                    DeviceName = string.IsNullOrWhiteSpace(entry.DeviceName) ? null : entry.DeviceName,
                    AreaName = string.IsNullOrWhiteSpace(entry.AreaName) ? null : entry.AreaName,
                });
            }

            memoryCache.Set(cacheKey, (IReadOnlyList<HomeAssistantEntityDetails>)results, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                Size = results.Count * 512 // Rough estimate
            });
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching entity details from Home Assistant");
            return [];
        }
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

    private async Task<string> FetchTemplateAsync(string baseUrl, string token, string template)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new { template });
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{baseUrl}/api/template", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private class EntityDetailEntry
    {
        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = "";

        [JsonPropertyName("device_name")]
        public string DeviceName { get; set; } = "";

        [JsonPropertyName("area_name")]
        public string AreaName { get; set; } = "";
    }
}

/// <summary>
/// An entity enriched with device and area information from the HA registry.
/// </summary>
public class HomeAssistantEntityDetails
{
    /// <summary>The full entity state, or null if the entity was not found in /api/states.</summary>
    public HomeAssistantEntity? Entity { get; set; }

    /// <summary>The entity_id as configured.</summary>
    public string EntityId { get; set; } = "";

    /// <summary>The name of the device this entity belongs to, if any.</summary>
    public string? DeviceName { get; set; }

    /// <summary>The area name assigned to this entity (or its device), if any.</summary>
    public string? AreaName { get; set; }
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
