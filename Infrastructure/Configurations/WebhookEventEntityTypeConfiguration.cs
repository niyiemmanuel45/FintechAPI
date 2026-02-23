using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class WebhookEventEntityTypeConfiguration : IEntityTypeConfiguration<WebhookEvent>
    {
        public void Configure(EntityTypeBuilder<WebhookEvent> builder)
        {
            builder.HasKey(e => e.WebhookEventId);

            builder.Property(e => e.Provider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.ProviderEventId)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(e => e.ProviderEventId)
                .IsUnique();

            builder.HasIndex(e => new { e.Status, e.NextRetryAt });
        }
    }
}