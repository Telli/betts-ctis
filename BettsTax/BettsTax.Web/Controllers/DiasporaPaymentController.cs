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
    public class DiasporaPaymentController : ControllerBase
    {
        private readonly IDiasporaPaymentService _diasporaPaymentService;
        private readonly ILogger<DiasporaPaymentController> _logger;

        public DiasporaPaymentController(
            IDiasporaPaymentService diasporaPaymentService,
            ILogger<DiasporaPaymentController> logger)
        {
            _diasporaPaymentService = diasporaPaymentService;
            _logger = logger;
        }

        /// <summary>
        /// Get supported countries for diaspora payments
        /// </summary>
        [HttpGet("countries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDiasporaCountries()
        {
            var result = await _diasporaPaymentService.GetDiasporaCountriesAsync();
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get country-specific payment information
        /// </summary>
        [HttpGet("countries/{countryCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCountryInfo(string countryCode)
        {
            var result = await _diasporaPaymentService.GetCountryInfoAsync(countryCode);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get available payment methods for a specific country and currency
        /// </summary>
        [HttpGet("countries/{countryCode}/payment-methods")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentMethodsForCountry(
            string countryCode,
            [FromQuery] string currency = "USD")
        {
            var result = await _diasporaPaymentService.GetPaymentMethodsForCountryAsync(countryCode, currency);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get currency conversion rates
        /// </summary>
        [HttpPost("currency/convert")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCurrencyConversion([FromBody] CurrencyConversionRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.GetCurrencyConversionAsync(
                request.Amount, request.FromCurrency, request.ToCurrency);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get exchange rate quote with fees
        /// </summary>
        [HttpPost("currency/quote")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExchangeRateQuote([FromBody] CurrencyConversionRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.GetExchangeRateQuoteAsync(
                request.Amount, request.FromCurrency, request.ToCurrency);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get supported currencies
        /// </summary>
        [HttpGet("currencies")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSupportedCurrencies()
        {
            var result = await _diasporaPaymentService.GetSupportedCurrenciesAsync();
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate diaspora payment (auto-selects best provider)
        /// </summary>
        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> InitiateDiasporaPayment([FromBody] DiasporaPaymentInitiateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.InitiateDiasporaPaymentAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate PayPal payment specifically
        /// </summary>
        [HttpPost("paypal/initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayPalPayment([FromBody] DiasporaPaymentInitiateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Provider = PaymentProvider.PayPal;
            var result = await _diasporaPaymentService.InitiatePayPalPaymentAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Initiate Stripe payment specifically
        /// </summary>
        [HttpPost("stripe/initiate")]
        [Authorize]
        public async Task<IActionResult> InitiateStripePayment([FromBody] DiasporaPaymentInitiateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.Provider = PaymentProvider.Stripe;
            var result = await _diasporaPaymentService.InitiateStripePaymentAsync(dto);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get diaspora payment status
        /// </summary>
        [HttpGet("transactions/{transactionId:int}/status")]
        [Authorize]
        public async Task<IActionResult> GetDiasporaPaymentStatus(int transactionId)
        {
            var result = await _diasporaPaymentService.GetDiasporaPaymentStatusAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get diaspora payment status by reference
        /// </summary>
        [HttpGet("transactions/reference/{reference}/status")]
        [Authorize]
        public async Task<IActionResult> GetDiasporaPaymentStatusByReference(string reference)
        {
            var result = await _diasporaPaymentService.GetDiasporaPaymentStatusByReferenceAsync(reference);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Handle PayPal return from payment approval
        /// </summary>
        [HttpGet("paypal/return")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePayPalReturn(
            [FromQuery] string orderId,
            [FromQuery] string PayerID)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(PayerID))
                    return BadRequest("Missing required parameters");

                var result = await _diasporaPaymentService.HandlePayPalReturnAsync(orderId, PayerID);
                
                if (result.IsSuccess)
                {
                    // Redirect to success page with transaction details
                    return Redirect($"/payment/success?transactionId={result.Value.PaymentTransactionId}");
                }
                
                // Redirect to failure page
                return Redirect($"/payment/failed?error={Uri.EscapeDataString(result.ErrorMessage)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal return for order {OrderId}", orderId);
                return Redirect("/payment/failed?error=processing_error");
            }
        }

        /// <summary>
        /// Handle PayPal cancellation
        /// </summary>
        [HttpGet("paypal/cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePayPalCancel([FromQuery] string token)
        {
            _logger.LogInformation("PayPal payment cancelled for token {Token}", token);
            return Redirect("/payment/cancelled");
        }

        /// <summary>
        /// Handle Stripe payment confirmation
        /// </summary>
        [HttpPost("stripe/confirm")]
        [Authorize]
        public async Task<IActionResult> HandleStripeConfirmation([FromBody] StripeConfirmationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.HandleStripeConfirmationAsync(dto.PaymentIntentId);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get diaspora payment summary and analytics
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> GetDiasporaPaymentSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _diasporaPaymentService.GetDiasporaPaymentSummaryAsync(fromDate, toDate);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get diaspora transactions with filtering
        /// </summary>
        [HttpGet("transactions")]
        [Authorize]
        public async Task<IActionResult> GetDiasporaTransactions(
            [FromQuery] string? countryCode = null,
            [FromQuery] string? currency = null,
            [FromQuery] PaymentProvider? provider = null)
        {
            var result = await _diasporaPaymentService.GetDiasporaTransactionsAsync(countryCode, currency, provider);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Cancel diaspora payment
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelDiasporaPayment(
            int transactionId,
            [FromBody] CancelPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.CancelDiasporaPaymentAsync(transactionId, dto.Reason);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Refund diaspora payment (Admin only)
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/refund")]
        [Authorize(Policy = "AdminOrAssociate")]
        public async Task<IActionResult> RefundDiasporaPayment(
            int transactionId,
            [FromBody] RefundDiasporaPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diasporaPaymentService.RefundDiasporaPaymentAsync(
                transactionId, dto.PartialAmount, dto.Reason);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Retry failed diaspora payment
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/retry")]
        [Authorize]
        public async Task<IActionResult> RetryFailedDiasporaPayment(int transactionId)
        {
            var result = await _diasporaPaymentService.RetryFailedDiasporaPaymentAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Generate payment receipt
        /// </summary>
        [HttpGet("transactions/{transactionId:int}/receipt")]
        [Authorize]
        public async Task<IActionResult> GeneratePaymentReceipt(
            int transactionId,
            [FromQuery] string format = "PDF")
        {
            var result = await _diasporaPaymentService.GeneratePaymentReceiptAsync(transactionId, format);
            
            if (result.IsSuccess)
            {
                var receiptData = Convert.FromBase64String(result.Value);
                var contentType = format.ToUpper() == "PDF" ? "application/pdf" : "text/html";
                var fileName = $"payment-receipt-{transactionId}.{format.ToLower()}";
                
                return File(receiptData, contentType, fileName);
            }
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Send payment confirmation email
        /// </summary>
        [HttpPost("transactions/{transactionId:int}/send-confirmation")]
        [Authorize]
        public async Task<IActionResult> SendPaymentConfirmationEmail(int transactionId)
        {
            var result = await _diasporaPaymentService.SendPaymentConfirmationEmailAsync(transactionId);
            
            if (result.IsSuccess)
                return Ok(new { success = result.Value });
            
            return BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Get payment instructions for a provider and country
        /// </summary>
        [HttpGet("instructions/{provider}/{countryCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentInstructions(PaymentProvider provider, string countryCode)
        {
            var result = await _diasporaPaymentService.GetPaymentInstructionsAsync(provider, countryCode);
            
            if (result.IsSuccess)
                return Ok(result.Value);
            
            return NotFound(new { error = result.ErrorMessage });
        }

        #region Webhook Endpoints

        /// <summary>
        /// PayPal webhook for diaspora payments
        /// </summary>
        [HttpPost("webhook/paypal")]
        [AllowAnonymous]
        public async Task<IActionResult> PayPalDiasporaWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                
                var signature = Request.Headers["PayPal-Transmission-Sig"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                    return BadRequest("Missing signature");

                var result = await _diasporaPaymentService.ProcessDiasporaWebhookAsync(
                    PaymentProvider.PayPal, payload, signature);

                if (result.IsSuccess)
                    return Ok();
                
                _logger.LogWarning("PayPal diaspora webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal diaspora webhook");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Stripe webhook for diaspora payments
        /// </summary>
        [HttpPost("webhook/stripe")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeDiasporaWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();
                
                var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                    return BadRequest("Missing signature");

                var result = await _diasporaPaymentService.ProcessDiasporaWebhookAsync(
                    PaymentProvider.Stripe, payload, signature);

                if (result.IsSuccess)
                    return Ok();
                
                _logger.LogWarning("Stripe diaspora webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe diaspora webhook");
                return StatusCode(500);
            }
        }

        #endregion
    }

    // Additional DTOs for controller endpoints
    public class CurrencyConversionRequestDto
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
    }

    public class StripeConfirmationDto
    {
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    public class CancelPaymentDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class RefundDiasporaPaymentDto
    {
        public decimal? PartialAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}