using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.GetTeam;

public sealed record GetTeamQuery(int ActorId, int TeamId) : IRequest<TeamDetailsDto>;
