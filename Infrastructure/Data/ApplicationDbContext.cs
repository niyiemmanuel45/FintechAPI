using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{

    private readonly IConfiguration _configuration;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_configuration["ConnectionStrings:Remote"]);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        new BankAccountEntityTypeConfig().Configure(modelBuilder.Entity<BankAccount>());
        new OperationEntityTypeConfig().Configure(modelBuilder.Entity<Operation>());
        new ReceiverClientEntityTypeConfig().Configure(modelBuilder.Entity<ReceiverClient>());
        new BankCardEntityTypeConfig().Configure(modelBuilder.Entity<Card>());
        new PaymentEntityTypeConfig().Configure(modelBuilder.Entity<Payment>());
        // Payment orchestration entities
        new IdempotencyKeyEntityTypeConfiguration().Configure(modelBuilder.Entity<IdempotencyKey>());
        new WebhookEventEntityTypeConfiguration().Configure(modelBuilder.Entity<WebhookEvent>());
        new PaymentStateHistoryEntityTypeConfiguration().Configure(modelBuilder.Entity<PaymentStateHistory>());
        new PaymentTransactionEntityTypeConfiguration().Configure(modelBuilder.Entity<PaymentTransaction>());
        new StockEntityTypeConfiguration().Configure(modelBuilder.Entity<Stock>());
        // Reconciliation entities
        new ReconciliationReportEntityTypeConfiguration().Configure(modelBuilder.Entity<ReconciliationReport>());
        new TransactionMismatchEntityTypeConfiguration().Configure(modelBuilder.Entity<TransactionMismatch>());
        // Security entities
        new SecretRotationHistoryEntityTypeConfiguration().Configure(modelBuilder.Entity<SecretRotationHistory>());
        new AuditLogEntityTypeConfiguration().Configure(modelBuilder.Entity<AuditLog>());
        new SecurityAuditLogEntityTypeConfiguration().Configure(modelBuilder.Entity<SecurityAuditLog>());
    }

    public DbSet<BankAccount> Accounts { get; set; }
    public DbSet<Operation> Operations { get; set; }
    public DbSet<Card> BankCards { get; set; }
    public DbSet<ReceiverClient> ReceiverClients { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<PaymentProvider> PaymentProviders { get; set; }
    public DbSet<PaymentStateHistory> PaymentStateHistories { get; set; }
    public DbSet<ReconciliationReport> ReconciliationReports { get; set; }
    public DbSet<TransactionMismatch> TransactionMismatches { get; set; }
    public DbSet<SecretRotationHistory> SecretRotationHistory { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }
}