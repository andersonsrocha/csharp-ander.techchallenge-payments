using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Application.Payments.Commands;

public class CreatePaymentRequest
{
    public Guid UserId { get; init; } = Guid.Empty;
    public Guid GameId { get; init; } = Guid.Empty;
    public double Price { get; init; } = 0;
    
    public static implicit operator Payment(CreatePaymentRequest request)
    {
        return new Payment(request.UserId, request.GameId);
    }
}