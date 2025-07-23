using BettsTax.Core.DTOs;
using FluentValidation;

namespace BettsTax.Core.Validation
{
    public class CreateTaxFilingDtoValidator : AbstractValidator<CreateTaxFilingDto>
    {
        public CreateTaxFilingDtoValidator()
        {
            RuleFor(x => x.ClientId)
                .GreaterThan(0)
                .WithMessage("Client ID must be greater than 0");

            RuleFor(x => x.TaxType)
                .IsInEnum()
                .WithMessage("Invalid tax type");

            RuleFor(x => x.TaxYear)
                .InclusiveBetween(2000, 2100)
                .WithMessage("Tax year must be between 2000 and 2100");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow.AddDays(-1))
                .WithMessage("Due date cannot be in the past");

            RuleFor(x => x.TaxLiability)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Tax liability must be zero or positive");

            RuleFor(x => x.FilingReference)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.FilingReference))
                .WithMessage("Filing reference cannot exceed 50 characters");
        }
    }

    public class UpdateTaxFilingDtoValidator : AbstractValidator<UpdateTaxFilingDto>
    {
        public UpdateTaxFilingDtoValidator()
        {
            RuleFor(x => x.TaxType)
                .IsInEnum()
                .When(x => x.TaxType.HasValue)
                .WithMessage("Invalid tax type");

            RuleFor(x => x.TaxYear)
                .InclusiveBetween(2000, 2100)
                .When(x => x.TaxYear.HasValue)
                .WithMessage("Tax year must be between 2000 and 2100");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow.AddDays(-1))
                .When(x => x.DueDate.HasValue)
                .WithMessage("Due date cannot be in the past");

            RuleFor(x => x.TaxLiability)
                .GreaterThanOrEqualTo(0)
                .When(x => x.TaxLiability.HasValue)
                .WithMessage("Tax liability must be zero or positive");

            RuleFor(x => x.FilingReference)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.FilingReference))
                .WithMessage("Filing reference cannot exceed 50 characters");

            RuleFor(x => x.ReviewComments)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.ReviewComments))
                .WithMessage("Review comments cannot exceed 1000 characters");
        }
    }

    public class ReviewTaxFilingDtoValidator : AbstractValidator<ReviewTaxFilingDto>
    {
        public ReviewTaxFilingDtoValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid filing status");

            RuleFor(x => x.ReviewComments)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.ReviewComments))
                .WithMessage("Review comments cannot exceed 1000 characters");

            // Require comments for rejections
            RuleFor(x => x.ReviewComments)
                .NotEmpty()
                .When(x => x.Status == Data.FilingStatus.Rejected)
                .WithMessage("Review comments are required when rejecting a filing");
        }
    }
}