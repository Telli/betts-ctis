using BettsTax.Core.DTOs;
using FluentValidation;

namespace BettsTax.Core.Validation
{
    public class CreatePaymentDtoValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentDtoValidator()
        {
            RuleFor(x => x.ClientId)
                .GreaterThan(0)
                .WithMessage("Client ID must be greater than 0");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than 0")
                .LessThan(1000000000) // 1 billion max
                .WithMessage("Payment amount is too large");

            RuleFor(x => x.Method)
                .IsInEnum()
                .WithMessage("Invalid payment method");

            RuleFor(x => x.PaymentReference)
                .NotEmpty()
                .WithMessage("Payment reference is required")
                .MaximumLength(100)
                .WithMessage("Payment reference cannot exceed 100 characters");

            RuleFor(x => x.PaymentDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Payment date cannot be in the future")
                .GreaterThan(DateTime.UtcNow.AddYears(-10))
                .WithMessage("Payment date cannot be more than 10 years ago");

            RuleFor(x => x.TaxYearId)
                .GreaterThan(0)
                .When(x => x.TaxYearId.HasValue)
                .WithMessage("Tax Year ID must be greater than 0");

            RuleFor(x => x.TaxFilingId)
                .GreaterThan(0)
                .When(x => x.TaxFilingId.HasValue)
                .WithMessage("Tax Filing ID must be greater than 0");
        }
    }

    public class ApprovePaymentDtoValidator : AbstractValidator<ApprovePaymentDto>
    {
        public ApprovePaymentDtoValidator()
        {
            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Comments))
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }

    public class RejectPaymentDtoValidator : AbstractValidator<RejectPaymentDto>
    {
        public RejectPaymentDtoValidator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty()
                .WithMessage("Rejection reason is required")
                .MaximumLength(500)
                .WithMessage("Rejection reason cannot exceed 500 characters");
        }
    }
}