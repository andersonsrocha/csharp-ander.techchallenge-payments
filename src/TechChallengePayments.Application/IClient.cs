using TechChallengePayments.Domain.Dto;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Application;

public interface IClient
{
    Task<LoginDto?> LoginAsync();
    Task<UserDto?> GetUserAsync(Guid userId, string jwtToken);
    Task<GameDto?> GetGameAsync(Guid gameId, string jwtToken);
    Task<PaymentFuncDto?> SendToPayment(Payment payment, GameDto game);
}