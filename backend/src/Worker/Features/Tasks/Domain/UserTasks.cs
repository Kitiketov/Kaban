using UserEntity = Kaban.Worker.Features.Users.Domain.Users;

namespace Kaban.Worker.Features.Tasks.Domain;

public class UserTasks
{
    public int UserId { get; set; }
    public UserEntity? User { get; set; }

    public int TaskId { get; set; }
    public Tasks? Task { get; set; }
}
