using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionMismatchEntityTypeConfiguration : IEntityTypeConfiguration<TransactionMismatch>
{
    public void Configure(EntityTypeBuilder<TransactionMismatch> builder)
    {
        builder.HasKey(m => m.MismatchId);
        
        builder.Property(m => m.MismatchType).IsRequired().HasMaxLength(50);
        builder.Property(m => m.ResolutionStatus).IsRequired().HasMaxLength(50);
        
        builder.Property(m => m.InternalAmount).HasColumnType("decimal(18,2)");
        builder.Property(m => m.ProviderAmount).HasColumnType("decimal(18,2)");
        
        builder.HasIndex(m => m.ReconciliationReportId);
        builder.HasIndex(m => m.MismatchType);
        builder.HasIndex(m => m.ResolutionStatus);
        builder.HasIndex(m => m.DetectedAt);
        builder.HasIndex(m => m.InternalTransactionId);
    }
}
