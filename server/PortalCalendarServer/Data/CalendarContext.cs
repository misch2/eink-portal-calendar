using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Models.DatabaseEntities;

namespace PortalCalendarServer.Data;

public partial class CalendarContext : DbContext
{
    public CalendarContext()
    {
    }

    public CalendarContext(DbContextOptions<CalendarContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cache> Caches { get; set; }

    public virtual DbSet<ColorPaletteLink> ColorPaletteLinks { get; set; }

    public virtual DbSet<ColorVariant> ColorVariants { get; set; }

    public virtual DbSet<Config> Configs { get; set; }

    public virtual DbSet<Display> Displays { get; set; }

    public virtual DbSet<DisplayType> DisplayTypes { get; set; }

    public virtual DbSet<DitheringType> DitheringTypes { get; set; }

    public virtual DbSet<EpdColor> EpdColors { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cache>(entity =>
        {
            entity.ToTable("cache");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.Creator, e.Key }, "cache_creator_key").IsUnique();

            entity.HasIndex(e => new { e.ExpiresAt, e.Creator }, "cache_expires_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("0")
                .HasColumnType("DATETIME")
                .HasColumnName("created_at");
            entity.Property(e => e.Creator)
                .HasColumnType("VARCHAR(255)")
                .HasColumnName("creator");
            entity.Property(e => e.Data).HasColumnName("data");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("0")
                .HasColumnType("DATETIME")
                .HasColumnName("expires_at");
            entity.Property(e => e.Key)
                .HasColumnType("VARCHAR(255)")
                .HasColumnName("key");
        });

        modelBuilder.Entity<Config>(entity =>
        {
            entity.ToTable("config");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.Name, e.DisplayId }, "config_name_display").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayId).HasColumnName("display_id");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.Value)
                .HasColumnType("VARCHAR")
                .HasColumnName("value");

            entity.HasOne(d => d.Display).WithMany(p => p.Configs)
                .HasForeignKey(d => d.DisplayId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Display>(entity =>
        {
            entity.ToTable("displays");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Mac, "IX_displays_mac").IsUnique();

            entity.HasIndex(e => e.Name, "IX_displays_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BorderBottom).HasColumnName("border_bottom");
            entity.Property(e => e.BorderLeft).HasColumnName("border_left");
            entity.Property(e => e.BorderRight).HasColumnName("border_right");
            entity.Property(e => e.BorderTop).HasColumnName("border_top");
            entity.Property(e => e.DisplayTypeCode)
                .HasColumnType("VARCHAR")
                .HasColumnName("display_type");
            entity.Property(e => e.ColorVariantCode)
                .HasColumnType("VARCHAR")
                .HasColumnName("color_variant");
            entity.Property(e => e.Firmware)
                .HasColumnType("VARCHAR")
                .HasColumnName("firmware");
            entity.Property(e => e.Gamma)
                .HasColumnType("NUMERIC(4,2)")
                .HasColumnName("gamma");
            entity.Property(e => e.Height).HasColumnName("height");
            entity.Property(e => e.Mac)
                .HasColumnType("VARCHAR")
                .HasColumnName("mac");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.Rotation).HasColumnName("rotation");
            entity.Property(e => e.Width).HasColumnName("width");
            entity.Property(e => e.ThemeId).HasColumnName("theme_id");
            entity.Property(e => e.DitheringTypeCode).HasColumnName("dithering_type_code");
            entity.Property(e => e.RenderedAt)
                .HasColumnType("DATETIME")
                .HasColumnName("rendered_at");
            entity.Property(e => e.RenderErrors).HasColumnName("render_errors");

            entity.HasOne(d => d.Theme).WithMany(p => p.Displays)
                .HasForeignKey(d => d.ThemeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.DisplayType).WithMany(p => p.Displays)
                .HasForeignKey(d => d.DisplayTypeCode)
                .OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(d => d.ColorVariant).WithMany(p => p.Displays)
                .HasForeignKey(d => d.ColorVariantCode)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.DitheringType).WithMany(p => p.Displays)
                .HasForeignKey(d => d.DitheringTypeCode)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DitheringType>(entity =>
        {
            entity.ToTable("dithering_types");

            entity.HasKey(e => e.Code);

            entity.Property(e => e.Code)
                .HasColumnType("VARCHAR")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasData(
                new DitheringType { Code = "", Name = "None", SortOrder = 100 },
                new DitheringType { Code = "fs", Name = "Floyd-Steinberg", SortOrder = 200 },
                new DitheringType { Code = "at", Name = "Atkinson", SortOrder = 300 },
                new DitheringType { Code = "jjn", Name = "Jarvis, Judice, Ninke", SortOrder = 400 },
                new DitheringType { Code = "st", Name = "Stucki", SortOrder = 500 }
             );
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.ToTable("themes");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.FileName).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName)
                .HasColumnType("VARCHAR")
                .HasColumnName("file_name");
            entity.Property(e => e.DisplayName)
                .HasColumnType("VARCHAR")
                .HasColumnName("display_name");
            entity.Property(e => e.HasCustomConfig).HasColumnName("has_custom_config");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);

            entity.HasData(
                new Theme { Id = 1, FileName = "Default", DisplayName = "Default", HasCustomConfig = false, SortOrder = 0, IsDefault = true },
                new Theme { Id = 2, FileName = "PortalStyleCalendarWithIcons", DisplayName = "Portal Style Calendar with Icons", HasCustomConfig = true, SortOrder = 100 },
                new Theme { Id = 3, FileName = "GoogleFitWeightWithCalendarAndIcons", DisplayName = "Google Fit Weight with Calendar and Icons", HasCustomConfig = true, SortOrder = 200 },
                new Theme { Id = 4, FileName = "WeatherForecast", DisplayName = "Weather", HasCustomConfig = false, SortOrder = 300 },
                new Theme { Id = 5, FileName = "MultidayCalendar", DisplayName = "Multi-day Calendar", HasCustomConfig = true, SortOrder = 400 },
                new Theme { Id = 6, FileName = "XKCD", DisplayName = "XKCD", HasCustomConfig = false, SortOrder = 500 },
                new Theme { Id = 7, FileName = "Test", DisplayName = "Test - Color Wheel", HasCustomConfig = false, SortOrder = 10000 }
             );
        });

        modelBuilder.Entity<EpdColor>(entity =>
        {
            entity.ToTable("epd_colors");

            entity.HasKey(e => e.Code);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Code)
                .HasColumnType("VARCHAR")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.HexValue)
                .HasColumnType("VARCHAR")
                .HasColumnName("hex_value");
            entity.Property(e => e.EpdPreviewHexValue)
                .HasColumnType("VARCHAR")
                .HasColumnName("epd_preview_hex_value");

            entity.HasData(
                new EpdColor { Code = "black", Name = "Black", HexValue = "000000", EpdPreviewHexValue = "111111" },
                new EpdColor { Code = "white", Name = "White", HexValue = "FFFFFF", EpdPreviewHexValue = "dddddd" },
                new EpdColor { Code = "red", Name = "Red", HexValue = "FF0000", EpdPreviewHexValue = "aa0000" },
                new EpdColor { Code = "yellow", Name = "Yellow", HexValue = "FFFF00", EpdPreviewHexValue = "dddd00" }
             );
        });

        modelBuilder.Entity<DisplayType>(entity =>
        {
            entity.ToTable("display_types");

            entity.HasKey(e => e.Code);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Code)
                .HasColumnType("VARCHAR")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.NumColors).HasColumnName("num_colors");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasMany(d => d.ColorVariants).WithOne(p => p.DisplayType)
                .HasForeignKey(d => d.DisplayTypeCode)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(d => d.Displays).WithOne(p => p.DisplayType)
                .HasForeignKey(d => d.DisplayTypeCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new DisplayType { Code = "BW", Name = "Black and White", NumColors = 2, SortOrder = 100 },
                new DisplayType { Code = "3C", Name = "3 Colors", NumColors = 3, SortOrder = 200 },
                new DisplayType { Code = "4C", Name = "4 Colors", NumColors = 4, SortOrder = 300 }
             );
        });

        modelBuilder.Entity<ColorVariant>(entity =>
        {
            entity.ToTable("color_variants");

            entity.HasKey(e => e.Code);

            entity.HasIndex(e => new { e.DisplayTypeCode, e.Name }).IsUnique();

            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.DisplayTypeCode)
                .HasColumnType("VARCHAR")
                .HasColumnName("display_type_code");
            entity.HasOne(d => d.DisplayType).WithMany(p => p.ColorVariants)
                .HasForeignKey(d => d.DisplayTypeCode)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasMany(d => d.EpdColors).WithMany(p => p.ColorVariants)
                .UsingEntity<ColorPaletteLink>(
                    j => j.HasOne(cl => cl.EpdColor).WithMany().HasForeignKey(cl => cl.EpdColorCode).OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(cl => cl.ColorVariant).WithMany().HasForeignKey(cl => cl.ColorVariantCode).OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey(cl => new { cl.ColorVariantCode, cl.EpdColorCode });
                        j.ToTable("color_palette_links");
                    }
                );


            entity.HasMany(d => d.Displays).WithOne(p => p.ColorVariant)
                .HasForeignKey(d => d.ColorVariantCode)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasData(
                new ColorVariant { Code = "BW", Name = "Black and White", DisplayTypeCode = "BW", SortOrder = 1000 },
                new ColorVariant { Code = "BWY", Name = "Black, White, Yellow", DisplayTypeCode = "3C", SortOrder = 2000 },
                new ColorVariant { Code = "BWR", Name = "Black, White, Red", DisplayTypeCode = "3C", SortOrder = 2010 },
                new ColorVariant { Code = "BWRY", Name = "Black, White, Red, Yellow", DisplayTypeCode = "4C", SortOrder = 3000 }
             );
        });

        modelBuilder.Entity<ColorPaletteLink>(entity =>
        {
            entity.ToTable("color_palette_links");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.ColorVariantCode, e.EpdColorCode }).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ColorVariantCode)
                .HasColumnType("VARCHAR")
                .HasColumnName("color_variant_code");
            entity.Property(e => e.EpdColorCode)
                .HasColumnType("VARCHAR")
                .HasColumnName("epd_color_code");

            entity.HasData(
                new ColorPaletteLink { Id = 1, ColorVariantCode = "BW", EpdColorCode = "black" },
                new ColorPaletteLink { Id = 2, ColorVariantCode = "BW", EpdColorCode = "white" },

                new ColorPaletteLink { Id = 3, ColorVariantCode = "BWY", EpdColorCode = "black" },
                new ColorPaletteLink { Id = 4, ColorVariantCode = "BWY", EpdColorCode = "white" },
                new ColorPaletteLink { Id = 5, ColorVariantCode = "BWY", EpdColorCode = "yellow" },

                new ColorPaletteLink { Id = 6, ColorVariantCode = "BWR", EpdColorCode = "black" },
                new ColorPaletteLink { Id = 7, ColorVariantCode = "BWR", EpdColorCode = "white" },
                new ColorPaletteLink { Id = 8, ColorVariantCode = "BWR", EpdColorCode = "red" },

                new ColorPaletteLink { Id = 9, ColorVariantCode = "BWRY", EpdColorCode = "black" },
                new ColorPaletteLink { Id = 10, ColorVariantCode = "BWRY", EpdColorCode = "white" },
                new ColorPaletteLink { Id = 11, ColorVariantCode = "BWRY", EpdColorCode = "red" },
                new ColorPaletteLink { Id = 12, ColorVariantCode = "BWRY", EpdColorCode = "yellow" }
            );
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
