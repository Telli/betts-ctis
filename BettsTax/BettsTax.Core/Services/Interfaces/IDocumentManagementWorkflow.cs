using BettsTax.Shared;
using BettsTax.Core.DTOs.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BettsTax.Core.Services.Interfaces
{
    /// <summary>
    /// Document Management Workflow Service - Manages document submissions, verification, and approvals
    /// </summary>
    public interface IDocumentManagementWorkflow
    {
        /// <summary>
        /// Submit a document for verification and approval
        /// </summary>
        Task<Result<DocumentSubmissionDto>> SubmitDocumentAsync(
            int documentId,
            int clientId,
            string documentType,
            string submittedBy);

        /// <summary>
        /// Verify a submitted document
        /// </summary>
        Task<Result<DocumentSubmissionDto>> VerifyDocumentAsync(
            Guid submissionId,
            string verifiedBy,
            string? notes = null);

        /// <summary>
        /// Approve a verified document
        /// </summary>
        Task<Result<DocumentSubmissionDto>> ApproveDocumentAsync(
            Guid submissionId,
            string approvedBy,
            string? comments = null);

        /// <summary>
        /// Reject a document submission
        /// </summary>
        Task<Result<DocumentSubmissionDto>> RejectDocumentAsync(
            Guid submissionId,
            string rejectedBy,
            string rejectionReason);

        /// <summary>
        /// Get document submission status
        /// </summary>
        Task<Result<DocumentSubmissionDto>> GetSubmissionStatusAsync(Guid submissionId);

        /// <summary>
        /// Get all submissions for a client
        /// </summary>
        Task<Result<List<DocumentSubmissionDto>>> GetClientSubmissionsAsync(int clientId);

        /// <summary>
        /// Get pending submissions for verification
        /// </summary>
        Task<Result<List<DocumentSubmissionDto>>> GetPendingVerificationsAsync();

        /// <summary>
        /// Get pending submissions for approval
        /// </summary>
        Task<Result<List<DocumentSubmissionDto>>> GetPendingApprovalsAsync();

        /// <summary>
        /// Add verification result
        /// </summary>
        Task<Result<DocumentVerificationResultDto>> AddVerificationResultAsync(
            Guid submissionId,
            string verificationType,
            string status,
            string? findings = null);

        /// <summary>
        /// Get verification results for a submission
        /// </summary>
        Task<Result<List<DocumentVerificationResultDto>>> GetVerificationResultsAsync(Guid submissionId);

        /// <summary>
        /// Create new document version
        /// </summary>
        Task<Result<DocumentVersionControlDto>> CreateDocumentVersionAsync(
            int documentId,
            string fileName,
            long fileSize,
            string fileHash,
            string uploadedBy,
            string? changeDescription = null);

        /// <summary>
        /// Get document version history
        /// </summary>
        Task<Result<List<DocumentVersionControlDto>>> GetDocumentVersionHistoryAsync(int documentId);

        /// <summary>
        /// Get active version of a document
        /// </summary>
        Task<Result<DocumentVersionControlDto>> GetActiveDocumentVersionAsync(int documentId);

        /// <summary>
        /// Get document submission statistics
        /// </summary>
        Task<Result<DocumentSubmissionStatisticsDto>> GetSubmissionStatisticsAsync(
            int? clientId = null,
            DateTime? from = null,
            DateTime? to = null);

        /// <summary>
        /// Get submission workflow steps
        /// </summary>
        Task<Result<List<DocumentSubmissionStepDto>>> GetSubmissionStepsAsync(Guid submissionId);
    }
}

