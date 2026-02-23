using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Core.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly string? _encryptionKey;

    public AuditLogService(
        ApplicationDbContext db,
        ILogger<AuditLogService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _encryptionKey = _configuration["AuditLog:EncryptionKey"];
    }

    public async Task LogAsync(AuditLog auditLog)
    {
        try
        {
            // Encrypt sensitive data if marked as sensitive
            if (auditLog.IsSensitive)
            {
                if (!string.IsNullOrEmpty(auditLog.OldValue))
                    auditLog.OldValue = EncryptData(auditLog.OldValue);
                
                if (!string.IsNullOrEmpty(auditLog.NewValue))
                    auditLog.NewValue = EncryptData(auditLog.NewValue);
                
                if (!string.IsNullOrEmpty(auditLog.Details))
                    auditLog.Details = EncryptData(auditLog.Details);
            }

            // Enrich with HTTP context if available
            EnrichWithHttpContext(auditLog);

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing audit log");
            // Don't throw - audit logging should not break application flow
        }
    }

    public async Task LogPaymentEventAsync(
        string eventType, 
        long? paymentTransactionId, 
        string? transactionReference,
        Guid? userId, 
        string? provider, 
        string? details, 
        bool isSuccess = true)
    {
        var auditLog = new AuditLog
        {
            EventType = eventType,
            Category = "Payment",
            Severity = isSuccess ? "Info" : "Error",
            UserId = userId,
            PaymentTransactionId = paymentTransactionId,
            TransactionReference = transactionReference,
            Provider = provider,
            Details = details,
            IsSuccess = isSuccess,
            IsSensitive = true, // Payment data is always sensitive
            CorrelationId = Guid.NewGuid().ToString()
        };

        await LogAsync(auditLog);
    }

    public async Task LogStateTransitionAsync(
        long paymentTransactionId, 
        string fromState, 
        string toState,
        string? reason, 
        Guid? userId)
    {
        var details = JsonSerializer.Serialize(new
        {
            FromState = fromState,
            ToState = toState,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });

        var auditLog = new AuditLog
        {
            EventType = "PaymentStateTransition",
            Category = "Payment",
            Severity = "Info",
            UserId = userId,
            PaymentTransactionId = paymentTransactionId,
            Action = "StateTransition",
            OldValue = fromState,
            NewValue = toState,
            Details = details,
            IsSuccess = true,
            IsSensitive = false
        };

        await LogAsync(auditLog);
    }

    public async Task LogSystemEventAsync(string eventType, string? details, string severity = "Info")
    {
        var auditLog = new AuditLog
        {
            EventType = eventType,
            Category = "System",
            Severity = severity,
            Details = details,
            IsSuccess = true,
            IsSensitive = false
        };

        await LogAsync(auditLog);
    }

    public async Task LogSecurityEventAsync(SecurityAuditLog securityLog)
    {
        try
        {
            // Encrypt sensitive details
            if (!string.IsNullOrEmpty(securityLog.Details))
                securityLog.Details = EncryptData(securityLog.Details);

            // Enrich with HTTP context if available
            EnrichSecurityLogWithHttpContext(securityLog);

            _db.SecurityAuditLogs.Add(securityLog);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing security audit log");
            // Don't throw - audit logging should not break application flow
        }
    }

    public async Task LogLoginAttemptAsync(
        Guid? userId, 
        string? email, 
        string? ipAddress, 
        bool isSuccess,
        string? failureReason = null)
    {
        var securityLog = new SecurityAuditLog
        {
            EventType = isSuccess ? "LoginSuccess" : "LoginFailed",
            Severity = isSuccess ? "Info" : "Warning",
            UserId = userId,
            UserEmail = email,
            IpAddress = ipAddress,
            Action = "Login",
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            RequiresInvestigation = !isSuccess && ShouldInvestigateFailedLogin(failureReason)
        };

        await LogSecurityEventAsync(securityLog);
    }

    public async Task LogPasswordChangeAsync(Guid userId, string? ipAddress, bool isSuccess)
    {
        var securityLog = new SecurityAuditLog
        {
            EventType = "PasswordChange",
            Severity = isSuccess ? "Info" : "Warning",
            UserId = userId,
            IpAddress = ipAddress,
            Action = "PasswordChange",
            IsSuccess = isSuccess,
            RequiresInvestigation = !isSuccess
        };

        await LogSecurityEventAsync(securityLog);
    }

    public async Task LogSecretRotationAsync(
        string secretName, 
        string rotationType, 
        bool isSuccess,
        string? errorMessage = null)
    {
        var details = JsonSerializer.Serialize(new
        {
            SecretName = secretName,
            RotationType = rotationType,
            Timestamp = DateTime.UtcNow,
            ErrorMessage = errorMessage
        });

        var securityLog = new SecurityAuditLog
        {
            EventType = "SecretRotation",
            Severity = isSuccess ? "Info" : "Critical",
            Action = "SecretRotation",
            Resource = "Secret",
            ResourceId = secretName,
            IsSuccess = isSuccess,
            FailureReason = errorMessage,
            Details = details,
            RequiresInvestigation = !isSuccess
        };

        await LogSecurityEventAsync(securityLog);
    }

    public async Task LogSuspiciousActivityAsync(
        string eventType, 
        Guid? userId, 
        string? ipAddress,
        string? threatIndicators)
    {
        var securityLog = new SecurityAuditLog
        {
            EventType = eventType,
            Severity = "Critical",
            UserId = userId,
            IpAddress = ipAddress,
            Action = "SuspiciousActivity",
            IsSuccess = false,
            ThreatIndicators = threatIndicators,
            RequiresInvestigation = true
        };

        await LogSecurityEventAsync(securityLog);
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? eventType = null,
        Guid? userId = null,
        int limit = 100)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.EventType == eventType);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetPaymentAuditLogsAsync(long paymentTransactionId)
    {
        return await _db.AuditLogs
            .Where(a => a.PaymentTransactionId == paymentTransactionId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<List<SecurityAuditLog>> GetSecurityAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? eventType = null,
        Guid? userId = null,
        int limit = 100)
    {
        var query = _db.SecurityAuditLogs.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.EventType == eventType);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<SecurityAuditLog>> GetSuspiciousActivitiesAsync(bool investigatedOnly = false)
    {
        var query = _db.SecurityAuditLogs
            .Where(a => a.RequiresInvestigation);

        if (investigatedOnly)
            query = query.Where(a => a.IsInvestigated);
        else
            query = query.Where(a => !a.IsInvestigated);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    private void EnrichWithHttpContext(AuditLog auditLog)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        auditLog.IpAddress ??= httpContext.Connection.RemoteIpAddress?.ToString();
        auditLog.UserAgent ??= httpContext.Request.Headers["User-Agent"].ToString();
        auditLog.SessionId ??= httpContext.Session?.Id;
    }

    private void EnrichSecurityLogWithHttpContext(SecurityAuditLog securityLog)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        securityLog.IpAddress ??= httpContext.Connection.RemoteIpAddress?.ToString();
        securityLog.UserAgent ??= httpContext.Request.Headers["User-Agent"].ToString();
        securityLog.SessionId ??= httpContext.Session?.Id;
    }

    private bool ShouldInvestigateFailedLogin(string? failureReason)
    {
        // Investigate if multiple failed attempts or suspicious patterns
        var suspiciousReasons = new[] { "AccountLocked", "InvalidCredentials", "TooManyAttempts" };
        return failureReason != null && suspiciousReasons.Contains(failureReason);
    }

    private string EncryptData(string data)
    {
        if (string.IsNullOrEmpty(_encryptionKey))
        {
            _logger.LogWarning("Encryption key not configured. Data will not be encrypted.");
            return data;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            
            // Write IV first
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(data);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting audit data");
            return data; // Return unencrypted if encryption fails
        }
    }

    private string DecryptData(string encryptedData)
    {
        if (string.IsNullOrEmpty(_encryptionKey))
        {
            return encryptedData;
        }

        try
        {
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);

            // Extract IV
            var iv = new byte[aes.IV.Length];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting audit data");
            return encryptedData;
        }
    }
}
