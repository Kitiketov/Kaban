using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.GetBoard;

public sealed class GetBoardValidator : AbstractValidator<GetBoardQuery>
{
    public GetBoardValidator() => RuleFor(x => x.BoardId).GreaterThan(0);
}