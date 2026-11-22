using TaskEntity = Kaban.Worker.Features.Tasks.Domain.Tasks;

namespace Kaban.Worker.Features.Boards.Domain;

public class BoardColumns
{
    public int Id { get; set; }
    public required string ColumnName { get; set; }
    public int BoardOrder { get; set; }

    public int BoardId { get; set; }
    public Boards? Board { get; set; }

    public List<TaskEntity> Tasks { get; set; } = [];
}
