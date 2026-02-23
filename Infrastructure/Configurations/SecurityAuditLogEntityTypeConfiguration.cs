using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SecurityAuditLogEntityTypeConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.HasKey(s => s.SecurityAuditLogId);
        
        builder.Property(s => s.EventType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Severity).IsRequired().HasMaxLength(20);
        
        builder.HasIndex(s => s.Timestamp);
        builder.HasIndex(s => s.EventType);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.IpAddress);
        builder.HasIndex(s => s.RequiresInvestigation);
        builder.HasIndex(s => new { s.RequiresInvestigation, s.IsInvestigated });
        builder.HasIndex(s => new { s.Timestamp, s.EventType });
        builder.HasIndex(s => new { s.UserId, s.Timestamp });
    }
}
