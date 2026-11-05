using MediatR;
namespace Kaban.Api.Features.Users.Application.Register;
public sealed record RegisterUserCommand(string Email, string Name, string Password) : IRequest;

