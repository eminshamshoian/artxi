using System;
using AuctionService.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Tests.TestUtils;

public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public AuctionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseSqlite(_connection)
            .Options;

        var ctx = new AuctionDbContext(options);
        ctx.Database.EnsureCreated(); // builds schema with constraints
        return ctx;
    }

    public void Dispose() => _connection.Dispose();
}
