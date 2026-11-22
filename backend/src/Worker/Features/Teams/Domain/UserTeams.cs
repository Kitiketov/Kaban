using TeamEntity = Kaban.Worker.Features.Teams.Domain.Teams;

namespace Kaban.Worker.Features.Teams.Domain;

public class UserTeams
{
    public int UserId { get; set; }
    public Users.Domain.Users? User { get; set; }
    public int TeamId { get; set; }
    public TeamEntity? Team { get; set; }
    public int RoleId { get; set; }
    public Roles? Role { get; set; }
}
