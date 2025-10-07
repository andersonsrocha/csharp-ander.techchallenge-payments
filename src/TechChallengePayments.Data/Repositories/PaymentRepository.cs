using Microsoft.EntityFrameworkCore;
using TechChallengePayments.Domain.Interfaces;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Data.Repositories;

public class PaymentRepository(TechChallengePaymentsContext context) : Repository<Payment>(context), IPaymentRepository
{
    public Payment? FindUserAndGame(Guid userId, Guid gameId)
    {
        return _dbSet.AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.GameId == gameId);
    }
}