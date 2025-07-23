using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IAssociatePermissionService _permissionService;
        private readonly IOnBehalfActionService _onBehalfActionService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            IAssociatePermissionService permissionService,
            IOnBehalfActionService onBehalfActionService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _permissionService = permissionService;
            _onBehalfActionService = onBehalfActionService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of documents with optional filtering
        /// </summary>
        [HttpGet]
        [AssociatePermission("Documents", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetDocuments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] DocumentCategory? category = null,
            [FromQuery] int? clientId = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _documentService.GetDocumentsAsync(page, pageSize, search, category, clientId);

                return Ok(new
                {
                    success = true,
                    data = result.Items,
                    pagination = new
                    {
                        currentPage = result.Page,
                        pageSize = result.PageSize,
                        totalCount = result.TotalCount,
                        totalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific document by ID
        /// </summary>
        [HttpGet("{id}")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetDocument(int id)
        {
            try
            {
                var document = await _documentService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                return Ok(new { success = true, data = document });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get documents for a specific client
        /// </summary>
        [HttpGet("client/{clientId}")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetClientDocuments(int clientId)
        {
            try
            {
                var documents = await _documentService.GetClientDocumentsAsync(clientId);
                return Ok(new { success = true, data = documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get documents for a specific tax filing
        /// </summary>
        [HttpGet("tax-filing/{taxFilingId}")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetTaxFilingDocuments(int taxFilingId)
        {
            try
            {
                var documents = await _documentService.GetTaxFilingDocumentsAsync(taxFilingId);
                return Ok(new { success = true, data = documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for tax filing {TaxFilingId}", taxFilingId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload a new document
        /// </summary>
        [HttpPost("upload")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Create)]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<object>> UploadDocument([FromForm] UploadDocumentDto uploadDto, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var document = await _documentService.UploadAsync(uploadDto, file, userId);

                // Log on-behalf action if applicable
                var clientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(clientId) && int.TryParse(clientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Upload Document",
                        "Document",
                        document.DocumentId,
                        null,
                        new { document.OriginalFileName, document.Category },
                        reason ?? "Document uploaded on behalf of client"
                    );
                }

                return CreatedAtAction(
                    nameof(GetDocument),
                    new { id = document.DocumentId },
                    new { success = true, data = document });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation uploading document");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update an existing document
        /// </summary>
        [HttpPut("{id}")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Update)]
        public async Task<ActionResult<object>> UpdateDocument(int id, [FromBody] UpdateDocumentDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Get original document for audit logging
                var originalDocument = await _documentService.GetDocumentByIdAsync(id);
                
                var document = await _documentService.UpdateAsync(id, updateDto, userId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Update Document",
                        "Document",
                        id,
                        originalDocument != null ? new { originalDocument.OriginalFileName, originalDocument.Category } : null,
                        new { document.OriginalFileName, document.Category },
                        reason ?? "Document updated on behalf of client"
                    );
                }

                return Ok(new { success = true, data = document });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation updating document {DocumentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a document (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Delete)]
        public async Task<ActionResult<object>> DeleteDocument(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Get document details for audit logging before deletion
                var documentToDelete = await _documentService.GetDocumentByIdAsync(id);
                
                var result = await _documentService.DeleteAsync(id, userId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId) && documentToDelete != null)
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Delete Document",
                        "Document",
                        id,
                        new { documentToDelete.OriginalFileName, documentToDelete.Category },
                        null,
                        reason ?? "Document deleted on behalf of client"
                    );
                }

                if (!result)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                return Ok(new { success = true, message = "Document deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation deleting document {DocumentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Download a document file
        /// </summary>
        [HttpGet("{id}/download")]
        [AssociatePermission("Documents", AssociatePermissionLevel.Read)]
        public async Task<ActionResult> DownloadDocument(int id)
        {
            try
            {
                var fileInfo = await _documentService.GetFileInfoAsync(id);
                if (fileInfo == null)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fileInfo.Value.Path);
                
                // Log document access for audit trail
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Download Document",
                        "Document",
                        id,
                        null,
                        null,
                        "Document downloaded on behalf of client"
                    );
                }
                
                // Clean up temp file
                try
                {
                    System.IO.File.Delete(fileInfo.Value.Path);
                }
                catch { /* Ignore cleanup errors */ }

                return File(fileBytes, fileInfo.Value.ContentType, fileInfo.Value.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get total storage used
        /// </summary>
        [HttpGet("storage-usage")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<ActionResult<object>> GetStorageUsage([FromQuery] int? clientId = null)
        {
            try
            {
                var totalBytes = await _documentService.GetTotalStorageUsedAsync(clientId);
                var totalMB = totalBytes / (1024.0 * 1024.0);
                var totalGB = totalMB / 1024.0;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalBytes,
                        totalMB = Math.Round(totalMB, 2),
                        totalGB = Math.Round(totalGB, 2),
                        clientId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving storage usage");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
