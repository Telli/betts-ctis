using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IDocumentVerificationService
    {
        // Document verification operations
        Task<Result<DocumentVerificationDto>> GetDocumentVerificationAsync(int documentId);
        Task<Result<DocumentVerificationDto>> CreateDocumentVerificationAsync(DocumentVerificationCreateDto dto);
        Task<Result<DocumentVerificationDto>> UpdateDocumentVerificationAsync(int documentId, DocumentVerificationUpdateDto dto);
        Task<Result> ReviewDocumentAsync(DocumentReviewRequestDto dto);
        Task<Result> BulkReviewDocumentsAsync(BulkDocumentReviewDto dto);
        
        // Document requirements management
        Task<Result<List<DocumentRequirementDto>>> GetDocumentRequirementsAsync(TaxType? taxType = null, TaxpayerCategory? category = null);
        Task<Result<DocumentRequirementDto>> GetDocumentRequirementAsync(int requirementId);
        Task<Result<DocumentRequirementDto>> CreateDocumentRequirementAsync(DocumentRequirementDto dto);
        Task<Result<DocumentRequirementDto>> UpdateDocumentRequirementAsync(int requirementId, DocumentRequirementDto dto);
        Task<Result> DeleteDocumentRequirementAsync(int requirementId);
        
        // Client document requirements
        Task<Result<List<ClientDocumentRequirementDto>>> GetClientDocumentRequirementsAsync(int clientId, int taxFilingId);
        Task<Result> GenerateClientDocumentRequirementsAsync(int clientId, int taxFilingId);
        Task<Result> RequestDocumentsFromClientAsync(int clientId, int taxFilingId, List<int> requirementIds);
        Task<Result<DocumentVerificationSummaryDto>> GetDocumentVerificationSummaryAsync(int clientId, int taxFilingId);
        
        // Document verification history
        Task<Result<List<DocumentVerificationHistoryDto>>> GetDocumentVerificationHistoryAsync(int documentId);
        
        // Associate/Admin operations
        Task<Result<PagedResult<DocumentVerificationDto>>> GetPendingDocumentReviewsAsync(string? associateId = null, int page = 1, int pageSize = 20);
        Task<Result<PagedResult<DocumentVerificationDto>>> GetDocumentsByStatusAsync(DocumentVerificationStatus status, int page = 1, int pageSize = 20);
        
        // Utility methods
        Task<Result> ValidateDocumentForRequirementAsync(int documentId, int requirementId);
        Task<Result<bool>> CheckAllRequiredDocumentsVerifiedAsync(int clientId, int taxFilingId);
    }
}