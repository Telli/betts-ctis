using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs.Documents
{
    /// <summary>
    /// Document Submission DTO
    /// </summary>
    public class DocumentSubmissionDto
    {
        public Guid Id { get; set; }
        public int DocumentId { get; set; }
        public int ClientId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? SubmittedBy { get; set; }
        public string? VerifiedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? RejectionReason { get; set; }
        public int VersionNumber { get; set; }
        public List<DocumentSubmissionStepDto> Steps { get; set; } = new();
        public List<DocumentVerificationResultDto> VerificationResults { get; set; } = new();
    }

    /// <summary>
    /// Document Submission Step DTO
    /// </summary>
    public class DocumentSubmissionStepDto
    {
        public Guid Id { get; set; }
        public string StepType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Document Verification Result DTO
    /// </summary>
    public class DocumentVerificationResultDto
    {
        public Guid Id { get; set; }
        public string VerificationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Findings { get; set; }
        public string? Recommendations { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
    }

    /// <summary>
    /// Document Version Control DTO
    /// </summary>
    public class DocumentVersionControlDto
    {
        public Guid Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public string? ChangeDescription { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Document Submission Statistics DTO
    /// </summary>
    public class DocumentSubmissionStatisticsDto
    {
        public int TotalSubmissions { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public int VerificationPassedCount { get; set; }
        public int VerificationFailedCount { get; set; }
        public decimal ApprovalRate { get; set; }
        public int AverageVerificationTime { get; set; }
        public int AverageApprovalTime { get; set; }
    }

    /// <summary>
    /// Request to submit a document
    /// </summary>
    public class SubmitDocumentRequest
    {
        public int DocumentId { get; set; }
        public int ClientId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to verify a document
    /// </summary>
    public class VerifyDocumentRequest
    {
        public Guid SubmissionId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to approve a document
    /// </summary>
    public class ApproveDocumentRequest
    {
        public Guid SubmissionId { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to reject a document
    /// </summary>
    public class RejectDocumentRequest
    {
        public Guid SubmissionId { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to add verification result
    /// </summary>
    public class AddVerificationResultRequest
    {
        public Guid SubmissionId { get; set; }
        public string VerificationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Findings { get; set; }
        public string? Recommendations { get; set; }
    }

    /// <summary>
    /// Request to create document version
    /// </summary>
    public class CreateDocumentVersionRequest
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public string? ChangeDescription { get; set; }
    }
}

