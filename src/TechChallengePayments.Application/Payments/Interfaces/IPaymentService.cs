using OperationResult;
using TechChallengePayments.Application.Payments.Commands;
using TechChallengePayments.Domain.Dto;

namespace TechChallengePayments.Application.Payments.Interfaces;

public interface IPaymentService : IService
{
    PaymentDto? Find(Guid id);
    IEnumerable<PaymentDto> Find();
    Task<Result<Guid>> CreateAsync(CreatePaymentRequest request);
    Task<Result> UpdateAsync(UpdatePaymentRequest request);
    Task<Result> DeleteAsync(Guid id);
}