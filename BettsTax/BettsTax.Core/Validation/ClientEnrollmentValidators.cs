using FluentValidation;
using BettsTax.Core.DTOs;

namespace BettsTax.Core.Validation
{
    public class ClientInvitationDtoValidator : AbstractValidator<ClientInvitationDto>
    {
        public ClientInvitationDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email address is required")
                .EmailAddress()
                .WithMessage("Please provide a valid email address");
        }
    }

    public class ClientRegistrationDtoValidator : AbstractValidator<ClientRegistrationDto>
    {
        public ClientRegistrationDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email address is required")
                .EmailAddress()
                .WithMessage("Please provide a valid email address");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Please confirm your password")
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .Length(2, 50)
                .WithMessage("First name must be between 2 and 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .Length(2, 50)
                .WithMessage("Last name must be between 2 and 50 characters");

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required")
                .Length(2, 100)
                .WithMessage("Business name must be between 2 and 100 characters");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required")
                .Matches(@"^[+]?[\d\s\-()]+$")
                .WithMessage("Please provide a valid phone number");

            RuleFor(x => x.TaxpayerCategory)
                .IsInEnum()
                .WithMessage("Please select a valid taxpayer category");

            RuleFor(x => x.ClientType)
                .IsInEnum()
                .WithMessage("Please select a valid client type");

            RuleFor(x => x.RegistrationToken)
                .NotEmpty()
                .WithMessage("Registration token is required");

            RuleFor(x => x.TaxpayerIdentificationNumber)
                .Length(5, 20)
                .When(x => !string.IsNullOrEmpty(x.TaxpayerIdentificationNumber))
                .WithMessage("TIN must be between 5 and 20 characters");

            RuleFor(x => x.AnnualTurnover)
                .GreaterThanOrEqualTo(0)
                .When(x => x.AnnualTurnover.HasValue)
                .WithMessage("Annual turnover must be a positive number");
        }
    }

    public class SelfRegistrationDtoValidator : AbstractValidator<SelfRegistrationDto>
    {
        public SelfRegistrationDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email address is required")
                .EmailAddress()
                .WithMessage("Please provide a valid email address");
        }
    }

    public class EmailVerificationDtoValidator : AbstractValidator<EmailVerificationDto>
    {
        public EmailVerificationDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Verification token is required");
        }
    }

    public class ResendVerificationDtoValidator : AbstractValidator<ResendVerificationDto>
    {
        public ResendVerificationDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email address is required")
                .EmailAddress()
                .WithMessage("Please provide a valid email address");
        }
    }
}