using FluentValidation;
namespace Kaban.Api.Features.Users.Application.Register;
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty().WithMessage("A non-empty UUID Idempotency-Key is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(1024);
    }
}

