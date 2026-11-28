using ProjectEntity = Kaban.Worker.Features.Projects.Domain.Projects;

namespace Kaban.Worker.Features.Boards.Domain;

public class Boards
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
}
