using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Payment? FindUserAndGame(Guid userId, Guid gameId);
}