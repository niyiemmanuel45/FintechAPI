using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class IdempotencyKeyEntityTypeConfiguration : IEntityTypeConfiguration<IdempotencyKey>
    {
        public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
        {
            builder.HasKey(k => k.IdempotencyKeyId);

            builder.Property(k => k.IdempotencyKeyValue)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(k => k.IdempotencyKeyValue)
                .IsUnique();

            builder.HasIndex(k => new { k.UserId, k.IdempotencyKeyValue })
                .IsUnique()
                .HasName("UQ_UserId_IdempotencyKeyValue");

            builder.HasIndex(k => k.ExpiresAt);
        }
    }
}