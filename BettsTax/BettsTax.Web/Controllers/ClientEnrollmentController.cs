using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Web.Filters;
using System.Security.Claims;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(AuditActionFilter))]
    public class ClientEnrollmentController : ControllerBase
    {
        private readonly IClientEnrollmentService _enrollmentService;
        private readonly ILogger<ClientEnrollmentController> _logger;

        public ClientEnrollmentController(
            IClientEnrollmentService enrollmentService,
            ILogger<ClientEnrollmentController> logger)
        {
            _enrollmentService = enrollmentService;
            _logger = logger;
        }

        /// <summary>
        /// Send a client invitation email (Associates and Admins only)
        /// </summary>
        /// <param name="dto">Client invitation details</param>
        /// <returns>Success or error response</returns>
        [HttpPost("invite")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult> SendClientInvitation([FromBody] ClientInvitationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var associateId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(associateId))
            {
                return Unauthorized("Unable to identify the current user.");
            }

            var result = await _enrollmentService.SendInvitationAsync(dto.Email, associateId);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Invitation sent successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Validate a registration token
        /// </summary>
        /// <param name="token">Registration token</param>
        /// <returns>Token validation result</returns>
        [HttpGet("validate-token/{token}")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenValidationResult>> ValidateRegistrationToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token is required" });
            }

            var result = await _enrollmentService.ValidateTokenAsync(token);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Complete client registration using invitation token
        /// </summary>
        /// <param name="dto">Client registration details</param>
        /// <returns>Success or error response</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> CompleteClientRegistration([FromBody] ClientRegistrationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _enrollmentService.CompleteRegistrationAsync(dto);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Registration completed successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Initiate self-registration process
        /// </summary>
        /// <param name="dto">Self-registration details</param>
        /// <returns>Success or error response</returns>
        [HttpPost("self-register")]
        [AllowAnonymous]
        public async Task<ActionResult> InitiateSelfRegistration([FromBody] SelfRegistrationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _enrollmentService.InitiateSelfRegistrationAsync(dto.Email);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Registration link has been sent to your email address" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        /// <param name="dto">Email verification details</param>
        /// <returns>Success or error response</returns>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<ActionResult> VerifyEmail([FromBody] EmailVerificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _enrollmentService.VerifyEmailAsync(dto.Token);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Email verified successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="dto">Resend verification details</param>
        /// <returns>Success or error response</returns>
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<ActionResult> ResendVerification([FromBody] ResendVerificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _enrollmentService.ResendVerificationAsync(dto.Email);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Verification email sent successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Cancel a pending invitation (Associates and Admins only)
        /// </summary>
        /// <param name="invitationId">Invitation ID</param>
        /// <returns>Success or error response</returns>
        [HttpPost("cancel-invitation/{invitationId}")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult> CancelInvitation(int invitationId)
        {
            var associateId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(associateId))
            {
                return Unauthorized("Unable to identify the current user.");
            }

            var result = await _enrollmentService.CancelInvitationAsync(invitationId, associateId);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Invitation cancelled successfully" });
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }

        /// <summary>
        /// Get pending invitations for the current associate
        /// </summary>
        /// <returns>List of pending invitations</returns>
        [HttpGet("pending-invitations")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<ActionResult> GetPendingInvitations()
        {
            var associateId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(associateId))
            {
                return Unauthorized("Unable to identify the current user.");
            }

            var result = await _enrollmentService.GetPendingInvitationsAsync(associateId);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.ErrorMessage, errors = result.Errors });
        }
    }
}