using Microsoft.EntityFrameworkCore;
using PortalCalendarServer.Models.Entities;

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

    public virtual DbSet<Config> Configs { get; set; }

    public virtual DbSet<Display> Displays { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cache>(entity =>
        {
            entity.ToTable("cache");

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

            entity.HasIndex(e => e.Mac, "IX_displays_mac").IsUnique();

            entity.HasIndex(e => e.Name, "IX_displays_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BorderBottom).HasColumnName("border_bottom");
            entity.Property(e => e.BorderLeft).HasColumnName("border_left");
            entity.Property(e => e.BorderRight).HasColumnName("border_right");
            entity.Property(e => e.BorderTop).HasColumnName("border_top");
            entity.Property(e => e.ColorType)
                .HasColumnType("VARCHAR")
                .HasColumnName("colortype");
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
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.HasData(
                new Theme { Id = 1, FileName = "Default", DisplayName = "Default", HasCustomConfig = false, SortOrder = 0 },
                new Theme { Id = 2, FileName = "PortalStyleCalendarWithIcons", DisplayName = "Portal Style Calendar with Icons", HasCustomConfig = true, SortOrder = 100 },
                new Theme { Id = 3, FileName = "GoogleFitWeightWithCalendarAndIcons", DisplayName = "Google Fit Weight with Calendar and Icons", HasCustomConfig = true, SortOrder = 200 },
                new Theme { Id = 4, FileName = "WeatherForecast", DisplayName = "Weather", HasCustomConfig = false, SortOrder = 300 },
                new Theme { Id = 5, FileName = "MultidayCalendar", DisplayName = "Multi-day Calendar", HasCustomConfig = true, SortOrder = 400 },
                new Theme { Id = 6, FileName = "XKCD", DisplayName = "XKCD", HasCustomConfig = false, SortOrder = 500 },
                new Theme { Id = 7, FileName = "Test", DisplayName = "Test - Color Wheel", HasCustomConfig = false, SortOrder = 10000 }
             );

        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
