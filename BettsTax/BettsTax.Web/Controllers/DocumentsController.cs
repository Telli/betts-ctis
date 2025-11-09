using System.Linq;
using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Provides document listings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDemoDataService demoDataService,
        ILogger<DocumentsController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves supporting documents. Optionally filter by client.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments([FromQuery] int? clientId = null)
    {
        try
        {
            var documents = await _demoDataService.GetDocumentsAsync();
            var filtered = clientId.HasValue
                ? documents.Where(doc => doc.ClientId == clientId).ToList()
                : documents;

            return Ok(new { success = true, data = filtered });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents");
            return StatusCode(500, new { success = false, message = "Failed to load documents" });
        }
    }
}
