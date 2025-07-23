using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class ClientEnrollmentService : IClientEnrollmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ISecureTokenGenerator _tokenGenerator;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientEnrollmentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public ClientEnrollmentService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ISecureTokenGenerator tokenGenerator,
            IMapper mapper,
            ILogger<ClientEnrollmentService> logger,
            IConfiguration configuration,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _tokenGenerator = tokenGenerator;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _auditService = auditService;
        }

        public async Task<Result> SendInvitationAsync(string email, string associateId)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Result.Failure("A user with this email address already exists.");
                }

                // Check for pending invitation
                var existingInvitation = await _context.ClientInvitations
                    .FirstOrDefaultAsync(ci => ci.Email == email && ci.Status == InvitationStatus.Pending);

                if (existingInvitation != null)
                {
                    // Cancel existing invitation and create new one
                    existingInvitation.Status = InvitationStatus.Cancelled;
                }

                // Create new invitation
                var token = _tokenGenerator.GenerateRegistrationToken();
                var invitation = new ClientInvitation
                {
                    Email = email,
                    Token = token,
                    InvitedByAssociateId = associateId,
                    ExpirationDate = _tokenGenerator.GetDefaultExpirationTime(TokenType.Registration),
                    Status = InvitationStatus.Pending
                };

                _context.ClientInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Get associate details
                var associate = await _userManager.FindByIdAsync(associateId);
                var associateName = associate != null ? $"{associate.FirstName} {associate.LastName}" : "Associate";

                // Generate registration URL
                var baseUrl = _configuration["ApplicationSettings:BaseUrl"] ?? "https://localhost:3000";
                var registrationUrl = $"{baseUrl}/enroll/register/{token}";

                // Send invitation email
                await _emailService.SendClientInvitationAsync(email, registrationUrl, associateName);

                // Log the activity
                await _auditService.LogAsync(associateId, "INVITE", "ClientInvitation", invitation.Id.ToString(), $"Invitation sent to {email}");

                _logger.LogInformation("Client invitation sent to {Email} by associate {AssociateId}", email, associateId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending client invitation to {Email}", email);
                return Result.Failure("Failed to send invitation. Please try again.");
            }
        }

        public async Task<Result<TokenValidationResult>> ValidateTokenAsync(string token)
        {
            try
            {
                var invitation = await _context.ClientInvitations
                    .FirstOrDefaultAsync(ci => ci.Token == token);

                if (invitation == null)
                {
                    return Result.Success(new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invalid invitation token."
                    });
                }

                if (invitation.IsUsed || invitation.Status != InvitationStatus.Pending)
                {
                    return Result.Success(new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "This invitation has already been used."
                    });
                }

                if (_tokenGenerator.IsTokenExpired(invitation.ExpirationDate))
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _context.SaveChangesAsync();

                    return Result.Success(new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "This invitation has expired."
                    });
                }

                return Result.Success(new TokenValidationResult
                {
                    IsValid = true,
                    Email = invitation.Email,
                    ExpirationDate = invitation.ExpirationDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token {Token}", token);
                return Result.Failure<TokenValidationResult>("Failed to validate token.");
            }
        }

        public async Task<Result> CompleteRegistrationAsync(ClientRegistrationDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Validate token
                var tokenValidation = await ValidateTokenAsync(dto.RegistrationToken);
                if (!tokenValidation.IsSuccess || !tokenValidation.Value.IsValid)
                {
                    return Result.Failure(tokenValidation.Value?.ErrorMessage ?? "Invalid token.");
                }

                // Create user account
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    EmailConfirmed = true, // Since they came through invitation, email is confirmed
                    EmailVerified = true,
                    IsActive = true,
                    RegistrationSource = RegistrationSource.Invitation,
                    RegistrationCompletedDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return Result.Failure(errors);
                }

                // Assign Client role
                await _userManager.AddToRoleAsync(user, "Client");

                // Create client profile
                var client = new Client
                {
                    UserId = user.Id,
                    BusinessName = dto.BusinessName,
                    ClientType = dto.ClientType,
                    TaxpayerCategory = dto.TaxpayerCategory,
                    TIN = dto.TaxpayerIdentificationNumber,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Address = dto.BusinessAddress ?? "",
                    ContactPerson = dto.ContactPersonName ?? $"{dto.FirstName} {dto.LastName}",
                    AnnualTurnover = dto.AnnualTurnover ?? 0,
                    Status = ClientStatus.Active,
                    ClientNumber = GenerateClientNumber()
                };

                _context.Clients.Add(client);

                // Mark invitation as used
                var invitation = await _context.ClientInvitations
                    .FirstOrDefaultAsync(ci => ci.Token == dto.RegistrationToken);

                if (invitation != null)
                {
                    invitation.IsUsed = true;
                    invitation.Status = InvitationStatus.Completed;
                    
                    // Assign client to the associate who sent the invitation
                    client.AssignedAssociateId = invitation.InvitedByAssociateId;
                }

                // Save all changes
                await _context.SaveChangesAsync();

                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(dto.Email, $"{dto.FirstName} {dto.LastName}");

                // Notify the associate
                if (invitation != null)
                {
                    var associate = await _userManager.FindByIdAsync(invitation.InvitedByAssociateId);
                    if (associate != null)
                    {
                        await _emailService.SendRegistrationCompletedNotificationAsync(
                            associate.Email!, 
                            $"{dto.FirstName} {dto.LastName}"
                        );
                    }
                }

                // Log the activity
                await _auditService.LogAsync(user.Id, "REGISTER", "Client", client.ClientId.ToString(), $"Registration completed for {dto.Email}");

                await transaction.CommitAsync();

                _logger.LogInformation("Client registration completed for {Email}", dto.Email);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing registration for {Email}", dto.Email);
                return Result.Failure("Registration failed. Please try again.");
            }
        }

        public async Task<Result> InitiateSelfRegistrationAsync(string email)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Result.Failure("A user with this email address already exists.");
                }

                // Create registration record
                var token = _tokenGenerator.GenerateRegistrationToken();
                var registration = new ClientRegistration
                {
                    Email = email,
                    RegistrationToken = token,
                    Type = RegistrationType.SelfRegistration,
                    Status = RegistrationStatus.Started
                };

                _context.ClientRegistrations.Add(registration);
                await _context.SaveChangesAsync();

                // Generate registration URL
                var baseUrl = _configuration["ApplicationSettings:BaseUrl"] ?? "https://localhost:3000";
                var registrationUrl = $"{baseUrl}/enroll/register/{token}";

                // Send email verification
                await _emailService.SendEmailVerificationAsync(email, registrationUrl);

                _logger.LogInformation("Self-registration initiated for {Email}", email);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating self-registration for {Email}", email);
                return Result.Failure("Failed to initiate registration. Please try again.");
            }
        }

        public async Task<Result> VerifyEmailAsync(string token)
        {
            try
            {
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

                if (user == null)
                {
                    return Result.Failure("Invalid verification token.");
                }

                if (user.EmailVerified)
                {
                    return Result.Failure("Email is already verified.");
                }

                // Check token expiration (24 hours)
                if (user.EmailVerificationSentDate.HasValue &&
                    DateTime.UtcNow > user.EmailVerificationSentDate.Value.AddHours(24))
                {
                    return Result.Failure("Verification link has expired.");
                }

                user.EmailVerified = true;
                user.EmailConfirmed = true;
                user.EmailVerificationToken = null;

                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Email verified for user {UserId}", user.Id);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email with token {Token}", token);
                return Result.Failure("Email verification failed.");
            }
        }

        public async Task<Result> ResendVerificationAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Result.Failure("User not found.");
                }

                if (user.EmailVerified)
                {
                    return Result.Failure("Email is already verified.");
                }

                // Generate new verification token
                user.EmailVerificationToken = _tokenGenerator.GenerateEmailVerificationToken();
                user.EmailVerificationSentDate = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);

                // Generate verification URL
                var baseUrl = _configuration["ApplicationSettings:BaseUrl"] ?? "https://localhost:3000";
                var verificationUrl = $"{baseUrl}/enroll/verify-email/{user.EmailVerificationToken}";

                // Send verification email
                await _emailService.SendEmailVerificationAsync(email, verificationUrl);

                _logger.LogInformation("Email verification resent to {Email}", email);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification to {Email}", email);
                return Result.Failure("Failed to resend verification email.");
            }
        }

        public async Task<Result> CancelInvitationAsync(int invitationId, string associateId)
        {
            try
            {
                var invitation = await _context.ClientInvitations
                    .FirstOrDefaultAsync(ci => ci.Id == invitationId && ci.InvitedByAssociateId == associateId);

                if (invitation == null)
                {
                    return Result.Failure("Invitation not found.");
                }

                if (invitation.Status != InvitationStatus.Pending)
                {
                    return Result.Failure("Can only cancel pending invitations.");
                }

                invitation.Status = InvitationStatus.Cancelled;
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(associateId, "CANCEL", "ClientInvitation", invitation.Id.ToString(), $"Invitation cancelled for {invitation.Email}");

                _logger.LogInformation("Invitation {InvitationId} cancelled by associate {AssociateId}", invitationId, associateId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invitation {InvitationId}", invitationId);
                return Result.Failure("Failed to cancel invitation.");
            }
        }

        public async Task<Result<IEnumerable<object>>> GetPendingInvitationsAsync(string associateId)
        {
            try
            {
                var invitations = await _context.ClientInvitations
                    .Where(ci => ci.InvitedByAssociateId == associateId && ci.Status == InvitationStatus.Pending)
                    .Select(ci => new
                    {
                        ci.Id,
                        ci.Email,
                        ci.CreatedDate,
                        ci.ExpirationDate,
                        ci.Status,
                        IsExpired = DateTime.UtcNow > ci.ExpirationDate
                    })
                    .OrderByDescending(ci => ci.CreatedDate)
                    .ToListAsync();

                return Result.Success<IEnumerable<object>>(invitations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending invitations for associate {AssociateId}", associateId);
                return Result.Failure<IEnumerable<object>>("Failed to retrieve pending invitations.");
            }
        }

        private string GenerateClientNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"CL{timestamp}{random}";
        }
    }
}