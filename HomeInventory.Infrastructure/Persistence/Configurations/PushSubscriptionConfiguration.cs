using HomeInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeInventory.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Endpoint).HasMaxLength(2048).IsRequired();
        builder.Property(s => s.P256dhKey).HasMaxLength(256).IsRequired();
        builder.Property(s => s.AuthKey).HasMaxLength(64).IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.Endpoint);
    }
}
