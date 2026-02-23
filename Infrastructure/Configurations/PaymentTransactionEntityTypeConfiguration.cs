using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class PaymentTransactionEntityTypeConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasKey(t => t.PaymentTransactionId);

            builder.Property(t => t.TransactionNumber)
                .IsRequired()
                .HasMaxLength(128);

            builder.HasIndex(t => t.TransactionNumber)
                .IsUnique();

            builder.HasIndex(t => t.ProviderTransactionId);
            builder.HasIndex(t => t.CurrentState);
            builder.HasIndex(t => new { t.UserId, t.CreatedAt });
            builder.HasIndex(t => new { t.Provider, t.CreatedAt });
        }
    }
}