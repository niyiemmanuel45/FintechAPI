using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AuditLogEntityTypeConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.AuditLogId);
        
        builder.Property(a => a.EventType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Category).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Severity).IsRequired().HasMaxLength(20);
        
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.EventType);
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.PaymentTransactionId);
        builder.HasIndex(a => a.CorrelationId);
        builder.HasIndex(a => new { a.Timestamp, a.EventType });
        builder.HasIndex(a => new { a.UserId, a.Timestamp });
    }
}
