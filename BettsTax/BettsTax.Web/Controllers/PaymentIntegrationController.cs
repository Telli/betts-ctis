using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BettsTax.Core.Services;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Web.Filters;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(AuditActionFilter))]
    public class PaymentIntegrationController : ControllerBase
    {
        private readonly IPaymentIntegrationService _paymentIntegrationService;
        private readonly ILogger<PaymentIntegrationController> _logger;

        public PaymentIntegrationController(
            IPaymentIntegrationService paymentIntegrationService,
            ILogger<PaymentIntegrationController> logger)
        {
            _paymentIntegrationService = paymentIntegrationService;
            _logger = logger;
        }

        /// <summary>
        /// Get available payment methods for an amount
        /// </summary>
        [HttpGet("methods")]
        [Authorize]
        public async Task<IActionResult> GetPaymentMethods([FromQuery] decimal amount, [FromQuery] string countryCode = "SL")
        {
            var result = await _paymentIntegrationService.GetAvailablePaymentMethodsAsync(amount, countryCode);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate a payment transaction
        /// </summary>
        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.InitiatePaymentAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate mobile money payment
        /// </summary>
        [HttpPost("mobile-money/initiate")]
        [Authorize]
        public async Task<IActionResult> InitiateMobileMoneyPayment([FromBody] InitiateMobileMoneyPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.InitiateMobileMoneyPaymentAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate bank transfer payment
        /// </summary>
        [HttpPost("bank-transfer/initiate")]
        [Authorize]
        public async Task<IActionResult> InitiateBankTransfer([FromBody] InitiateBankTransferDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.InitiateBankTransferAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get payment transaction by ID
        /// </summary>
        [HttpGet("transactions/{transactionId:int}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentTransaction(int transactionId)
        {
            var result = await _paymentIntegrationService.GetPaymentTransactionAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get payment transaction by reference
        /// </summary>
        [HttpGet("transactions/reference/{reference}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentTransactionByReference(string reference)
        {
            var result = await _paymentIntegrationService.GetPaymentTransactionByReferenceAsync(reference);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Check payment transaction status
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/status")]
        [Authorize]
        public async Task<IActionResult> CheckPaymentStatus(int transactionId)
        {
            var result = await _paymentIntegrationService.CheckPaymentStatusAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get payment transactions with filtering
        /// </summary>
        [HttpGet("transactions")]
        [Authorize]
        public async Task<IActionResult> GetPaymentTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] PaymentTransactionStatus? status = null,
            [FromQuery] PaymentProvider? provider = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _paymentIntegrationService.GetPaymentTransactionsAsync(
                page, pageSize, status, provider, fromDate, toDate);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get client payment transactions
        /// </summary>
        [HttpGet("clients/{clientId:int}/transactions")]
        [Authorize]
        public async Task<IActionResult> GetClientPaymentTransactions(int clientId)
        {
            var result = await _paymentIntegrationService.GetClientPaymentTransactionsAsync(clientId);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get payment transaction summary/statistics
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetPaymentTransactionSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _paymentIntegrationService.GetPaymentTransactionSummaryAsync(fromDate, toDate);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Validate phone number for mobile money provider
        /// </summary>
        [HttpPost("validate-phone")]
        [Authorize]
        public async Task<IActionResult> ValidatePhoneNumber([FromBody] ValidatePhoneNumberDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.ValidatePhoneNumberAsync(dto.PhoneNumber, dto.Provider);
            
            return Ok(new { isValid = result.IsSuccess && result.Value, message = result.ErrorMessage });
        }

        /// <summary>
        /// Calculate transaction fee for a payment method
        /// </summary>
        [HttpPost("calculate-fee")]
        [Authorize]
        public async Task<IActionResult> CalculateTransactionFee([FromBody] CalculateFeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.CalculateTransactionFeeAsync(dto.Amount, dto.Provider);
            
            if (result.IsSuccess)
                return Ok(new { fee = result.Value, currency = "SLE" });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get mobile money balance (Admin only)
        /// </summary>
        [HttpGet("mobile-money/{provider}/balance")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetMobileMoneyBalance(PaymentProvider provider)
        {
            var result = await _paymentIntegrationService.GetMobileMoneyBalanceAsync(provider);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Test payment provider connection (Admin only)
        /// </summary>
        [HttpPost("providers/{provider}/test")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> TestPaymentProvider(PaymentProvider provider)
        {
            var result = await _paymentIntegrationService.TestPaymentProviderAsync(provider);
            
            return Ok(new { success = result.IsSuccess && result.Value, message = result.ErrorMessage });
        }

        /// <summary>
        /// Retry failed payment (Admin only)
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/retry")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RetryFailedPayment(int transactionId, [FromBody] RetryPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.RetryFailedPaymentAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Refund payment (Admin only)
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/refund")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RefundPayment(int transactionId, [FromBody] RefundPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _paymentIntegrationService.RefundPaymentAsync(
                transactionId, dto.PartialAmount, dto.Reason);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Process pending payments (Admin background task)
        /// </summary>
        [HttpPost("process-pending")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> ProcessPendingPayments()
        {
            var result = await _paymentIntegrationService.ProcessPendingPaymentsAsync();
            
            if (result.IsSuccess)
                return Ok(new { processedCount = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        #region Webhook Endpoints

        /// <summary>
        /// Orange Money webhook endpoint
        /// </summary>
        [HttpPost("webhook/orange-money")]
        [AllowAnonymous]
        public async Task<IActionResult> OrangeMoneyWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                
                var signature = Request.Headers["X-Orange-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                    return BadRequest("Missing signature");

                var result = await _paymentIntegrationService.ProcessWebhookAsync(
                    PaymentProvider.OrangeMoney, payload, signature);

                if (result.IsSuccess)
                    return Ok();
                
                _logger.LogWarning("Orange Money webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Orange Money webhook");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Africell Money webhook endpoint
        /// </summary>
        [HttpPost("webhook/africell-money")]
        [AllowAnonymous]
        public async Task<IActionResult> AfricellMoneyWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                
                var signature = Request.Headers["X-Africell-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                    return BadRequest("Missing signature");

                var result = await _paymentIntegrationService.ProcessWebhookAsync(
                    PaymentProvider.AfricellMoney, payload, signature);

                if (result.IsSuccess)
                    return Ok();
                
                _logger.LogWarning("Africell Money webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Africell Money webhook");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Salone Payment Switch webhook endpoint (pain.002 status updates)
        /// </summary>
        [HttpPost("webhook/salone-switch")]
        [AllowAnonymous]
        public async Task<IActionResult> SaloneSwitchWebhook([FromServices] PaymentWebhookProcessor processor)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();

                // Collect headers into dictionary for hashing / signature validation
                var headers = Request.Headers.ToDictionary(h => h.Key, h => (string?)h.Value.FirstOrDefault());

                var ok = await processor.ProcessAsync(PaymentProvider.SalonePaymentSwitch.ToString(), payload, headers, HttpContext.RequestAborted);
                if (ok)
                    return Ok();
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Salone Payment Switch webhook");
                return StatusCode(500);
            }
        }

        #endregion
    }

    // Additional DTOs for controller endpoints
    public class ValidatePhoneNumberDto
    {
        public required string PhoneNumber { get; set; }
        public PaymentProvider Provider { get; set; }
    }

    public class CalculateFeeDto
    {
        public decimal Amount { get; set; }
        public PaymentProvider Provider { get; set; }
    }
}