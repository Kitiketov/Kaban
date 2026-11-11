using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.CreateTeam;

public sealed record CreateTeamCommand(int ActorId, string Name) : IRequest<TeamDto>;
