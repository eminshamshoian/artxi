using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuctionService.Infrastructure.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Item>    Items    => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One-to-one with FK on Auction.ItemId (Option A)
        modelBuilder.Entity<Auction>()
            .HasOne(a => a.Item)
            .WithOne(i => i.Auction)
            .HasForeignKey<Auction>(a => a.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique to enforce 1:1
        modelBuilder.Entity<Auction>()
            .HasIndex(a => a.ItemId)
            .IsUnique();

        // Store enums as strings (optional but recommended)
        modelBuilder.Entity<Auction>().Property(a => a.Status).HasConversion<string>();
        modelBuilder.Entity<Item>().Property(i => i.Status).HasConversion<string>();

        // Money precision (adjust to your provider)
        modelBuilder.Entity<Auction>().Property(a => a.StartingPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Auction>().Property(a => a.ReservePrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Auction>().Property(a => a.BuyNowPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Auction>().Property(a => a.MinimumBidIncrement).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Auction>().Property(a => a.CurrentHighBid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Auction>().Property(a => a.SoldAmount).HasColumnType("decimal(18,2)");

        // If you're on PostgreSQL and using text[] / hstore (per your screenshot):
        // modelBuilder.HasPostgresExtension("hstore");

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

            var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, long?>(
                v => v.HasValue ? v.Value.UtcTicks : (long?)null,
                v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : (DateTimeOffset?)null);

            modelBuilder.Entity<Auction>().Property(a => a.CreatedAt).HasConversion(dateTimeOffsetConverter);
            modelBuilder.Entity<Auction>().Property(a => a.UpdatedAt).HasConversion(dateTimeOffsetConverter);
            modelBuilder.Entity<Auction>().Property(a => a.StartsAt).HasConversion(dateTimeOffsetConverter);
            modelBuilder.Entity<Auction>().Property(a => a.EndsAt).HasConversion(dateTimeOffsetConverter);

            modelBuilder.Entity<Item>().Property(i => i.CreatedAt).HasConversion(dateTimeOffsetConverter);
            modelBuilder.Entity<Item>().Property(i => i.PublishedAt).HasConversion(nullableDateTimeOffsetConverter);

            var tagsProperty = modelBuilder.Entity<Item>()
                .Property(i => i.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>());

            var tagsComparer = new ValueComparer<List<string>>(
                (left, right) =>
                    left == null || right == null
                        ? left == right
                        : left.SequenceEqual(right),
                v => (v ?? new List<string>()).Aggregate(0,
                    (hash, element) => HashCode.Combine(hash, StringComparer.Ordinal.GetHashCode(element ?? string.Empty))),
                v => (v ?? new List<string>()).ToList());

            tagsProperty.Metadata.SetValueComparer(tagsComparer);

            var attributesProperty = modelBuilder.Entity<Item>()
                .Property(i => i.Attributes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new Dictionary<string, string>()
                        : JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>());

            var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
                (left, right) =>
                    left == null || right == null
                        ? left == right
                        : left.Count == right.Count &&
                          left.OrderBy(kv => kv.Key).SequenceEqual(right.OrderBy(kv => kv.Key)),
                v => (v ?? new Dictionary<string, string>()).Aggregate(0,
                    (hash, kv) => HashCode.Combine(hash,
                        StringComparer.Ordinal.GetHashCode(kv.Key),
                        StringComparer.Ordinal.GetHashCode(kv.Value ?? string.Empty))),
                v => v != null
                    ? v.ToDictionary(entry => entry.Key, entry => entry.Value)
                    : new Dictionary<string, string>());

            attributesProperty.Metadata.SetValueComparer(dictionaryComparer);
        }
    }
}
