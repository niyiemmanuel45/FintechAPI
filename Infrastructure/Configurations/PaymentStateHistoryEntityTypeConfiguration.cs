using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class PaymentStateHistoryEntityTypeConfiguration : IEntityTypeConfiguration<PaymentStateHistory>
    {
        public void Configure(EntityTypeBuilder<PaymentStateHistory> builder)
        {
            builder.HasKey(h => h.StateHistoryId);

            builder.HasOne(h => h.PaymentTransaction)
                .WithMany(t => t.StateHistory)
                .HasForeignKey(h => h.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(h => h.PaymentTransactionId);
        }
    }
}