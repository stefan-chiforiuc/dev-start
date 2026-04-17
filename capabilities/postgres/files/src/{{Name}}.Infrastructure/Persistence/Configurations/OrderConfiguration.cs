using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {{Name}}.Domain.Orders;

namespace {{Name}}.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");

        b.HasKey(o => o.Id);
        b.Property(o => o.Id)
            .HasConversion(id => id.Value, v => new OrderId(v));

        b.Property(o => o.CustomerEmail).HasMaxLength(320).IsRequired();
        b.Property(o => o.PlacedAt);

        b.OwnsMany(o => o.Lines, lb =>
        {
            lb.ToTable("order_lines");
            lb.WithOwner().HasForeignKey("order_id");
            lb.Property<int>("id").ValueGeneratedOnAdd();
            lb.HasKey("id");
            lb.Property(l => l.Sku).HasMaxLength(64).IsRequired();
            lb.Property(l => l.Quantity);
            lb.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });

        b.Ignore(o => o.DomainEvents);
        b.Ignore(o => o.Total);
    }
}
