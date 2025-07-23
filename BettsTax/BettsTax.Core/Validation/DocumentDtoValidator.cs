using BettsTax.Core.Services;
using FluentValidation;

namespace BettsTax.Core.Validation
{
    public class UploadDocumentDtoValidator : AbstractValidator<UploadDocumentDto>
    {
        public UploadDocumentDtoValidator()
        {
            RuleFor(x => x.ClientId)
                .GreaterThan(0)
                .WithMessage("Client ID must be greater than 0");

            RuleFor(x => x.Category)
                .IsInEnum()
                .WithMessage("Invalid document category");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Document description is required")
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters");

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

    public class UpdateDocumentDtoValidator : AbstractValidator<UpdateDocumentDto>
    {
        public UpdateDocumentDtoValidator()
        {
            RuleFor(x => x.Category)
                .IsInEnum()
                .When(x => x.Category.HasValue)
                .WithMessage("Invalid document category");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description cannot exceed 500 characters");
        }
    }
}