using TechChallengePayments.Domain.Dto;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Application;

public interface IClient
{
    Task<PaymentFuncDto?> SendToPayment(Payment payment, double price);
}