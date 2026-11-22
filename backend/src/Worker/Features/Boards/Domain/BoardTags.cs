using Kaban.Worker.Features.Tasks.Domain;

namespace Kaban.Worker.Features.Boards.Domain;

public class BoardTags
{
    public int TagId { get; set; }
    public TaskTags? Tag { get; set; }

    public int BoardId { get; set; }
    public Boards? Board { get; set; }
}
