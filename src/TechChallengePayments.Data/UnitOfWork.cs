using TechChallengePayments.Domain.Interfaces;

namespace TechChallengePayments.Data;

public sealed class UnitOfWork(TechChallengePaymentsContext usersContext) : IUnitOfWork
{
    public async Task<bool> CommitAsync(CancellationToken cancellationToken)
        => await usersContext.SaveChangesAsync(cancellationToken) > 0;
}