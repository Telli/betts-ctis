using BettsTax.Core.Services;
using BettsTax.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using BettsTax.Web.Authorization;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/client-portal")]
    [Authorize(Policy = "ClientPortal")]
    public class ClientPortalController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IDocumentService _documentService;
        private readonly IPaymentService _paymentService;
        private readonly ITaxFilingService _taxFilingService;
        private readonly IClientService _clientService;
        private readonly IUserContextService _userContextService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<ClientPortalController> _logger;

        public ClientPortalController(
            IDashboardService dashboardService,
            IDocumentService documentService,
            IPaymentService paymentService,
            ITaxFilingService taxFilingService,
            IClientService clientService,
            IUserContextService userContextService,
            IAuthorizationService authorizationService,
            ILogger<ClientPortalController> logger)
        {
            _dashboardService = dashboardService;
            _documentService = documentService;
            _paymentService = paymentService;
            _taxFilingService = taxFilingService;
            _clientService = clientService;
            _userContextService = userContextService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        /// <summary>
        /// Get client-specific dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetClientDashboard()
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var data = await _dashboardService.GetClientDashboardDataAsync(clientId.Value);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client dashboard data");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new tax filing for the current client
        /// </summary>
        [HttpPost("tax-filings")]
        public async Task<ActionResult<object>> CreateTaxFiling([FromBody] CreateTaxFilingDto createDto)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Create);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                // Set the client ID from the authenticated user context
                createDto.ClientId = clientId.Value;

                var userId = _userContextService.GetCurrentUserId();
                var taxFiling = await _taxFilingService.CreateTaxFilingAsync(createDto, userId!);
                return Ok(new { success = true, data = taxFiling });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tax filing for client");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create a new payment for the current client
        /// </summary>
        [HttpPost("payments")]
        public async Task<ActionResult<object>> CreatePayment([FromBody] CreatePaymentDto createDto)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Create);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                // Set the client ID from the authenticated user context
                createDto.ClientId = clientId.Value;

                var userId = _userContextService.GetCurrentUserId();
                var payment = await _paymentService.CreateAsync(createDto, userId!);
                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for client");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client's documents
        /// </summary>
        [HttpGet("documents")]
        public async Task<ActionResult<object>> GetClientDocuments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _documentService.GetDocumentsAsync(page, pageSize, null, null, clientId.Value);

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
                _logger.LogError(ex, "Error retrieving client documents");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload a document for the client
        /// </summary>
        [HttpPost("documents/upload")]
        public async Task<ActionResult<object>> UploadDocument([FromForm] UploadDocumentDto uploadDto, IFormFile file)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Update);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                // Override the client ID to ensure security
                uploadDto.ClientId = clientId.Value;

                var userId = _userContextService.GetCurrentUserId();
                var document = await _documentService.UploadAsync(uploadDto, file, userId!);

                return Ok(new { success = true, data = document });
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
        /// Download a document (client can only download their own documents)
        /// </summary>
        [HttpGet("documents/{documentId}/download")]
        public async Task<ActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var document = await _documentService.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                // Ensure the document belongs to the current client
                if (document.ClientId != clientId.Value)
                {
                    return Forbid();
                }

                var fileInfo = await _documentService.GetFileInfoAsync(documentId);
                if (fileInfo == null)
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fileInfo.Value.Path);
                return File(fileBytes, fileInfo.Value.ContentType, fileInfo.Value.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client's tax filings
        /// </summary>
        [HttpGet("tax-filings")]
        public async Task<ActionResult<object>> GetClientTaxFilings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _taxFilingService.GetTaxFilingsAsync(page, pageSize, null, null, null, clientId.Value);

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
                _logger.LogError(ex, "Error retrieving client tax filings");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client's payment history
        /// </summary>
        [HttpGet("payments")]
        public async Task<ActionResult<object>> GetClientPayments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _paymentService.GetPaymentsAsync(page, pageSize, null, null, clientId.Value);

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
                _logger.LogError(ex, "Error retrieving client payments");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get client's business profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<object>> GetClientProfile()
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var client = await _clientService.GetByIdAsync(clientId.Value);
                if (client == null)
                {
                    return NotFound(new { success = false, message = "Client not found" });
                }

                return Ok(new { success = true, data = client });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client profile");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update client's business profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<object>> UpdateClientProfile([FromBody] UpdateClientDto updateDto)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Update);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                // Get existing client first
                var existingClient = await _clientService.GetByIdAsync(clientId.Value);
                if (existingClient == null)
                {
                    return NotFound(new { success = false, message = "Client not found" });
                }

                // Update only the provided fields
                if (!string.IsNullOrEmpty(updateDto.BusinessName))
                    existingClient.BusinessName = updateDto.BusinessName;
                if (!string.IsNullOrEmpty(updateDto.ContactPerson))
                    existingClient.ContactPerson = updateDto.ContactPerson;
                if (!string.IsNullOrEmpty(updateDto.Email))
                    existingClient.Email = updateDto.Email;
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                    existingClient.PhoneNumber = updateDto.PhoneNumber;
                if (!string.IsNullOrEmpty(updateDto.Address))
                    existingClient.Address = updateDto.Address;
                if (updateDto.AnnualTurnover.HasValue)
                    existingClient.AnnualTurnover = updateDto.AnnualTurnover.Value;
                if (!string.IsNullOrEmpty(updateDto.TIN))
                    existingClient.TIN = updateDto.TIN;

                var client = await _clientService.UpdateAsync(clientId.Value, existingClient);

                return Ok(new { success = true, data = client });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation updating client profile");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client profile");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get compliance overview for the client
        /// </summary>
        [HttpGet("compliance")]
        public async Task<ActionResult<object>> GetClientCompliance()
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var compliance = await _dashboardService.GetClientComplianceOverviewAsync(clientId.Value);
                return Ok(new { success = true, data = compliance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client compliance data");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get upcoming deadlines for the client
        /// </summary>
        [HttpGet("deadlines")]
        public async Task<ActionResult<object>> GetClientDeadlines([FromQuery] int days = 30)
        {
            try
            {
                var clientId = await _userContextService.GetCurrentUserClientIdAsync();
                if (!clientId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Client profile not found" });
                }

                var authResult = await _authorizationService.AuthorizeAsync(User, clientId.Value, ClientDataOperations.Read);
                if (!authResult.Succeeded)
                {
                    return Forbid();
                }

                var deadlines = await _dashboardService.GetClientUpcomingDeadlinesAsync(clientId.Value, days);
                return Ok(new { success = true, data = deadlines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client deadlines");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}