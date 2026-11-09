using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.ListBoards;

public sealed class ListBoardsValidator : AbstractValidator<ListBoardsQuery>
{
    public ListBoardsValidator() => RuleFor(x => x.ProjectId).GreaterThan(0);
}