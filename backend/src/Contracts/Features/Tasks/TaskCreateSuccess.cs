namespace Contracts.Features.Tasks;

public record TaskCreateSuccess
{
    public int TaskId { get; set; }
    public int Order { get; set; }
}