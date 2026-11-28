namespace Kaban.Worker.Features.Users.Domain;

public class Users
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
}
