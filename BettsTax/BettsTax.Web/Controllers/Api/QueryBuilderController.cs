using BettsTax.Core.DTOs.QueryBuilder;
using BettsTax.Core.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QueryBuilderController : ControllerBase
{
    private readonly IAdvancedQueryBuilderService _queryBuilderService;
    private readonly ILogger<QueryBuilderController> _logger;

    public QueryBuilderController(IAdvancedQueryBuilderService queryBuilderService, ILogger<QueryBuilderController> logger)
    {
        _queryBuilderService = queryBuilderService;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<QueryBuilderResponse>> ExecuteQuery([FromBody] QueryBuilderRequest request)
    {
        try
        {
            var result = await _queryBuilderService.ExecuteQueryAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {@Request}", request);
            return BadRequest(new QueryBuilderResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }

    [HttpGet("datasources")]
    public async Task<ActionResult<List<DataSourceInfo>>> GetDataSources()
    {
        try
        {
            var dataSources = await _queryBuilderService.GetAvailableDataSourcesAsync();
            return Ok(dataSources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data sources");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("datasources/{dataSourceName}")]
    public async Task<ActionResult<DataSourceInfo>> GetDataSourceInfo(string dataSourceName)
    {
        try
        {
            var dataSourceInfo = await _queryBuilderService.GetDataSourceInfoAsync(dataSourceName);
            return Ok(dataSourceInfo);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data source info for {DataSource}", dataSourceName);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("saved-queries")]
    public async Task<ActionResult<List<SavedQuery>>> GetSavedQueries([FromQuery] bool publicOnly = false)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var savedQueries = await _queryBuilderService.GetSavedQueriesAsync(userId, publicOnly);
            return Ok(savedQueries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saved queries");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("save-query")]
    public async Task<ActionResult<SavedQuery>> SaveQuery([FromBody] SaveQueryRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var savedQuery = await _queryBuilderService.SaveQueryAsync(
                request.Query, 
                request.Name, 
                request.Description, 
                userId, 
                request.IsPublic);
            return Ok(savedQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving query: {@Request}", request);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("saved-queries/{queryId}")]
    public async Task<ActionResult> DeleteSavedQuery(int queryId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _queryBuilderService.DeleteSavedQueryAsync(queryId, userId);
            
            if (!result)
                return NotFound(new { error = "Query not found or you don't have permission to delete it" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved query {QueryId}", queryId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("execute-saved/{queryId}")]
    public async Task<ActionResult<QueryBuilderResponse>> ExecuteSavedQuery(int queryId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _queryBuilderService.ExecuteSavedQueryAsync(queryId, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing saved query {QueryId}", queryId);
            return BadRequest(new QueryBuilderResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }

    [HttpPost("validate")]
    public async Task<ActionResult> ValidateQuery([FromBody] QueryBuilderRequest request)
    {
        try
        {
            // Basic validation logic
            if (string.IsNullOrEmpty(request.DataSource))
                return BadRequest(new { error = "Data source is required" });

            if (!request.SelectFields.Any())
                return BadRequest(new { error = "At least one field must be selected" });

            // Check if data source exists
            var dataSources = await _queryBuilderService.GetAvailableDataSourcesAsync();
            if (!dataSources.Any(ds => ds.Name.Equals(request.DataSource, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { error = $"Data source '{request.DataSource}' not found" });

            return Ok(new { valid = true, message = "Query is valid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating query: {@Request}", request);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("field-suggestions/{dataSource}")]
    public async Task<ActionResult<List<string>>> GetFieldSuggestions(string dataSource, [FromQuery] string? fieldName = null)
    {
        try
        {
            var dataSourceInfo = await _queryBuilderService.GetDataSourceInfoAsync(dataSource);
            var fields = dataSourceInfo.Fields.Select(f => f.Name).ToList();

            if (!string.IsNullOrEmpty(fieldName))
            {
                fields = fields.Where(f => f.Contains(fieldName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(fields);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field suggestions for {DataSource}", dataSource);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("preview")]
    public async Task<ActionResult<QueryBuilderResponse>> PreviewQuery([FromBody] QueryBuilderRequest request)
    {
        try
        {
            // Limit preview to first 100 rows
            request.Take = Math.Min(request.Take ?? 100, 100);
            
            var result = await _queryBuilderService.ExecuteQueryAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing query: {@Request}", request);
            return BadRequest(new QueryBuilderResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
    }
}

public class SaveQueryRequest
{
    public QueryBuilderRequest Query { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}