using Kaban.Api.Features.Teams.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Teams.Application.ListTeams;

public sealed record ListTeamsQuery(int ActorId) : IRequest<TeamListDto>;
