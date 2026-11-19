using MediatR;

namespace Kaban.Api.Features.Teams.Application.DeleteTeam;

public sealed record DeleteTeamCommand(int ActorId, int TeamId) : IRequest;
