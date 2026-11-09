using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.DeleteBoardColumn;

public sealed class DeleteBoardColumnValidator : AbstractValidator<DeleteBoardColumnCommand>
{
    public DeleteBoardColumnValidator() => RuleFor(x => x.ColumnId).GreaterThan(0);
}