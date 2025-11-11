using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Document;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Documents Controller - Manages document uploads and retrieval
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        Services.IAuthorizationService authorizationService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all documents with optional filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? year = null,
        [FromQuery] int? clientId = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Security check
            if (!_authorizationService.CanAccessClientData(User, clientId))
            {
                _logger.LogWarning("Unauthorized access attempt by user {UserId} to client {ClientId} documents", userId, clientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            // Auto-filter for client users
            var effectiveClientId = clientId;
            if (!effectiveClientId.HasValue && !_authorizationService.IsStaffOrAdmin(User))
            {
                effectiveClientId = _authorizationService.GetUserClientId(User);
            }

            var documents = await _documentService.GetDocumentsAsync(search, type, year, effectiveClientId);

            _logger.LogInformation("User {UserId} retrieved {Count} documents", userId, documents.Count);

            return Ok(new { success = true, data = documents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving documents" });
        }
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB limit
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] UploadDocumentDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

            // Security check - verify user can upload for this client
            if (!_authorizationService.CanAccessClientData(User, dto.ClientId))
            {
                _logger.LogWarning("Unauthorized document upload attempt by user {UserId} for client {ClientId}", userId, dto.ClientId);
                return StatusCode(403, new { success = false, message = "Access denied" });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            // Validate file type (basic check)
            var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls", ".doc", ".docx", ".csv" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { success = false, message = $"File type {extension} not allowed" });
            }

            var newDocument = await _documentService.UploadDocumentAsync(file, dto, userName);

            _logger.LogInformation("User {UserId} uploaded document {DocumentId}: {FileName}", userId, newDocument.Id, file.FileName);

            return StatusCode(201, new { success = true, data = newDocument });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { success = false, message = "An error occurred while uploading document" });
        }
    }

    /// <summary>
    /// Download a document by ID
    /// </summary>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get document metadata to check permissions
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound(new { success = false, message = "Document not found" });
            }

            // Security check - in production, extract clientId from document and verify access
            // For now, allow access to all authenticated users
            // if (!_authorizationService.CanAccessClientData(User, document.ClientId))
            // {
            //     return StatusCode(403, new { success = false, message = "Access denied" });
            // }

            var fileContent = await _documentService.DownloadDocumentAsync(id);
            if (fileContent == null)
            {
                return NotFound(new { success = false, message = "Document file not found" });
            }

            _logger.LogInformation("User {UserId} downloaded document {DocumentId}", userId, id);

            // Determine content type based on file extension
            var contentType = "application/octet-stream";
            if (document.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                contentType = "application/pdf";
            else if (document.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            else if (document.Name.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                contentType = "application/vnd.ms-excel";

            return File(fileContent, contentType, document.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while downloading document" });
        }
    }
}
