using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using PortalCalendarServer.Data;
using PortalCalendarServer.Models.DatabaseEntities;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Services.Integrations;

/// <summary>
/// Calls the OpenAI Images API (DALL-E) to generate an image from a text prompt.
/// </summary>
public class AiImageIntegrationService(
    ILogger<AiImageIntegrationService> loggerParam,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IDatabaseCacheServiceFactory databaseCacheFactory,
    CalendarContext context,
    string apiKey,
    string prompt,
    string size,
    string quality,
    string style
    ) : IntegrationServiceBase(loggerParam, httpClientFactory, memoryCache, databaseCacheFactory, context)
{
    private new readonly ILogger<AiImageIntegrationService> logger = loggerParam;

    private const string ApiUrl = "https://api.openai.com/v1/images/generations";

    public override bool IsConfigured(Display display)
    {
        return !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(prompt);
    }

    /// <summary>
    /// Generate an image using the OpenAI DALL-E API.
    /// Returns the raw image bytes (PNG).
    /// </summary>
    public async Task<AiImageResult> GenerateImageAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating AI image with prompt: {Prompt}", prompt);

        var requestBody = new OpenAiImageRequest
        {
            Model = "dall-e-3",
            Prompt = prompt,
            N = 1,
            Size = size,
            Quality = quality,
            Style = style,
            ResponseFormat = "b64_json"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient("AiImageGeneration");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.PostAsync(ApiUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OpenAiImageResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to parse OpenAI image response");

        if (result.Data == null || result.Data.Count == 0)
            throw new InvalidOperationException("OpenAI returned no image data");

        var imageData = result.Data[0];
        var imageBytes = Convert.FromBase64String(imageData.B64Json
            ?? throw new InvalidOperationException("OpenAI returned no base64 image data"));

        logger.LogInformation("AI image generated successfully ({Size} bytes), revised prompt: {RevisedPrompt}",
            imageBytes.Length, imageData.RevisedPrompt);

        return new AiImageResult
        {
            ImageBytes = imageBytes,
            RevisedPrompt = imageData.RevisedPrompt
        };
    }

    // ── OpenAI API DTOs ──────────────────────────────────────────────────────

    private class OpenAiImageRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "dall-e-3";

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public int N { get; set; } = 1;

        [JsonPropertyName("size")]
        public string Size { get; set; } = "1024x1024";

        [JsonPropertyName("quality")]
        public string Quality { get; set; } = "standard";

        [JsonPropertyName("style")]
        public string Style { get; set; } = "natural";

        [JsonPropertyName("response_format")]
        public string ResponseFormat { get; set; } = "b64_json";
    }

    private class OpenAiImageResponse
    {
        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("data")]
        public List<OpenAiImageData>? Data { get; set; }
    }

    private class OpenAiImageData
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; set; }

        [JsonPropertyName("revised_prompt")]
        public string? RevisedPrompt { get; set; }
    }
}

public class AiImageResult
{
    public required byte[] ImageBytes { get; set; }
    public string? RevisedPrompt { get; set; }
}
