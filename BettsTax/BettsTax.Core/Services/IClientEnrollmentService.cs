using BettsTax.Core.DTOs;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IClientEnrollmentService
    {
        Task<Result> SendInvitationAsync(string email, string associateId);
        Task<Result<TokenValidationResult>> ValidateTokenAsync(string token);
        Task<Result> CompleteRegistrationAsync(ClientRegistrationDto dto);
        Task<Result> InitiateSelfRegistrationAsync(string email);
        Task<Result> VerifyEmailAsync(string token);
        Task<Result> ResendVerificationAsync(string email);
        Task<Result> CancelInvitationAsync(int invitationId, string associateId);
        Task<Result<IEnumerable<object>>> GetPendingInvitationsAsync(string associateId);
    }
}