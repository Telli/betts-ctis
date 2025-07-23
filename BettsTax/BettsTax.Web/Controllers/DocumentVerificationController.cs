using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVerificationController : ControllerBase
    {
        private readonly IDocumentVerificationService _verificationService;
        private readonly ILogger<DocumentVerificationController> _logger;

        public DocumentVerificationController(
            IDocumentVerificationService verificationService,
            ILogger<DocumentVerificationController> logger)
        {
            _verificationService = verificationService;
            _logger = logger;
        }

        [HttpGet("{documentId}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetDocumentVerification(int documentId)
        {
            var result = await _verificationService.GetDocumentVerificationAsync(documentId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> CreateDocumentVerification([FromBody] DocumentVerificationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.CreateDocumentVerificationAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{documentId}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> UpdateDocumentVerification(int documentId, [FromBody] DocumentVerificationUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.UpdateDocumentVerificationAsync(documentId, dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("review")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ReviewDocument([FromBody] DocumentReviewRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.ReviewDocumentAsync(dto);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpPost("bulk-review")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> BulkReviewDocuments([FromBody] BulkDocumentReviewDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.BulkReviewDocumentsAsync(dto);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpGet("requirements")]
        [Authorize]
        public async Task<IActionResult> GetDocumentRequirements([FromQuery] TaxType? taxType, [FromQuery] TaxpayerCategory? category)
        {
            var result = await _verificationService.GetDocumentRequirementsAsync(taxType, category);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("requirements/{requirementId}")]
        [Authorize]
        public async Task<IActionResult> GetDocumentRequirement(int requirementId)
        {
            var result = await _verificationService.GetDocumentRequirementAsync(requirementId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("requirements")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateDocumentRequirement([FromBody] DocumentRequirementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.CreateDocumentRequirementAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPut("requirements/{requirementId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateDocumentRequirement(int requirementId, [FromBody] DocumentRequirementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.UpdateDocumentRequirementAsync(requirementId, dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpDelete("requirements/{requirementId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteDocumentRequirement(int requirementId)
        {
            var result = await _verificationService.DeleteDocumentRequirementAsync(requirementId);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}/filing/{taxFilingId}/requirements")]
        [Authorize]
        public async Task<IActionResult> GetClientDocumentRequirements(int clientId, int taxFilingId)
        {
            var result = await _verificationService.GetClientDocumentRequirementsAsync(clientId, taxFilingId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("client/{clientId}/filing/{taxFilingId}/generate")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GenerateClientDocumentRequirements(int clientId, int taxFilingId)
        {
            var result = await _verificationService.GenerateClientDocumentRequirementsAsync(clientId, taxFilingId);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpPost("client/{clientId}/filing/{taxFilingId}/request")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RequestDocumentsFromClient(int clientId, int taxFilingId, [FromBody] List<int> requirementIds)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _verificationService.RequestDocumentsFromClientAsync(clientId, taxFilingId, requirementIds);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}/filing/{taxFilingId}/summary")]
        [Authorize]
        public async Task<IActionResult> GetDocumentVerificationSummary(int clientId, int taxFilingId)
        {
            var result = await _verificationService.GetDocumentVerificationSummaryAsync(clientId, taxFilingId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("{documentId}/history")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetDocumentVerificationHistory(int documentId)
        {
            var result = await _verificationService.GetDocumentVerificationHistoryAsync(documentId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("pending")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetPendingDocumentReviews([FromQuery] string? associateId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _verificationService.GetPendingDocumentReviewsAsync(associateId, page, pageSize);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpGet("status/{status}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetDocumentsByStatus(DocumentVerificationStatus status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _verificationService.GetDocumentsByStatusAsync(status, page, pageSize);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("{documentId}/validate/{requirementId}")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ValidateDocumentForRequirement(int documentId, int requirementId)
        {
            var result = await _verificationService.ValidateDocumentForRequirementAsync(documentId, requirementId);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }

        [HttpGet("client/{clientId}/filing/{taxFilingId}/check-verified")]
        [Authorize]
        public async Task<IActionResult> CheckAllRequiredDocumentsVerified(int clientId, int taxFilingId)
        {
            var result = await _verificationService.CheckAllRequiredDocumentsVerifiedAsync(clientId, taxFilingId);
            return result.IsSuccess ? Ok(new { allVerified = result.Value }) : BadRequest(result.ErrorMessage);
        }
    }
}