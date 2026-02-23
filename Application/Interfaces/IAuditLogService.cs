using Core.Entities;

namespace Application.Interfaces;

public interface IAuditLogService
{
    // General audit logging
    Task LogAsync(AuditLog auditLog);
    Task LogPaymentEventAsync(string eventType, long? paymentTransactionId, string? transactionReference, 
        Guid? userId, string? provider, string? details, bool isSuccess = true);
    Task LogStateTransitionAsync(long paymentTransactionId, string fromState, string toState, 
        string? reason, Guid? userId);
    Task LogSystemEventAsync(string eventType, string? details, string severity = "Info");
    
    // Security audit logging
    Task LogSecurityEventAsync(SecurityAuditLog securityLog);
    Task LogLoginAttemptAsync(Guid? userId, string? email, string? ipAddress, bool isSuccess, string? failureReason = null);
    Task LogPasswordChangeAsync(Guid userId, string? ipAddress, bool isSuccess);
    Task LogSecretRotationAsync(string secretName, string rotationType, bool isSuccess, string? errorMessage = null);
    Task LogSuspiciousActivityAsync(string eventType, Guid? userId, string? ipAddress, string? threatIndicators);
    
    // Query methods
    Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
        string? eventType = null, Guid? userId = null, int limit = 100);
    Task<List<AuditLog>> GetPaymentAuditLogsAsync(long paymentTransactionId);
    Task<List<SecurityAuditLog>> GetSecurityAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
        string? eventType = null, Guid? userId = null, int limit = 100);
    Task<List<SecurityAuditLog>> GetSuspiciousActivitiesAsync(bool investigatedOnly = false);
}
