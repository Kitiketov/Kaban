namespace Kaban.Worker.Features.Tasks.Domain;

public class TaskTags
{
    public int Id { get; set; }
    public required string TagName { get; set; }

    public int TaskId { get; set; }
    public Tasks? Task { get; set; }
}
