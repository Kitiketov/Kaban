using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.DeleteBoard;

public sealed class DeleteBoardValidator : AbstractValidator<DeleteBoardCommand>
{
    public DeleteBoardValidator() => RuleFor(x => x.BoardId).GreaterThan(0);
}