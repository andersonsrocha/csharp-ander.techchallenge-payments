using TechChallengePayments.Domain.Dto;

namespace TechChallengePayments.Application;

public interface IClient
{
    Task<LoginDto?> LoginAsync();
    Task<UserDto?> GetUserAsync(Guid userId, string jwtToken);
    Task<GameDto?> GetGameAsync(Guid gameId, string jwtToken);
}