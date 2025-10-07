using TechChallengePayments.Domain.Enums;

namespace TechChallengePayments.Application.Payments.Commands;

public class UpdatePaymentRequest
{
    public Guid Id { get; init; }
    public Status Status { get; init; }
}