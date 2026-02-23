using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReconciliationReportEntityTypeConfiguration : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.HasKey(r => r.ReconciliationReportId);
        
        builder.Property(r => r.ReportDate).IsRequired();
        builder.Property(r => r.PeriodStart).IsRequired();
        builder.Property(r => r.PeriodEnd).IsRequired();
        builder.Property(r => r.Status).IsRequired().HasMaxLength(50);
        
        builder.Property(r => r.TotalInternalAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalProviderAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalMatchedAmount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.TotalMismatchedAmount).HasColumnType("decimal(18,2)");
        
        builder.HasMany(r => r.Mismatches)
            .WithOne(m => m.ReconciliationReport)
            .HasForeignKey(m => m.ReconciliationReportId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(r => r.ReportDate);
        builder.HasIndex(r => new { r.PeriodStart, r.PeriodEnd });
        builder.HasIndex(r => r.Status);
    }
}
