using Microsoft.EntityFrameworkCore;

namespace PortalCalendarServer.Data;

public class AuthSession
{
    public string Key { get; set; } = null!;
    public byte[] Value { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class SessionContext : DbContext
{
    public SessionContext(DbContextOptions<SessionContext> options) : base(options) { }

    public DbSet<AuthSession> AuthSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("auth_sessions");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasColumnName("key").HasColumnType("VARCHAR(128)");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("DATETIME");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_auth_sessions_expires_at");
        });
    }
}
