using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<ReconciliationController> _logger;

    public ReconciliationController(
        IReconciliationService reconciliationService,
        IClaimsService claimsService,
        ILogger<ReconciliationController> logger)
    {
        _reconciliationService = reconciliationService;
        _claimsService = claimsService;
        _logger = logger;
    }

    [HttpPost("run-daily")]
    public async Task<IActionResult> RunDailyReconciliation([FromQuery] DateTime? date = null)
    {
        try
        {
            var reconciliationDate = date ?? DateTime.UtcNow.Date.AddDays(-1);
            var report = await _reconciliationService.RunDailyReconciliationAsync(reconciliationDate);

            return Ok(new
            {
                success = true,
                report = new
                {
                    report.ReconciliationReportId,
                    report.ReportDate,
                    report.PeriodStart,
                    report.PeriodEnd,
                    report.TotalInternalTransactions,
                    report.TotalProviderTransactions,
                    report.MatchedTransactions,
                    report.MismatchedTransactions,
                    report.InternalOnlyTransactions,
                    report.ProviderOnlyTransactions,
                    report.TotalInternalAmount,
                    report.TotalProviderAmount,
                    report.TotalMatchedAmount,
                    report.TotalMismatchedAmount,
                    report.Status,
                    report.CompletedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running daily reconciliation");
            return StatusCode(500, new { success = false, message = "Error running reconciliation", error = ex.Message });
        }
    }

    [HttpPost("run-period")]
    public async Task<IActionResult> RunPeriodReconciliation([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _reconciliationService.RunReconciliationForPeriodAsync(startDate, endDate);

            return Ok(new
            {
                success = true,
                report = new
                {
                    report.ReconciliationReportId,
                    report.ReportDate,
                    report.PeriodStart,
                    report.PeriodEnd,
                    report.TotalInternalTransactions,
                    report.MatchedTransactions,
                    report.MismatchedTransactions,
                    report.Status
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running period reconciliation");
            return StatusCode(500, new { success = false, message = "Error running reconciliation", error = ex.Message });
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetRecentReports([FromQuery] int count = 10)
    {
        try
        {
            var reports = await _reconciliationService.GetRecentReportsAsync(count);
            return Ok(new { success = true, reports });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reconciliation reports");
            return StatusCode(500, new { success = false, message = "Error fetching reports", error = ex.Message });
        }
    }

    [HttpGet("reports/{reportId}")]
    public async Task<IActionResult> GetReportById(long reportId)
    {
        try
        {
            var report = await _reconciliationService.GetReportByIdAsync(reportId);
            
            if (report == null)
                return NotFound(new { success = false, message = "Report not found" });

            return Ok(new { success = true, report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reconciliation report");
            return StatusCode(500, new { success = false, message = "Error fetching report", error = ex.Message });
        }
    }

    [HttpGet("mismatches/unresolved")]
    public async Task<IActionResult> GetUnresolvedMismatches()
    {
        try
        {
            var mismatches = await _reconciliationService.GetUnresolvedMismatchesAsync();
            return Ok(new { success = true, mismatches, count = mismatches.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unresolved mismatches");
            return StatusCode(500, new { success = false, message = "Error fetching mismatches", error = ex.Message });
        }
    }

    [HttpPost("mismatches/{mismatchId}/resolve")]
    public async Task<IActionResult> ResolveMismatch(long mismatchId, [FromBody] ResolveMismatchRequest request)
    {
        try
        {
            var userIdString = await _claimsService.GetUserIdAsync(User);
            var userId = Guid.Parse(userIdString);
            var result = await _reconciliationService.ResolveMismatchAsync(mismatchId, request.ResolutionNotes, userId);

            if (!result)
                return NotFound(new { success = false, message = "Mismatch not found" });

            return Ok(new { success = true, message = "Mismatch resolved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving mismatch");
            return StatusCode(500, new { success = false, message = "Error resolving mismatch", error = ex.Message });
        }
    }

    public class ResolveMismatchRequest
    {
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
