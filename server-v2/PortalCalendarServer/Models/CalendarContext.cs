using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PortalCalendarServer.Models;

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

    public virtual DbSet<MojoMigration> MojoMigrations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=..\\_data\\devel\\calendar.db");

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
            entity.Property(e => e.Colortype)
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

        modelBuilder.Entity<MojoMigration>(entity =>
        {
            entity.HasKey(e => e.Name);

            entity.ToTable("mojo_migrations");

            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
