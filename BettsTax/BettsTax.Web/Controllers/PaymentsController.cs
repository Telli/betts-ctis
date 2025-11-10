using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IAssociatePermissionService _permissionService;
        private readonly IOnBehalfActionService _onBehalfActionService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            IAssociatePermissionService permissionService,
            IOnBehalfActionService onBehalfActionService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _permissionService = permissionService;
            _onBehalfActionService = onBehalfActionService;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated list of payments with optional filtering
        /// </summary>
        [HttpGet]
        [AssociatePermission("Payments", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] PaymentStatus? status = null,
            [FromQuery] int? clientId = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _paymentService.GetPaymentsAsync(page, pageSize, search, status, clientId);

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
                _logger.LogError(ex, "Error retrieving payments");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get specific payment by ID
        /// </summary>
        [HttpGet("{id}")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetPayment(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get payments for a specific client
        /// </summary>
        [HttpGet("client/{clientId}")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetClientPayments(int clientId)
        {
            try
            {
                var payments = await _paymentService.GetClientPaymentsAsync(clientId);
                return Ok(new { success = true, data = payments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for client {ClientId}", clientId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get pending payment approvals
        /// </summary>
        [HttpGet("pending-approvals")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Approve)]
        public async Task<ActionResult<object>> GetPendingApprovals()
        {
            try
            {
                var approvals = await _paymentService.GetPendingApprovalsAsync();
                return Ok(new { success = true, data = approvals });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new payment
        /// </summary>
        [HttpPost]
        [AssociatePermission("Payments", AssociatePermissionLevel.Create)]
        public async Task<ActionResult<object>> CreatePayment([FromBody] CreatePaymentDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var payment = await _paymentService.CreateAsync(createDto, userId);

                // Log on-behalf action if applicable
                var clientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(clientId) && int.TryParse(clientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Create Payment",
                        "Payment",
                        payment.PaymentId,
                        null,
                        new { payment.Amount, payment.TaxType, payment.Method },
                        reason ?? "Payment created on behalf of client"
                    );
                }

                return CreatedAtAction(
                    nameof(GetPayment),
                    new { id = payment.PaymentId },
                    new { success = true, data = payment });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update an existing payment
        /// </summary>
        [HttpPut("{id}")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Update)]
        public async Task<ActionResult<object>> UpdatePayment(int id, [FromBody] CreatePaymentDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Get original payment for audit logging
                var originalPayment = await _paymentService.GetPaymentByIdAsync(id);
                
                var payment = await _paymentService.UpdateAsync(id, updateDto, userId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Update Payment",
                        "Payment",
                        id,
                        originalPayment != null ? new { originalPayment.Amount, originalPayment.TaxType, originalPayment.Method } : null,
                        new { payment.Amount, payment.TaxType, payment.Method },
                        reason ?? "Payment updated on behalf of client"
                    );
                }

                return Ok(new { success = true, data = payment });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation updating payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete a payment (only pending payments)
        /// </summary>
        [HttpDelete("{id}")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Delete)]
        public async Task<ActionResult<object>> DeletePayment(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                
                // Get payment details for audit logging before deletion
                var paymentToDelete = await _paymentService.GetPaymentByIdAsync(id);
                
                var result = await _paymentService.DeleteAsync(id, userId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId) && paymentToDelete != null)
                {
                    await _onBehalfActionService.LogActionAsync(
                        userId,
                        parsedClientId,
                        "Delete Payment",
                        "Payment",
                        id,
                        new { paymentToDelete.Amount, paymentToDelete.TaxType, paymentToDelete.Method },
                        null,
                        reason ?? "Payment deleted on behalf of client"
                    );
                }

                if (!result)
                {
                    return NotFound(new { success = false, message = "Payment not found" });
                }

                return Ok(new { success = true, message = "Payment deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation deleting payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Approve a payment
        /// </summary>
        [HttpPost("{id}/approve")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Approve)]
        public async Task<ActionResult<object>> ApprovePayment(int id, [FromBody] ApprovePaymentDto approveDto)
        {
            try
            {
                var approverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var payment = await _paymentService.ApproveAsync(id, approveDto, approverId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        approverId,
                        parsedClientId,
                        "Approve Payment",
                        "Payment",
                        id,
                        new { Status = "Pending" },
                        new { Status = "Approved" },
                        reason ?? "Payment approved on behalf of client"
                    );
                }

                return Ok(new { success = true, data = payment, message = "Payment approved successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation approving payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Reject a payment
        /// </summary>
        [HttpPost("{id}/reject")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Approve)]
        public async Task<ActionResult<object>> RejectPayment(int id, [FromBody] RejectPaymentDto rejectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var approverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var payment = await _paymentService.RejectAsync(id, rejectDto, approverId);

                // Log on-behalf action if applicable
                var onBehalfClientId = HttpContext.Request.Headers["X-On-Behalf-Of"].FirstOrDefault();
                var reason = HttpContext.Request.Headers["X-Action-Reason"].FirstOrDefault();
                if (!string.IsNullOrEmpty(onBehalfClientId) && int.TryParse(onBehalfClientId, out var parsedClientId))
                {
                    await _onBehalfActionService.LogActionAsync(
                        approverId,
                        parsedClientId,
                        "Reject Payment",
                        "Payment",
                        id,
                        new { Status = "Pending" },
                        new { Status = "Rejected", Reason = rejectDto.RejectionReason },
                        reason ?? "Payment rejected on behalf of client"
                    );
                }

                return Ok(new { success = true, data = payment, message = "Payment rejected" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation rejecting payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload payment evidence (e.g., bank transfer slip)
        /// </summary>
        [HttpPost("{id}/evidence")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Update)]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<object>> UploadPaymentEvidence(int id, [FromForm] UploadPaymentEvidenceDto dto, [FromForm] IFormFile file)
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
                var document = await _paymentService.UploadEvidenceAsync(id, dto, file, userId);

                return Ok(new { success = true, data = document, message = "Payment evidence uploaded" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation uploading payment evidence for payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading payment evidence for payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Reconcile a payment (mark as reconciled and optionally complete)
        /// </summary>
        [HttpPost("{id}/reconcile")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Approve)]
        public async Task<ActionResult<object>> ReconcilePayment(int id, [FromBody] ReconcilePaymentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
                var payment = await _paymentService.ReconcileAsync(id, dto, userId);

                return Ok(new { success = true, data = payment, message = "Payment reconciled" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation reconciling payment {PaymentId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling payment {PaymentId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get payments by tax filing
        /// </summary>
        [HttpGet("tax-filing/{taxFilingId}")]
        [AssociatePermission("Payments", AssociatePermissionLevel.Read)]
        public async Task<ActionResult<object>> GetPaymentsByTaxFiling(int taxFilingId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByTaxFilingAsync(taxFilingId);
                return Ok(new { success = true, data = payments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for tax filing {TaxFilingId}", taxFilingId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
