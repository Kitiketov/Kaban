using TeamEntity = Kaban.Worker.Features.Teams.Domain.Teams;

namespace Kaban.Worker.Features.Projects.Domain;

public class TeamProjects
{
    public int ProjectId { get; set; }
    public Projects? Project { get; set; }

    public int TeamId { get; set; }
    public TeamEntity? Team { get; set; }
}
