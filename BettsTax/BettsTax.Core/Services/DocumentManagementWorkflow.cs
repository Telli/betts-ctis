using BettsTax.Data;
using BettsTax.Shared;
using BettsTax.Core.DTOs.Documents;
using BettsTax.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Document Management Workflow Service - Manages document submissions, verification, and approvals
    /// </summary>
    public class DocumentManagementWorkflow : IDocumentManagementWorkflow
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DocumentManagementWorkflow> _logger;

        public DocumentManagementWorkflow(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService,
            ILogger<DocumentManagementWorkflow> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Result<DocumentSubmissionDto>> SubmitDocumentAsync(
            int documentId, int clientId, string documentType, string submittedBy)
        {
            try
            {
                _logger.LogInformation("Submitting document {DocumentId} for client {ClientId}", documentId, clientId);

                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                    return Result.Failure<DocumentSubmissionDto>("Document not found");

                var submission = new DocumentSubmissionWorkflow
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ClientId = clientId,
                    DocumentType = documentType,
                    Status = DocumentSubmissionStatus.Submitted,
                    SubmittedAt = DateTime.UtcNow,
                    SubmittedBy = submittedBy,
                    CreatedAt = DateTime.UtcNow
                };

                // Create submission step
                var step = new DocumentSubmissionStep
                {
                    Id = Guid.NewGuid(),
                    DocumentSubmissionWorkflowId = submission.Id,
                    StepType = "Submission",
                    Status = DocumentSubmissionStepStatus.Completed,
                    CompletedAt = DateTime.UtcNow,
                    CompletedBy = submittedBy
                };

                submission.SubmissionSteps.Add(step);

                // Create verification step if required
                if (submission.RequiresVerification)
                {
                    var verificationStep = new DocumentSubmissionStep
                    {
                        Id = Guid.NewGuid(),
                        DocumentSubmissionWorkflowId = submission.Id,
                        StepType = "Verification",
                        Status = DocumentSubmissionStepStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    submission.SubmissionSteps.Add(verificationStep);
                    submission.Status = DocumentSubmissionStatus.UnderVerification;
                }

                _context.DocumentSubmissionWorkflows.Add(submission);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(submittedBy, "CREATE", "DocumentSubmission", submission.Id.ToString(),
                    $"Submitted document {documentId} for verification");

                _logger.LogInformation("Document submitted: {SubmissionId}", submission.Id);

                return Result.Success(MapToDto(submission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting document");
                return Result.Failure<DocumentSubmissionDto>($"Error submitting document: {ex.Message}");
            }
        }

        public async Task<Result<DocumentSubmissionDto>> VerifyDocumentAsync(
            Guid submissionId, string verifiedBy, string? notes = null)
        {
            try
            {
                var submission = await _context.DocumentSubmissionWorkflows
                    .Include(s => s.SubmissionSteps)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                    return Result.Failure<DocumentSubmissionDto>("Submission not found");

                submission.VerifiedAt = DateTime.UtcNow;
                submission.VerifiedBy = verifiedBy;
                submission.VerificationNotes = notes;
                submission.Status = DocumentSubmissionStatus.VerificationPassed;

                var verificationStep = submission.SubmissionSteps.FirstOrDefault(s => s.StepType == "Verification");
                if (verificationStep != null)
                {
                    verificationStep.Status = DocumentSubmissionStepStatus.Completed;
                    verificationStep.CompletedAt = DateTime.UtcNow;
                    verificationStep.CompletedBy = verifiedBy;
                    verificationStep.Comments = notes;
                }

                // Create approval step if required
                if (submission.RequiresApproval)
                {
                    var approvalStep = new DocumentSubmissionStep
                    {
                        Id = Guid.NewGuid(),
                        DocumentSubmissionWorkflowId = submission.Id,
                        StepType = "Approval",
                        Status = DocumentSubmissionStepStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    submission.SubmissionSteps.Add(approvalStep);
                    submission.Status = DocumentSubmissionStatus.UnderApproval;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(verifiedBy, "VERIFY", "DocumentSubmission", submissionId.ToString(),
                    $"Verified document submission");

                return Result.Success(MapToDto(submission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying document");
                return Result.Failure<DocumentSubmissionDto>($"Error verifying document: {ex.Message}");
            }
        }

        public async Task<Result<DocumentSubmissionDto>> ApproveDocumentAsync(
            Guid submissionId, string approvedBy, string? comments = null)
        {
            try
            {
                var submission = await _context.DocumentSubmissionWorkflows
                    .Include(s => s.SubmissionSteps)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                    return Result.Failure<DocumentSubmissionDto>("Submission not found");

                submission.ApprovedAt = DateTime.UtcNow;
                submission.ApprovedBy = approvedBy;
                submission.Status = DocumentSubmissionStatus.Approved;

                var approvalStep = submission.SubmissionSteps.FirstOrDefault(s => s.StepType == "Approval");
                if (approvalStep != null)
                {
                    approvalStep.Status = DocumentSubmissionStepStatus.Completed;
                    approvalStep.CompletedAt = DateTime.UtcNow;
                    approvalStep.CompletedBy = approvedBy;
                    approvalStep.Comments = comments;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(approvedBy, "APPROVE", "DocumentSubmission", submissionId.ToString(),
                    $"Approved document submission");

                return Result.Success(MapToDto(submission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document");
                return Result.Failure<DocumentSubmissionDto>($"Error approving document: {ex.Message}");
            }
        }

        public async Task<Result<DocumentSubmissionDto>> RejectDocumentAsync(
            Guid submissionId, string rejectedBy, string rejectionReason)
        {
            try
            {
                var submission = await _context.DocumentSubmissionWorkflows
                    .Include(s => s.SubmissionSteps)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                    return Result.Failure<DocumentSubmissionDto>("Submission not found");

                submission.RejectedAt = DateTime.UtcNow;
                submission.RejectedBy = rejectedBy;
                submission.RejectionReason = rejectionReason;
                submission.Status = DocumentSubmissionStatus.Rejected;

                var currentStep = submission.SubmissionSteps.LastOrDefault();
                if (currentStep != null)
                {
                    currentStep.Status = DocumentSubmissionStepStatus.Failed;
                    currentStep.CompletedAt = DateTime.UtcNow;
                    currentStep.CompletedBy = rejectedBy;
                    currentStep.Comments = rejectionReason;
                }

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(rejectedBy, "REJECT", "DocumentSubmission", submissionId.ToString(),
                    $"Rejected document submission. Reason: {rejectionReason}");

                return Result.Success(MapToDto(submission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document");
                return Result.Failure<DocumentSubmissionDto>($"Error rejecting document: {ex.Message}");
            }
        }

        public async Task<Result<DocumentSubmissionDto>> GetSubmissionStatusAsync(Guid submissionId)
        {
            try
            {
                var submission = await _context.DocumentSubmissionWorkflows
                    .Include(s => s.SubmissionSteps)
                    .Include(s => s.VerificationResults)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                    return Result.Failure<DocumentSubmissionDto>("Submission not found");

                return Result.Success(MapToDto(submission));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission status");
                return Result.Failure<DocumentSubmissionDto>($"Error getting submission status: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentSubmissionDto>>> GetClientSubmissionsAsync(int clientId)
        {
            try
            {
                var submissions = await _context.DocumentSubmissionWorkflows
                    .Where(s => s.ClientId == clientId)
                    .Include(s => s.SubmissionSteps)
                    .Include(s => s.VerificationResults)
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToListAsync();

                return Result.Success(submissions.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client submissions");
                return Result.Failure<List<DocumentSubmissionDto>>($"Error getting client submissions: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentSubmissionDto>>> GetPendingVerificationsAsync()
        {
            try
            {
                var submissions = await _context.DocumentSubmissionWorkflows
                    .Where(s => s.Status == DocumentSubmissionStatus.UnderVerification)
                    .Include(s => s.SubmissionSteps)
                    .OrderBy(s => s.SubmittedAt)
                    .ToListAsync();

                return Result.Success(submissions.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verifications");
                return Result.Failure<List<DocumentSubmissionDto>>($"Error getting pending verifications: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentSubmissionDto>>> GetPendingApprovalsAsync()
        {
            try
            {
                var submissions = await _context.DocumentSubmissionWorkflows
                    .Where(s => s.Status == DocumentSubmissionStatus.UnderApproval)
                    .Include(s => s.SubmissionSteps)
                    .OrderBy(s => s.SubmittedAt)
                    .ToListAsync();

                return Result.Success(submissions.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return Result.Failure<List<DocumentSubmissionDto>>($"Error getting pending approvals: {ex.Message}");
            }
        }

        public async Task<Result<DocumentVerificationResultDto>> AddVerificationResultAsync(
            Guid submissionId, string verificationType, string status, string? findings = null)
        {
            try
            {
                var result = new DocumentVerificationResult
                {
                    Id = Guid.NewGuid(),
                    DocumentSubmissionWorkflowId = submissionId,
                    VerificationType = verificationType,
                    Status = Enum.Parse<VerificationResultStatus>(status),
                    Findings = findings,
                    VerifiedAt = DateTime.UtcNow
                };

                _context.DocumentVerificationResults.Add(result);
                await _context.SaveChangesAsync();

                return Result.Success(MapVerificationToDto(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding verification result");
                return Result.Failure<DocumentVerificationResultDto>($"Error adding verification result: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentVerificationResultDto>>> GetVerificationResultsAsync(Guid submissionId)
        {
            try
            {
                var results = await _context.DocumentVerificationResults
                    .Where(r => r.DocumentSubmissionWorkflowId == submissionId)
                    .OrderByDescending(r => r.VerifiedAt)
                    .ToListAsync();

                return Result.Success(results.Select(MapVerificationToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification results");
                return Result.Failure<List<DocumentVerificationResultDto>>($"Error getting verification results: {ex.Message}");
            }
        }

        public async Task<Result<DocumentVersionControlDto>> CreateDocumentVersionAsync(
            int documentId, string fileName, long fileSize, string fileHash, string uploadedBy, string? changeDescription = null)
        {
            try
            {
                var latestVersion = await _context.DocumentVersionControls
                    .Where(v => v.DocumentId == documentId)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync();

                var newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

                var version = new DocumentVersionControl
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    VersionNumber = newVersionNumber,
                    FileName = fileName,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    ChangeDescription = changeDescription,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Deactivate previous version
                if (latestVersion != null)
                    latestVersion.IsActive = false;

                _context.DocumentVersionControls.Add(version);
                await _context.SaveChangesAsync();

                return Result.Success(MapVersionToDto(version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document version");
                return Result.Failure<DocumentVersionControlDto>($"Error creating document version: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentVersionControlDto>>> GetDocumentVersionHistoryAsync(int documentId)
        {
            try
            {
                var versions = await _context.DocumentVersionControls
                    .Where(v => v.DocumentId == documentId)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();

                return Result.Success(versions.Select(MapVersionToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document version history");
                return Result.Failure<List<DocumentVersionControlDto>>($"Error getting document version history: {ex.Message}");
            }
        }

        public async Task<Result<DocumentVersionControlDto>> GetActiveDocumentVersionAsync(int documentId)
        {
            try
            {
                var version = await _context.DocumentVersionControls
                    .Where(v => v.DocumentId == documentId && v.IsActive)
                    .FirstOrDefaultAsync();

                if (version == null)
                    return Result.Failure<DocumentVersionControlDto>("No active version found");

                return Result.Success(MapVersionToDto(version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active document version");
                return Result.Failure<DocumentVersionControlDto>($"Error getting active document version: {ex.Message}");
            }
        }

        public async Task<Result<DocumentSubmissionStatisticsDto>> GetSubmissionStatisticsAsync(
            int? clientId = null, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var query = _context.DocumentSubmissionWorkflows.AsQueryable();

                if (clientId.HasValue)
                    query = query.Where(s => s.ClientId == clientId.Value);

                if (from.HasValue)
                    query = query.Where(s => s.CreatedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(s => s.CreatedAt <= to.Value);

                var submissions = await query.ToListAsync();

                var stats = new DocumentSubmissionStatisticsDto
                {
                    TotalSubmissions = submissions.Count,
                    ApprovedCount = submissions.Count(s => s.Status == DocumentSubmissionStatus.Approved),
                    RejectedCount = submissions.Count(s => s.Status == DocumentSubmissionStatus.Rejected),
                    PendingCount = submissions.Count(s => s.Status == DocumentSubmissionStatus.Submitted || s.Status == DocumentSubmissionStatus.UnderVerification || s.Status == DocumentSubmissionStatus.UnderApproval),
                    VerificationPassedCount = submissions.Count(s => s.Status == DocumentSubmissionStatus.VerificationPassed),
                    VerificationFailedCount = submissions.Count(s => s.Status == DocumentSubmissionStatus.VerificationFailed)
                };

                if (stats.TotalSubmissions > 0)
                    stats.ApprovalRate = (decimal)stats.ApprovedCount / stats.TotalSubmissions * 100;

                return Result.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission statistics");
                return Result.Failure<DocumentSubmissionStatisticsDto>($"Error getting submission statistics: {ex.Message}");
            }
        }

        public async Task<Result<List<DocumentSubmissionStepDto>>> GetSubmissionStepsAsync(Guid submissionId)
        {
            try
            {
                var steps = await _context.DocumentSubmissionSteps
                    .Where(s => s.DocumentSubmissionWorkflowId == submissionId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                return Result.Success(steps.Select(MapStepToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission steps");
                return Result.Failure<List<DocumentSubmissionStepDto>>($"Error getting submission steps: {ex.Message}");
            }
        }

        // Helper methods
        private DocumentSubmissionDto MapToDto(DocumentSubmissionWorkflow submission)
        {
            return new DocumentSubmissionDto
            {
                Id = submission.Id,
                DocumentId = submission.DocumentId,
                ClientId = submission.ClientId,
                DocumentType = submission.DocumentType,
                Status = submission.Status.ToString(),
                SubmittedAt = submission.SubmittedAt,
                VerifiedAt = submission.VerifiedAt,
                ApprovedAt = submission.ApprovedAt,
                RejectedAt = submission.RejectedAt,
                SubmittedBy = submission.SubmittedBy,
                VerifiedBy = submission.VerifiedBy,
                ApprovedBy = submission.ApprovedBy,
                RejectionReason = submission.RejectionReason,
                VersionNumber = submission.VersionNumber,
                Steps = submission.SubmissionSteps.Select(MapStepToDto).ToList(),
                VerificationResults = submission.VerificationResults.Select(MapVerificationToDto).ToList()
            };
        }

        private DocumentSubmissionStepDto MapStepToDto(DocumentSubmissionStep step)
        {
            return new DocumentSubmissionStepDto
            {
                Id = step.Id,
                StepType = step.StepType,
                Status = step.Status.ToString(),
                AssignedTo = step.AssignedTo,
                Comments = step.Comments,
                CreatedAt = step.CreatedAt,
                CompletedAt = step.CompletedAt
            };
        }

        private DocumentVerificationResultDto MapVerificationToDto(DocumentVerificationResult result)
        {
            return new DocumentVerificationResultDto
            {
                Id = result.Id,
                VerificationType = result.VerificationType,
                Status = result.Status.ToString(),
                Findings = result.Findings,
                Recommendations = result.Recommendations,
                VerifiedAt = result.VerifiedAt,
                VerifiedBy = result.VerifiedBy
            };
        }

        private DocumentVersionControlDto MapVersionToDto(DocumentVersionControl version)
        {
            return new DocumentVersionControlDto
            {
                Id = version.Id,
                DocumentId = version.DocumentId,
                VersionNumber = version.VersionNumber,
                FileName = version.FileName,
                FileSize = version.FileSize,
                FileHash = version.FileHash,
                ChangeDescription = version.ChangeDescription,
                UploadedBy = version.UploadedBy,
                UploadedAt = version.UploadedAt,
                IsActive = version.IsActive
            };
        }
    }
}

