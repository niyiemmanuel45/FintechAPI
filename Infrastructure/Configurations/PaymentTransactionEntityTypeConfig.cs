using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentTransactionEntityTypeConfig : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(p => p.PaymentTransactionId);
        builder.Property(p => p.TransactionNumber).HasMaxLength(128);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasConversion<int>();
        builder.Property(p => p.CurrentState).HasConversion<int>();
        builder.Property(p => p.PreviousState).HasConversion<int?>();
        builder.Property(p => p.Provider).HasConversion<int>();
        builder.ToTable("PaymentTransactions");
    }
}
