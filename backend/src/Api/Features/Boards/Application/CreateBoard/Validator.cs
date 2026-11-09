using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.CreateBoard;

public sealed class CreateBoardValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}