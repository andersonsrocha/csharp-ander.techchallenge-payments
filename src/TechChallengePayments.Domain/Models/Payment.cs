using TechChallengePayments.Domain.Enums;

namespace TechChallengePayments.Domain.Models;

public sealed class Payment(Guid userId, Guid gameId) : Entity
{
    public Guid UserId { get; private set; } = userId;
    public Guid GameId { get; private set; } = gameId;
    public Status Status { get; private set; } = Status.Pending;

    public void Update(Status status)
    {
        Status = status;
    }
}