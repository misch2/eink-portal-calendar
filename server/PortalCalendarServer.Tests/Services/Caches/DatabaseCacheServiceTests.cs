using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PortalCalendarServer.Data;
using PortalCalendarServer.Services.Caches;

namespace PortalCalendarServer.Tests.Services.Caches;

public class DatabaseCacheServiceTests : IAsyncDisposable
{
    private readonly SqliteFixture _fixture = new();

    private CalendarContext Context => _fixture.Context;

    private DatabaseCacheService CreateService(string creator = "test", TimeSpan? expiration = null)
        => _fixture.CreateService(creator, expiration);

    [Fact]
    public async Task GetOrSetAsync_CacheMiss_InvokesCallbackAndReturnsValue()
    {
        var service = CreateService();
        var callCount = 0;

        var result = await service.GetOrSetAsync(
            () => { callCount++; return Task.FromResult("hello"); },
            new { key = "k1" });

        Assert.Equal("hello", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheHit_ReturnsCachedValueWithoutCallingCallback()
    {
        var service = CreateService();
        var callCount = 0;

        await service.GetOrSetAsync(() => { callCount++; return Task.FromResult("first"); }, new { key = "k1" });
        var result = await service.GetOrSetAsync(() => { callCount++; return Task.FromResult("second"); }, new { key = "k1" });

        Assert.Equal("first", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrSetAsync_ExpiredEntry_RecalculatesValue()
    {
        var service = CreateService(expiration: TimeSpan.FromMilliseconds(-1)); // already expired
        var callCount = 0;

        await service.GetOrSetAsync(() => { callCount++; return Task.FromResult("first"); }, new { key = "k1" });
        var result = await service.GetOrSetAsync(() => { callCount++; return Task.FromResult("second"); }, new { key = "k1" });

        Assert.Equal("second", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrSetAsync_DifferentKeys_CachedIndependently()
    {
        var service = CreateService();

        var r1 = await service.GetOrSetAsync(() => Task.FromResult("value1"), new { key = "k1" });
        var r2 = await service.GetOrSetAsync(() => Task.FromResult("value2"), new { key = "k2" });

        Assert.Equal("value1", r1);
        Assert.Equal("value2", r2);
    }

    [Fact]
    public async Task GetOrSetAsync_DifferentCreators_CachedIndependently()
    {
        var service1 = CreateService(creator: "creator1");
        var service2 = CreateService(creator: "creator2");
        var cacheKey = new { key = "shared" };

        await service1.GetOrSetAsync(() => Task.FromResult("from-creator1"), cacheKey);
        var result = await service2.GetOrSetAsync(() => Task.FromResult("from-creator2"), cacheKey);

        Assert.Equal("from-creator2", result);
    }

    [Fact]
    public async Task GetOrSetAsync_StoresEntryInDatabase()
    {
        var service = CreateService(creator: "test-creator");

        await service.GetOrSetAsync(() => Task.FromResult(42), new { key = "k1" });

        var entry = await Context.Caches.FirstOrDefaultAsync(c => c.Creator == "test-creator");
        Assert.NotNull(entry);
        Assert.NotNull(entry.Data);
    }

    [Fact]
    public async Task GetOrSetAsync_UpdatesExistingEntryOnExpiry()
    {
        var expiredService = CreateService(expiration: TimeSpan.FromMilliseconds(-1));
        var freshService = CreateService(expiration: TimeSpan.FromHours(1));
        var cacheKey = new { key = "k1" };

        await expiredService.GetOrSetAsync(() => Task.FromResult("old"), cacheKey);
        await freshService.GetOrSetAsync(() => Task.FromResult("new"), cacheKey);

        var entryCount = await Context.Caches.CountAsync(c => c.Creator == "test");
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task GetOrSetAsync_WorksWithComplexType()
    {
        var service = CreateService();
        var payload = new MyData { Id = 7, Name = "test" };

        await service.GetOrSetAsync(() => Task.FromResult(payload), new { key = "complex" });
        var cached = await service.GetOrSetAsync(() => Task.FromResult(new MyData { Id = 99, Name = "ignored" }), new { key = "complex" });

        Assert.Equal(7, cached.Id);
        Assert.Equal("test", cached.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WorksWithByteArray()
    {
        var service = CreateService();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        await service.GetOrSetAsync(() => Task.FromResult(data), new { key = "bytes" });
        var cached = await service.GetOrSetAsync(() => Task.FromResult(Array.Empty<byte>()), new { key = "bytes" });

        Assert.Equal(data, cached);
    }

    [Fact]
    public async Task GetOrSetAsync_SupportsCancellation()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetOrSetAsync(() => Task.FromResult("x"), new { key = "k1" }, cts.Token));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntriesForCreator()
    {
        var service = CreateService(creator: "to-clear");
        var otherService = CreateService(creator: "keep");

        await service.GetOrSetAsync(() => Task.FromResult("a"), new { key = "k1" });
        await service.GetOrSetAsync(() => Task.FromResult("b"), new { key = "k2" });
        await otherService.GetOrSetAsync(() => Task.FromResult("c"), new { key = "k3" });

        await service.ClearAsync();

        var remaining = await Context.Caches.ToListAsync();
        Assert.DoesNotContain(remaining, c => c.Creator == "to-clear");
        Assert.Contains(remaining, c => c.Creator == "keep");
    }

    [Fact]
    public async Task ClearAsync_EmptyCache_DoesNotThrow()
    {
        var service = CreateService();

        await service.ClearAsync();
    }

    public async ValueTask DisposeAsync() => await _fixture.DisposeAsync();

    private sealed class MyData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public CalendarContext Context { get; }

        public SqliteFixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<CalendarContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new CalendarContext(options);
            Context.Database.EnsureCreated();
        }

        public DatabaseCacheService CreateService(string creator = "test", TimeSpan? expiration = null)
            => new(Context, NullLogger<DatabaseCacheService>.Instance, creator, expiration ?? TimeSpan.FromHours(1));

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
