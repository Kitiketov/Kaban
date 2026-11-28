namespace Kaban.Worker.Features.Teams.Domain;

public class Roles
{
    public int Id { get; set; }
    public required string RoleName { get; set; }

    public int TeamId { get; set; }
    public Teams? Team { get; set; }
}
