using BettsTax.Core.DTOs.Analytics;
using BettsTax.Core.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvancedAnalyticsController : ControllerBase
{
    private readonly IAdvancedAnalyticsService _analyticsService;
    private readonly ILogger<AdvancedAnalyticsController> _logger;

    public AdvancedAnalyticsController(IAdvancedAnalyticsService analyticsService, ILogger<AdvancedAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpPost("dashboard")]
    public async Task<ActionResult<AnalyticsDashboardResponse>> GetDashboard([FromBody] AnalyticsDashboardRequest request)
    {
        try
        {
            var dashboard = await _analyticsService.GenerateDashboardAsync(request);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard: {@Request}", request);
            return BadRequest(new AnalyticsDashboardResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpGet("dashboard/{dashboardType}")]
    public async Task<ActionResult<AnalyticsDashboardResponse>> GetDashboardByType(
        string dashboardType,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? clientId = null)
    {
        try
        {
            var request = new AnalyticsDashboardRequest
            {
                DashboardType = dashboardType,
                StartDate = startDate,
                EndDate = endDate,
                ClientId = clientId
            };

            var dashboard = await _analyticsService.GenerateDashboardAsync(request);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard by type: {DashboardType}", dashboardType);
            return BadRequest(new AnalyticsDashboardResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message
            });
        }
    }

    [HttpGet("metrics/real-time")]
    public async Task<ActionResult<List<AnalyticsWidget>>> GetRealTimeMetrics()
    {
        try
        {
            var metrics = await _analyticsService.GetRealTimeMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time metrics");
            return BadRequest($"Error getting real-time metrics: {ex.Message}");
        }
    }

    [HttpGet("metrics/available")]
    public async Task<ActionResult<Dictionary<string, object>>> GetAvailableMetrics()
    {
        try
        {
            var metrics = await _analyticsService.GetAvailableMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available metrics");
            return BadRequest($"Error getting available metrics: {ex.Message}");
        }
    }

    [HttpGet("dashboard/types")]
    public ActionResult<List<string>> GetDashboardTypes()
    {
        try
        {
            var types = new List<string>
            {
                DashboardTypes.TaxCompliance,
                DashboardTypes.Revenue,
                DashboardTypes.ClientPerformance,
                DashboardTypes.PaymentAnalytics,
                DashboardTypes.OperationalEfficiency,
                DashboardTypes.DocumentManagement,
                DashboardTypes.UserActivity,
                DashboardTypes.SystemHealth
            };
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard types");
            return BadRequest($"Error getting dashboard types: {ex.Message}");
        }
    }

    [HttpGet("health")]
    public ActionResult<object> GetHealth()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Advanced Analytics"
        });
    }
}