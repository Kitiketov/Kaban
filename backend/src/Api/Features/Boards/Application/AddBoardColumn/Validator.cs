using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.AddBoardColumn;

public sealed class AddBoardColumnValidator : AbstractValidator<AddBoardColumnCommand>
{
    public AddBoardColumnValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}