using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SecretRotationHistoryEntityTypeConfiguration : IEntityTypeConfiguration<SecretRotationHistory>
{
    public void Configure(EntityTypeBuilder<SecretRotationHistory> builder)
    {
        builder.HasKey(s => s.RotationId);
        
        builder.Property(s => s.SecretName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.SecretType).IsRequired().HasMaxLength(50);
        builder.Property(s => s.RotationType).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Status).IsRequired().HasMaxLength(50);
        
        builder.HasIndex(s => s.SecretName);
        builder.HasIndex(s => new { s.SecretName, s.IsActive });
        builder.HasIndex(s => s.RotatedAt);
        builder.HasIndex(s => s.Status);
    }
}
