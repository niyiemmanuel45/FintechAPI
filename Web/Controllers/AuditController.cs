using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditLogService auditLogService,
        IClaimsService claimsService,
        ILogger<AuditController> logger)
    {
        _auditLogService = auditLogService;
        _claimsService = claimsService;
        _logger = logger;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var logs = await _auditLogService.GetAuditLogsAsync(startDate, endDate, eventType, userId, limit);
            return Ok(new { success = true, logs, count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs");
            return StatusCode(500, new { success = false, message = "Error fetching audit logs", error = ex.Message });
        }
    }

    [HttpGet("logs/payment/{paymentTransactionId}")]
    public async Task<IActionResult> GetPaymentAuditLogs(long paymentTransactionId)
    {
        try
        {
            var logs = await _auditLogService.GetPaymentAuditLogsAsync(paymentTransactionId);
            return Ok(new { success = true, logs, count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment audit logs");
            return StatusCode(500, new { success = false, message = "Error fetching payment audit logs", error = ex.Message });
        }
    }

    [HttpGet("security-logs")]
    public async Task<IActionResult> GetSecurityAuditLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var logs = await _auditLogService.GetSecurityAuditLogsAsync(startDate, endDate, eventType, userId, limit);
            return Ok(new { success = true, logs, count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching security audit logs");
            return StatusCode(500, new { success = false, message = "Error fetching security audit logs", error = ex.Message });
        }
    }

    [HttpGet("security-logs/suspicious")]
    public async Task<IActionResult> GetSuspiciousActivities([FromQuery] bool investigatedOnly = false)
    {
        try
        {
            var activities = await _auditLogService.GetSuspiciousActivitiesAsync(investigatedOnly);
            return Ok(new { success = true, activities, count = activities.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching suspicious activities");
            return StatusCode(500, new { success = false, message = "Error fetching suspicious activities", error = ex.Message });
        }
    }

    [HttpGet("logs/my-activity")]
    public async Task<IActionResult> GetMyActivity([FromQuery] int limit = 50)
    {
        try
        {
            var userIdString = await _claimsService.GetUserIdAsync(User);
            var userId = Guid.Parse(userIdString);
            var logs = await _auditLogService.GetAuditLogsAsync(userId: userId, limit: limit);
            return Ok(new { success = true, logs, count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user activity");
            return StatusCode(500, new { success = false, message = "Error fetching activity", error = ex.Message });
        }
    }
}
