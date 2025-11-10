using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class TaxFilingService : ITaxFilingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TaxFilingService> _logger;
        private readonly IAuditService _auditService;
        private readonly ISierraLeoneTaxCalculationService _taxCalculationService;

        public TaxFilingService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<TaxFilingService> logger,
            IAuditService auditService,
            ISierraLeoneTaxCalculationService taxCalculationService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _taxCalculationService = taxCalculationService;
        }

        public async Task<PagedResult<TaxFilingDto>> GetTaxFilingsAsync(
            int page, 
            int pageSize, 
            string? searchTerm = null, 
            TaxType? taxType = null, 
            FilingStatus? status = null, 
            int? clientId = null)
        {
            var query = _context.TaxFilings
                .Include(tf => tf.Client)
                .Include(tf => tf.SubmittedBy)
                .Include(tf => tf.ReviewedBy)
                .Include(tf => tf.Documents)
                .Include(tf => tf.Payments)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(tf => 
                    tf.FilingReference.Contains(searchTerm) ||
                    tf.Client!.BusinessName.Contains(searchTerm) ||
                    tf.Client!.ClientNumber.Contains(searchTerm));
            }

            if (taxType.HasValue)
                query = query.Where(tf => tf.TaxType == taxType.Value);

            if (status.HasValue)
                query = query.Where(tf => tf.Status == status.Value);

            if (clientId.HasValue)
                query = query.Where(tf => tf.ClientId == clientId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(tf => tf.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<TaxFilingDto>>(items);

            return new PagedResult<TaxFilingDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<TaxFilingDto?> GetTaxFilingByIdAsync(int id)
        {
            var taxFiling = await _context.TaxFilings
                .Include(tf => tf.Client)
                .Include(tf => tf.SubmittedBy)
                .Include(tf => tf.ReviewedBy)
                .Include(tf => tf.Documents)
                .Include(tf => tf.Payments)
                .FirstOrDefaultAsync(tf => tf.TaxFilingId == id);

            return taxFiling == null ? null : _mapper.Map<TaxFilingDto>(taxFiling);
        }

        public async Task<List<TaxFilingDto>> GetTaxFilingsByClientIdAsync(int clientId)
        {
            var taxFilings = await _context.TaxFilings
                .Include(tf => tf.Client)
                .Include(tf => tf.SubmittedBy)
                .Include(tf => tf.ReviewedBy)
                .Include(tf => tf.Documents)
                .Include(tf => tf.Payments)
                .Where(tf => tf.ClientId == clientId)
                .OrderByDescending(tf => tf.TaxYear)
                .ThenByDescending(tf => tf.CreatedDate)
                .ToListAsync();

            return _mapper.Map<List<TaxFilingDto>>(taxFilings);
        }

        public async Task<TaxFilingDto> CreateTaxFilingAsync(CreateTaxFilingDto createDto, string userId)
        {
            // Validate client exists
            var client = await _context.Clients.FindAsync(createDto.ClientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            // Generate filing reference if not provided
            var filingReference = createDto.FilingReference ?? 
                GenerateFilingReference(createDto.TaxType, createDto.TaxYear, createDto.ClientId);

            var taxFiling = new TaxFiling
            {
                ClientId = createDto.ClientId,
                TaxType = createDto.TaxType,
                TaxYear = createDto.TaxYear,
                FilingDate = DateTime.UtcNow,
                DueDate = createDto.DueDate,
                Status = FilingStatus.Draft,
                TaxLiability = createDto.TaxLiability,
                FilingReference = filingReference,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                // Extended mapping
                FilingPeriod = string.IsNullOrWhiteSpace(createDto.FilingPeriod) ? string.Empty : createDto.FilingPeriod!,
                TaxableAmount = createDto.TaxableAmount ?? 0,
                PenaltyAmount = createDto.PenaltyAmount,
                InterestAmount = createDto.InterestAmount,
                AdditionalData = string.IsNullOrWhiteSpace(createDto.AdditionalData) ? null : createDto.AdditionalData,
                // Withholding-specific
                WithholdingTaxSubtype = string.IsNullOrWhiteSpace(createDto.WithholdingTaxSubtype) ? null : createDto.WithholdingTaxSubtype,
                IsResident = createDto.IsResident
            };

            _context.TaxFilings.Add(taxFiling);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "CREATE", "TaxFiling", taxFiling.TaxFilingId.ToString(), 
                $"Created tax filing {filingReference} for client {client.BusinessName}");

            _logger.LogInformation("Created tax filing {FilingReference} for client {ClientId}", 
                filingReference, createDto.ClientId);

            return await GetTaxFilingByIdAsync(taxFiling.TaxFilingId) ?? 
                throw new InvalidOperationException("Failed to retrieve created tax filing");
        }

        public async Task<TaxFilingDto> UpdateTaxFilingAsync(int id, UpdateTaxFilingDto updateDto, string userId)
        {
            var taxFiling = await _context.TaxFilings.FindAsync(id);
            if (taxFiling == null)
                throw new InvalidOperationException("Tax filing not found");

            // Only allow updates if not filed
            if (taxFiling.Status == FilingStatus.Filed)
                throw new InvalidOperationException("Cannot update filed tax returns");

            var oldValues = new
            {
                taxFiling.TaxType,
                taxFiling.TaxYear,
                taxFiling.DueDate,
                taxFiling.TaxLiability,
                taxFiling.FilingReference,
                taxFiling.ReviewComments,
                taxFiling.FilingPeriod,
                taxFiling.TaxableAmount,
                taxFiling.PenaltyAmount,
                taxFiling.InterestAmount,
                taxFiling.AdditionalData
            };

            // Update fields
            if (updateDto.TaxType.HasValue)
                taxFiling.TaxType = updateDto.TaxType.Value;
            if (updateDto.TaxYear.HasValue)
                taxFiling.TaxYear = updateDto.TaxYear.Value;
            if (updateDto.DueDate.HasValue)
                taxFiling.DueDate = updateDto.DueDate.Value;
            if (updateDto.TaxLiability.HasValue)
                taxFiling.TaxLiability = updateDto.TaxLiability.Value;
            if (!string.IsNullOrEmpty(updateDto.FilingReference))
                taxFiling.FilingReference = updateDto.FilingReference;
            if (updateDto.ReviewComments != null)
                taxFiling.ReviewComments = updateDto.ReviewComments;
            // Extended mapping
            if (!string.IsNullOrWhiteSpace(updateDto.FilingPeriod))
                taxFiling.FilingPeriod = updateDto.FilingPeriod!;
            if (updateDto.TaxableAmount.HasValue)
                taxFiling.TaxableAmount = updateDto.TaxableAmount.Value;
            if (updateDto.PenaltyAmount.HasValue)
                taxFiling.PenaltyAmount = updateDto.PenaltyAmount.Value;
            if (updateDto.InterestAmount.HasValue)
                taxFiling.InterestAmount = updateDto.InterestAmount.Value;
            if (updateDto.AdditionalData != null)
                taxFiling.AdditionalData = updateDto.AdditionalData;
            // Withholding-specific
            if (!string.IsNullOrWhiteSpace(updateDto.WithholdingTaxSubtype))
                taxFiling.WithholdingTaxSubtype = updateDto.WithholdingTaxSubtype;
            if (updateDto.IsResident.HasValue)
                taxFiling.IsResident = updateDto.IsResident.Value;

            taxFiling.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UPDATE", "TaxFiling", taxFiling.TaxFilingId.ToString(),
                $"Updated tax filing {taxFiling.FilingReference}");

            _logger.LogInformation("Updated tax filing {TaxFilingId}", id);

            return await GetTaxFilingByIdAsync(id) ??
                throw new InvalidOperationException("Failed to retrieve updated tax filing");
        }

        public async Task<bool> DeleteTaxFilingAsync(int id, string userId)
        {
            var taxFiling = await _context.TaxFilings.FindAsync(id);
            if (taxFiling == null)
                return false;

            // Only allow deletion if draft
            if (taxFiling.Status != FilingStatus.Draft)
                throw new InvalidOperationException("Can only delete draft tax filings");

            _context.TaxFilings.Remove(taxFiling);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "DELETE", "TaxFiling", taxFiling.TaxFilingId.ToString(),
                $"Deleted tax filing {taxFiling.FilingReference}");

            _logger.LogInformation("Deleted tax filing {TaxFilingId}", id);

            return true;
        }

        public async Task<TaxFilingDto> SubmitTaxFilingAsync(int id, string userId)
        {
            var taxFiling = await _context.TaxFilings.FindAsync(id);
            if (taxFiling == null)
                throw new InvalidOperationException("Tax filing not found");

            if (taxFiling.Status != FilingStatus.Draft)
                throw new InvalidOperationException("Only draft filings can be submitted");

            taxFiling.Status = FilingStatus.Submitted;
            taxFiling.SubmittedById = userId;
            taxFiling.SubmittedDate = DateTime.UtcNow;
            taxFiling.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "SUBMIT", "TaxFiling", taxFiling.TaxFilingId.ToString(),
                $"Submitted tax filing {taxFiling.FilingReference} for review");

            _logger.LogInformation("Submitted tax filing {TaxFilingId} for review", id);

            return await GetTaxFilingByIdAsync(id) ??
                throw new InvalidOperationException("Failed to retrieve submitted tax filing");
        }

        public async Task<TaxFilingDto> ReviewTaxFilingAsync(int id, ReviewTaxFilingDto reviewDto, string userId)
        {
            var taxFiling = await _context.TaxFilings.FindAsync(id);
            if (taxFiling == null)
                throw new InvalidOperationException("Tax filing not found");

            if (taxFiling.Status != FilingStatus.Submitted && taxFiling.Status != FilingStatus.UnderReview)
                throw new InvalidOperationException("Only submitted filings can be reviewed");

            taxFiling.Status = reviewDto.Status;
            taxFiling.ReviewedById = userId;
            taxFiling.ReviewedDate = DateTime.UtcNow;
            taxFiling.ReviewComments = reviewDto.ReviewComments;
            taxFiling.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "REVIEW", "TaxFiling", taxFiling.TaxFilingId.ToString(),
                $"Reviewed tax filing {taxFiling.FilingReference} - Status: {reviewDto.Status}");

            _logger.LogInformation("Reviewed tax filing {TaxFilingId} with status {Status}", id, reviewDto.Status);

            return await GetTaxFilingByIdAsync(id) ??
                throw new InvalidOperationException("Failed to retrieve reviewed tax filing");
        }

        public async Task<List<TaxFilingDto>> GetUpcomingDeadlinesAsync(int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(days);
            
            var taxFilings = await _context.TaxFilings
                .Include(tf => tf.Client)
                .Include(tf => tf.SubmittedBy)
                .Include(tf => tf.ReviewedBy)
                .Where(tf => tf.DueDate <= cutoffDate && 
                           tf.Status != FilingStatus.Filed &&
                           tf.DueDate >= DateTime.UtcNow.Date)
                .OrderBy(tf => tf.DueDate)
                .ToListAsync();

            return _mapper.Map<List<TaxFilingDto>>(taxFilings);
        }

        public async Task<decimal> CalculateTaxLiabilityAsync(int clientId, TaxType taxType, int taxYear, decimal taxableAmount, decimal annualTurnover = 0, bool isIndividual = false)
        {
            // Get client for taxpayer category
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            // Calculate tax liability with proper Sierra Leone tax rates
            return taxType switch
            {
                TaxType.IncomeTax => _taxCalculationService.CalculateIncomeTax(taxableAmount, client.TaxpayerCategory, isIndividual),
                TaxType.PersonalIncomeTax => _taxCalculationService.CalculateIncomeTax(taxableAmount, client.TaxpayerCategory, true),
                TaxType.CorporateIncomeTax => _taxCalculationService.CalculateIncomeTax(taxableAmount, client.TaxpayerCategory, false),
                TaxType.GST => _taxCalculationService.CalculateGST(taxableAmount),
                TaxType.PayrollTax => _taxCalculationService.CalculatePAYE(taxableAmount),
                TaxType.PAYE => _taxCalculationService.CalculatePAYE(taxableAmount),
                TaxType.WithholdingTax => _taxCalculationService.CalculateWithholdingTax(taxableAmount, WithholdingTaxType.ProfessionalFees),
                TaxType.ExciseDuty => CalculateExciseDuty(taxableAmount), // Keep existing simple calculation for now
                _ => throw new NotSupportedException($"Tax type {taxType} not supported")
            };
        }

        /// <summary>
        /// Calculate comprehensive tax liability including penalties and interest
        /// </summary>
        public async Task<TaxCalculationResult> CalculateComprehensiveTaxLiabilityAsync(
            int clientId, 
            TaxType taxType, 
            int taxYear, 
            decimal taxableAmount, 
            DateTime dueDate,
            decimal annualTurnover = 0, 
            bool isIndividual = false)
        {
            // Get client for taxpayer category
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            return _taxCalculationService.CalculateTotalTaxLiability(
                taxableAmount,
                taxType,
                client.TaxpayerCategory,
                dueDate,
                annualTurnover,
                isIndividual
            );
        }

        private string GenerateFilingReference(TaxType taxType, int taxYear, int clientId)
        {
            var prefix = taxType switch
            {
                TaxType.IncomeTax => "IT",
                TaxType.GST => "GST",
                TaxType.PayrollTax => "PT",
                TaxType.ExciseDuty => "ED",
                TaxType.PAYE => "PAYE",
                TaxType.WithholdingTax => "WHT",
                TaxType.PersonalIncomeTax => "PIT",
                TaxType.CorporateIncomeTax => "CIT",
                _ => "TX"
            };

            return $"{prefix}-{taxYear}-{clientId:D6}-{DateTime.UtcNow:yyyyMMddHHmm}";
        }


        private decimal CalculateExciseDuty(decimal taxableAmount)
        {
            // Sierra Leone Excise Duty (varies by product)
            return taxableAmount * 0.10m; // 10% average rate
        }

        // Associate on-behalf methods
        public async Task<List<TaxFilingDto>> GetTaxFilingsForClientsAsync(List<int> clientIds, string? searchTerm = null, TaxType? taxType = null, FilingStatus? status = null)
        {
            try
            {
                var query = _context.TaxFilings
                    .Include(tf => tf.Client)
                    .Include(tf => tf.SubmittedBy)
                    .Include(tf => tf.ReviewedBy)
                    .Include(tf => tf.Documents)
                    .Include(tf => tf.Payments)
                    .Where(tf => clientIds.Contains(tf.ClientId));

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(tf => 
                        tf.FilingReference.Contains(searchTerm) ||
                        tf.Client!.BusinessName.Contains(searchTerm));
                }

                if (taxType.HasValue)
                {
                    query = query.Where(tf => tf.TaxType == taxType.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(tf => tf.Status == status.Value);
                }

                var taxFilings = await query
                    .OrderByDescending(tf => tf.CreatedDate)
                    .ToListAsync();

                return _mapper.Map<List<TaxFilingDto>>(taxFilings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tax filings for clients {ClientIds}", string.Join(",", clientIds));
                return new List<TaxFilingDto>();
            }
        }

        public async Task<TaxFilingDto> CreateTaxFilingOnBehalfAsync(CreateTaxFilingDto createDto, string associateId, int clientId)
        {
            try
            {
                var client = await _context.Clients.FindAsync(clientId);
                if (client == null)
                    throw new ArgumentException($"Client with ID {clientId} not found");

                var taxFiling = new TaxFiling
                {
                    ClientId = clientId,
                    TaxType = createDto.TaxType,
                    TaxYear = createDto.TaxYear,
                    DueDate = createDto.DueDate,
                    TaxLiability = createDto.TaxLiability,
                    FilingReference = createDto.FilingReference ?? GenerateFilingReference(createDto.TaxType, createDto.TaxYear, clientId),
                    Status = FilingStatus.Draft,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByAssociateId = associateId,
                    IsCreatedOnBehalf = true,
                    OnBehalfActionDate = DateTime.UtcNow,
                    FilingDate = DateTime.UtcNow,
                    // Withholding-specific
                    WithholdingTaxSubtype = string.IsNullOrWhiteSpace(createDto.WithholdingTaxSubtype) ? null : createDto.WithholdingTaxSubtype,
                    IsResident = createDto.IsResident
                };

                _context.TaxFilings.Add(taxFiling);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    associateId,
                    "Create",
                    "TaxFiling",
                    taxFiling.TaxFilingId.ToString(),
                    $"Created tax filing on behalf of client {client.BusinessName}"
                );

                return _mapper.Map<TaxFilingDto>(taxFiling);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tax filing on behalf for associate {AssociateId}, client {ClientId}", associateId, clientId);
                throw;
            }
        }

        public async Task<TaxFilingDto> UpdateTaxFilingOnBehalfAsync(int id, UpdateTaxFilingDto updateDto, string associateId, int clientId)
        {
            try
            {
                var taxFiling = await _context.TaxFilings
                    .Include(tf => tf.Client)
                    .FirstOrDefaultAsync(tf => tf.TaxFilingId == id && tf.ClientId == clientId);

                if (taxFiling == null)
                    throw new ArgumentException($"Tax filing with ID {id} not found for client {clientId}");

                if (taxFiling.Status == FilingStatus.Submitted || taxFiling.Status == FilingStatus.Approved)
                    throw new InvalidOperationException("Cannot update a submitted or approved tax filing");

                var oldValues = _mapper.Map<TaxFilingDto>(taxFiling);

                // Update fields
                if (updateDto.TaxLiability.HasValue)
                    taxFiling.TaxLiability = updateDto.TaxLiability.Value;
                if (updateDto.DueDate.HasValue)
                    taxFiling.DueDate = updateDto.DueDate.Value;
                if (updateDto.TaxType.HasValue)
                    taxFiling.TaxType = updateDto.TaxType.Value;
                if (updateDto.TaxYear.HasValue)
                    taxFiling.TaxYear = updateDto.TaxYear.Value;
                if (!string.IsNullOrEmpty(updateDto.FilingReference))
                    taxFiling.FilingReference = updateDto.FilingReference;
                if (!string.IsNullOrEmpty(updateDto.ReviewComments))
                    taxFiling.ReviewComments = updateDto.ReviewComments;
                // Extended mapping
                if (!string.IsNullOrWhiteSpace(updateDto.FilingPeriod))
                    taxFiling.FilingPeriod = updateDto.FilingPeriod!;
                if (updateDto.TaxableAmount.HasValue)
                    taxFiling.TaxableAmount = updateDto.TaxableAmount.Value;
                if (updateDto.PenaltyAmount.HasValue)
                    taxFiling.PenaltyAmount = updateDto.PenaltyAmount.Value;
                if (updateDto.InterestAmount.HasValue)
                    taxFiling.InterestAmount = updateDto.InterestAmount.Value;
                if (updateDto.AdditionalData != null)
                    taxFiling.AdditionalData = updateDto.AdditionalData;
                // Withholding-specific
                if (!string.IsNullOrWhiteSpace(updateDto.WithholdingTaxSubtype))
                    taxFiling.WithholdingTaxSubtype = updateDto.WithholdingTaxSubtype;
                if (updateDto.IsResident.HasValue)
                    taxFiling.IsResident = updateDto.IsResident.Value;
                    
                taxFiling.UpdatedDate = DateTime.UtcNow;
                taxFiling.LastModifiedByAssociateId = associateId;

                await _context.SaveChangesAsync();

                var newValues = _mapper.Map<TaxFilingDto>(taxFiling);

                await _auditService.LogAsync(
                    associateId,
                    "Update",
                    "TaxFiling",
                    taxFiling.TaxFilingId.ToString(),
                    $"Updated tax filing on behalf of client {taxFiling.Client!.BusinessName}"
                );

                return newValues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tax filing {Id} on behalf for associate {AssociateId}, client {ClientId}", id, associateId, clientId);
                throw;
            }
        }

        public async Task<TaxFilingDto> SubmitTaxFilingOnBehalfAsync(int id, string associateId, int clientId)
        {
            try
            {
                var taxFiling = await _context.TaxFilings
                    .Include(tf => tf.Client)
                    .FirstOrDefaultAsync(tf => tf.TaxFilingId == id && tf.ClientId == clientId);

                if (taxFiling == null)
                    throw new ArgumentException($"Tax filing with ID {id} not found for client {clientId}");

                if (taxFiling.Status != FilingStatus.Draft)
                    throw new InvalidOperationException("Only draft tax filings can be submitted");

                var oldValues = _mapper.Map<TaxFilingDto>(taxFiling);

                taxFiling.Status = FilingStatus.Submitted;
                taxFiling.SubmittedDate = DateTime.UtcNow;
                taxFiling.SubmittedById = associateId;
                taxFiling.UpdatedDate = DateTime.UtcNow;
                taxFiling.LastModifiedByAssociateId = associateId;

                await _context.SaveChangesAsync();

                var newValues = _mapper.Map<TaxFilingDto>(taxFiling);

                await _auditService.LogAsync(
                    associateId,
                    "Submit",
                    "TaxFiling",
                    taxFiling.TaxFilingId.ToString(),
                    $"Submitted tax filing on behalf of client {taxFiling.Client!.BusinessName}"
                );

                return newValues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting tax filing {Id} on behalf for associate {AssociateId}, client {ClientId}", id, associateId, clientId);
                throw;
            }
        }
    }
}