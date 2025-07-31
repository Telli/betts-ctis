using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Core.DTOs.Tax;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxCalculationEngineController : ControllerBase
{
    private readonly ITaxCalculationEngineService _taxCalculationService;
    private readonly ILogger<TaxCalculationEngineController> _logger;

    public TaxCalculationEngineController(
        ITaxCalculationEngineService taxCalculationService,
        ILogger<TaxCalculationEngineController> logger)
    {
        _taxCalculationService = taxCalculationService;
        _logger = logger;
    }

    #region Income Tax Calculations

    /// <summary>
    /// Calculate income tax based on Finance Act 2025 rules
    /// </summary>
    [HttpPost("income-tax/calculate")]
    public async Task<ActionResult<IncomeTaxCalculationDto>> CalculateIncomeTax(
        [FromBody] IncomeTaxCalculationRequestDto request)
    {
        try
        {
            var result = await _taxCalculationService.CalculateIncomeTaxAsync(request);
            
            _logger.LogInformation("Income tax calculated for {Category} taxpayer: {Amount} SLE", 
                request.TaxpayerCategory, result.IncomeTaxDue);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid income tax calculation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate income tax");
            return StatusCode(500, "Income tax calculation failed");
        }
    }

    /// <summary>
    /// Get income tax rates for a specific tax year and taxpayer category
    /// </summary>
    [HttpGet("income-tax/rates/{taxYear}/{taxpayerCategory}")]
    public async Task<ActionResult<List<IncomeTaxRateDto>>> GetIncomeTaxRates(
        int taxYear, 
        string taxpayerCategory)
    {
        try
        {
            var rates = await _taxCalculationService.GetIncomeTaxRatesAsync(taxYear, taxpayerCategory);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get income tax rates for {TaxYear} {Category}", taxYear, taxpayerCategory);
            return StatusCode(500, "Failed to retrieve income tax rates");
        }
    }

    #endregion

    #region GST Calculations

    /// <summary>
    /// Calculate GST liability
    /// </summary>
    [HttpPost("gst/calculate")]
    public async Task<ActionResult<GstCalculationDto>> CalculateGst(
        [FromBody] GstCalculationRequestDto request)
    {
        try
        {
            var result = await _taxCalculationService.CalculateGstAsync(request);
            
            _logger.LogInformation("GST calculated: {NetLiability} SLE net liability", result.NetGstLiability);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid GST calculation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate GST");
            return StatusCode(500, "GST calculation failed");
        }
    }

    /// <summary>
    /// Get current GST rate
    /// </summary>
    [HttpGet("gst/rate/{taxYear}")]
    public async Task<ActionResult<GstRateDto>> GetGstRate(int taxYear, [FromQuery] bool isExport = false)
    {
        try
        {
            var rate = await _taxCalculationService.GetGstRateAsync(taxYear, isExport);
            return Ok(rate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GST rate for tax year {TaxYear}", taxYear);
            return StatusCode(500, "Failed to retrieve GST rate");
        }
    }

    #endregion

    #region Payroll Tax Calculations

    /// <summary>
    /// Calculate payroll tax including PAYE and skills development levy
    /// </summary>
    [HttpPost("payroll-tax/calculate")]
    public async Task<ActionResult<PayrollTaxCalculationDto>> CalculatePayrollTax(
        [FromBody] PayrollTaxCalculationRequestDto request)
    {
        try
        {
            var result = await _taxCalculationService.CalculatePayrollTaxAsync(request);
            
            _logger.LogInformation("Payroll tax calculated: {Amount} SLE for {EmployeeCount} employees", 
                result.PayrollTaxDue, request.Employees.Count);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid payroll tax calculation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate payroll tax");
            return StatusCode(500, "Payroll tax calculation failed");
        }
    }

    #endregion

    #region Excise Duty Calculations

    /// <summary>
    /// Calculate excise duty for specified products
    /// </summary>
    [HttpPost("excise-duty/calculate")]
    public async Task<ActionResult<ExciseDutyCalculationDto>> CalculateExciseDuty(
        [FromBody] ExciseDutyCalculationRequestDto request)
    {
        try
        {
            var result = await _taxCalculationService.CalculateExciseDutyAsync(request);
            
            _logger.LogInformation("Excise duty calculated: {Amount} SLE for {Category} products", 
                result.TotalExciseDuty, request.ProductCategory);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid excise duty calculation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate excise duty");
            return StatusCode(500, "Excise duty calculation failed");
        }
    }

    /// <summary>
    /// Get excise duty rates for a product category
    /// </summary>
    [HttpGet("excise-duty/rates/{taxYear}/{productCategory}")]
    public async Task<ActionResult<List<ExciseDutyRateDto>>> GetExciseDutyRates(
        int taxYear, 
        string productCategory)
    {
        try
        {
            var rates = await _taxCalculationService.GetExciseDutyRatesAsync(taxYear, productCategory);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get excise duty rates for {Category}", productCategory);
            return StatusCode(500, "Failed to retrieve excise duty rates");
        }
    }

    #endregion

    #region Penalty Calculations

    /// <summary>
    /// Calculate penalties for late tax payments or filings
    /// </summary>
    [HttpPost("penalties/calculate")]
    public async Task<ActionResult<TaxPenaltyCalculationDto>> CalculateLatePenalties(
        [FromBody] CalculatePenaltiesRequestDto request)
    {
        try
        {
            var result = await _taxCalculationService.CalculateLatePenaltiesAsync(
                request.TaxAmount, 
                request.DueDate, 
                request.ActualDate, 
                request.TaxType);
            
            _logger.LogInformation("Penalties calculated: {Amount} SLE for {DaysLate} days late", 
                result.TotalPenalty, result.DaysLate);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid penalty calculation request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate penalties");
            return StatusCode(500, "Penalty calculation failed");
        }
    }

    /// <summary>
    /// Get penalty rules for a specific tax type
    /// </summary>
    [HttpGet("penalties/rules/{taxType}")]
    public async Task<ActionResult<List<TaxPenaltyRuleDto>>> GetPenaltyRules(string taxType)
    {
        try
        {
            var rules = await _taxCalculationService.GetPenaltyRulesAsync(taxType);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get penalty rules for {TaxType}", taxType);
            return StatusCode(500, "Failed to retrieve penalty rules");
        }
    }

    #endregion

    #region Tax Rate Management

    /// <summary>
    /// Create a new tax rate
    /// </summary>
    [HttpPost("rates")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<TaxRateDto>> CreateTaxRate([FromBody] CreateTaxRateDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _taxCalculationService.CreateTaxRateAsync(request, userId);
            
            _logger.LogInformation("Tax rate created: {TaxType} {Rate}% by {UserId}", 
                request.TaxType, request.Rate, userId);

            return CreatedAtAction(nameof(GetTaxRates), new { taxYear = request.TaxYear, taxType = request.TaxType }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid create tax rate request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tax rate");
            return StatusCode(500, "Failed to create tax rate");
        }
    }

    /// <summary>
    /// Update an existing tax rate
    /// </summary>
    [HttpPut("rates/{rateId}")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<ActionResult<TaxRateDto>> UpdateTaxRate(int rateId, [FromBody] CreateTaxRateDto request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _taxCalculationService.UpdateTaxRateAsync(rateId, request, userId);
            
            _logger.LogInformation("Tax rate updated: ID {RateId} -> {Rate}% by {UserId}", 
                rateId, request.Rate, userId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Tax rate not found: {RateId}", rateId);
            return NotFound($"Tax rate not found: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid update tax rate request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tax rate {RateId}", rateId);
            return StatusCode(500, "Failed to update tax rate");
        }
    }

    /// <summary>
    /// Get tax rates for a specific tax year and optionally tax type
    /// </summary>
    [HttpGet("rates/{taxYear}")]
    public async Task<ActionResult<List<TaxRateDto>>> GetTaxRates(int taxYear, [FromQuery] string? taxType = null)
    {
        try
        {
            var rates = await _taxCalculationService.GetTaxRatesAsync(taxYear, taxType);
            return Ok(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tax rates for tax year {TaxYear}", taxYear);
            return StatusCode(500, "Failed to retrieve tax rates");
        }
    }

    #endregion

    #region Comprehensive Tax Assessment

    /// <summary>
    /// Perform a comprehensive tax assessment for a client
    /// </summary>
    [HttpPost("assessment")]
    public async Task<ActionResult<ComprehensiveTaxAssessmentDto>> PerformComprehensiveTaxAssessment(
        [FromBody] ComprehensiveTaxAssessmentRequestDto request)
    {
        try
        {
            // Check if user can access this client's data
            if (!await CanAccessClientData(request.ClientId))
                return Forbid("Access denied to this client's data");

            var result = await _taxCalculationService.PerformComprehensiveTaxAssessmentAsync(request);
            
            _logger.LogInformation("Comprehensive tax assessment completed for client {ClientId}: {GrandTotal} SLE total", 
                request.ClientId, result.GrandTotal);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid comprehensive assessment request: {Message}", ex.Message);
            return BadRequest($"Invalid request: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform comprehensive tax assessment for client {ClientId}", request.ClientId);
            return StatusCode(500, "Comprehensive tax assessment failed");
        }
    }

    /// <summary>
    /// Calculate compliance score for a client
    /// </summary>
    [HttpGet("compliance/score/{clientId}/{taxYear}")]
    public async Task<ActionResult<TaxComplianceScoreDto>> GetComplianceScore(int clientId, int taxYear)
    {
        try
        {
            if (!await CanAccessClientData(clientId))
                return Forbid("Access denied to this client's data");

            var result = await _taxCalculationService.CalculateComplianceScoreAsync(clientId, taxYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate compliance score for client {ClientId}", clientId);
            return StatusCode(500, "Failed to calculate compliance score");
        }
    }

    /// <summary>
    /// Get compliance issues for a client
    /// </summary>
    [HttpGet("compliance/issues/{clientId}/{taxYear}")]
    public async Task<ActionResult<List<TaxComplianceIssueDto>>> GetComplianceIssues(int clientId, int taxYear)
    {
        try
        {
            if (!await CanAccessClientData(clientId))
                return Forbid("Access denied to this client's data");

            var result = await _taxCalculationService.IdentifyComplianceIssuesAsync(clientId, taxYear);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compliance issues for client {ClientId}", clientId);
            return StatusCode(500, "Failed to retrieve compliance issues");
        }
    }

    #endregion

    #region Tax Calculation Tools

    /// <summary>
    /// Get available tax types and their descriptions
    /// </summary>
    [HttpGet("tax-types")]
    public ActionResult<List<TaxTypeInfoDto>> GetTaxTypes()
    {
        var taxTypes = new List<TaxTypeInfoDto>
        {
            new()
            {
                TaxType = "Income Tax",
                Description = "Tax on personal and corporate income",
                ApplicableCategories = new[] { "Individual", "Large", "Medium", "Small", "Micro" }
            },
            new()
            {
                TaxType = "GST",
                Description = "Goods and Services Tax",
                ApplicableCategories = new[] { "Large", "Medium", "Small" }
            },
            new()
            {
                TaxType = "Payroll Tax",
                Description = "PAYE and Skills Development Levy",
                ApplicableCategories = new[] { "Large", "Medium", "Small" }
            },
            new()
            {
                TaxType = "Excise Duty",
                Description = "Tax on specific goods (tobacco, alcohol, fuel)",
                ApplicableCategories = new[] { "Large", "Medium", "Small" }
            }
        };

        return Ok(taxTypes);
    }

    /// <summary>
    /// Get taxpayer categories and their descriptions
    /// </summary>
    [HttpGet("taxpayer-categories")]
    public ActionResult<List<TaxpayerCategoryInfoDto>> GetTaxpayerCategories()
    {
        var categories = new List<TaxpayerCategoryInfoDto>
        {
            new()
            {
                Category = "Individual",
                Description = "Individual taxpayers and sole proprietors",
                AnnualTurnoverThreshold = null,
                TaxObligations = new[] { "Income Tax" }
            },
            new()
            {
                Category = "Large",
                Description = "Large companies and corporations",
                AnnualTurnoverThreshold = 10000000000, // 10B SLE
                TaxObligations = new[] { "Income Tax", "GST", "Payroll Tax", "Excise Duty" }
            },
            new()
            {
                Category = "Medium",
                Description = "Medium-sized businesses",
                AnnualTurnoverThreshold = 1000000000, // 1B SLE
                TaxObligations = new[] { "Income Tax", "GST", "Payroll Tax" }
            },
            new()
            {
                Category = "Small",
                Description = "Small businesses",
                AnnualTurnoverThreshold = 100000000, // 100M SLE
                TaxObligations = new[] { "Income Tax", "GST" }
            },
            new()
            {
                Category = "Micro",
                Description = "Micro businesses (tax-exempt)",
                AnnualTurnoverThreshold = 10000000, // 10M SLE
                TaxObligations = new string[] { }
            }
        };

        return Ok(categories);
    }

    /// <summary>
    /// Get Finance Act 2025 key changes and updates
    /// </summary>
    [HttpGet("finance-act-2025/changes")]
    public ActionResult<FinanceAct2025ChangesDto> GetFinanceAct2025Changes()
    {
        var changes = new FinanceAct2025ChangesDto
        {
            EffectiveDate = new DateTime(2025, 1, 1),
            KeyChanges = new List<FinanceActChangeDto>
            {
                new()
                {
                    ChangeType = "Income Tax",
                    Description = "New progressive tax rates for individuals",
                    Details = "15% (up to 12M), 20% (12M-60M), 30% (above 60M) SLE"
                },
                new()
                {
                    ChangeType = "Corporate Tax",
                    Description = "Reduced corporate tax rates",
                    Details = "Large: 30%, Medium: 25%, Small: 20%, Micro: 0%"
                },
                new()
                {
                    ChangeType = "GST",
                    Description = "Maintained GST rate",
                    Details = "15% standard rate, 0% for exports"
                },
                new()
                {
                    ChangeType = "Minimum Tax",
                    Description = "New minimum tax for companies",
                    Details = "Large: 0.5% of turnover, Medium: 0.25% of turnover"
                },
                new()
                {
                    ChangeType = "Skills Levy",
                    Description = "New skills development levy",
                    Details = "1% of total payroll for all employers"
                },
                new()
                {
                    ChangeType = "Penalties",
                    Description = "Increased penalties and interest rates",
                    Details = "2% interest per month, 5% late filing penalty"
                }
            },
            ComplianceDeadlines = new Dictionary<string, DateTime>
            {
                ["Income Tax"] = new DateTime(DateTime.Now.Year + 1, 3, 31),
                ["GST"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 15),
                ["Payroll Tax"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 15),
                ["Excise Duty"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 15)
            }
        };

        return Ok(changes);
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> CanAccessClientData(int clientId)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admins and SystemAdmins can access all client data
        if (userRole == "Admin" || userRole == "SystemAdmin")
            return true;

        // Associates can access their assigned clients
        if (userRole == "Associate")
        {
            // Implementation would check if associate is assigned to the client
            return true; // Simplified for now
        }

        // Clients can only access their own data
        if (userRole == "Client")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userClientId = await GetClientIdForUser(userId);
            return clientId == userClientId;
        }

        return false;
    }

    private async Task<int?> GetClientIdForUser(string? userId)
    {
        // This would typically query the database to find the client ID for the user
        // Implementation depends on your user-client relationship structure
        return await Task.FromResult<int?>(null); // Simplified for now
    }

    #endregion
}

// Supporting DTOs for API responses
public class CalculatePenaltiesRequestDto
{
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ActualDate { get; set; }
    public string TaxType { get; set; } = string.Empty;
}

public class TaxTypeInfoDto
{
    public string TaxType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] ApplicableCategories { get; set; } = Array.Empty<string>();
}

public class TaxpayerCategoryInfoDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? AnnualTurnoverThreshold { get; set; }
    public string[] TaxObligations { get; set; } = Array.Empty<string>();
}

public class FinanceAct2025ChangesDto
{
    public DateTime EffectiveDate { get; set; }
    public List<FinanceActChangeDto> KeyChanges { get; set; } = new();
    public Dictionary<string, DateTime> ComplianceDeadlines { get; set; } = new();
}

public class FinanceActChangeDto
{
    public string ChangeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}