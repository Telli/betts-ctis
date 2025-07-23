using BettsTax.Core.DTOs;
using FluentValidation;

namespace BettsTax.Core.Validation
{
    public class ClientDtoValidator : AbstractValidator<ClientDto>
    {
        public ClientDtoValidator()
        {
            RuleFor(c => c.BusinessName).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Email).EmailAddress().NotEmpty();
            RuleFor(c => c.PhoneNumber).MaximumLength(20);
        }
    }
}
