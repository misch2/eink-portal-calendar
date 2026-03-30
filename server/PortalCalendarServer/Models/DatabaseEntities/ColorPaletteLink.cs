using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PortalCalendarServer.Models.DatabaseEntities
{
    public class ColorPaletteLink
    {
        public int Id { get; set; }
        public required string ColorVariantCode { get; set; }
        public required string EpdColorCode { get; set; }

        /// <summary>
        /// Optional per-variant override for the web preview hex value.
        /// When null, falls back to <see cref="EpdColor.EpdPreviewHexValue"/>.
        /// </summary>
        public string? EpdPreviewHexValueOverride { get; set; }

        public ColorVariant ColorVariant { get; set; } = null!;
        public EpdColor EpdColor { get; set; } = null!;

        // Resolved values: use override if present, otherwise fall back to the base EpdColor
        public string EffectiveHexValue => EpdColor.HexValue;   // there's no override for regular (non-preview) hex value
        public string EffectiveEpdPreviewHexValue => EpdPreviewHexValueOverride ?? EpdColor.EpdPreviewHexValue;

        private Rgba32? _effectivePreviewRgba32;
        /// <summary>
        /// Parsed RGBA32 representation of the effective preview hex value for fast pixel comparison.
        /// </summary>
        public Rgba32 EffectiveEpdPreviewRgba32Value => _effectivePreviewRgba32 ??= Color.ParseHex(EffectiveEpdPreviewHexValue).ToPixel<Rgba32>();
    }
}
