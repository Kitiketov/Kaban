using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.RenameBoard;

public sealed class RenameBoardValidator : AbstractValidator<RenameBoardCommand>
{
    public RenameBoardValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}