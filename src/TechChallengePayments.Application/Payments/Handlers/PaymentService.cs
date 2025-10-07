using OperationResult;
using TechChallengePayments.Application.Payments.Commands;
using TechChallengePayments.Application.Payments.Interfaces;
using TechChallengePayments.Domain.Dto;
using TechChallengePayments.Domain.Interfaces;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Application.Payments.Handlers;

public class PaymentService(IPaymentRepository repository, IUnitOfWork unitOfWork) : IPaymentService
{
    public PaymentDto? Find(Guid id)
    {
        var payment = repository.Find(id);
        if (payment is null)
            return null;

        return new PaymentDto(payment.Id, payment.Active, payment.CreatedIn, payment.UpdatedIn, payment.UserId, payment.GameId, payment.Status);
    }

    public IEnumerable<PaymentDto> Find()
    {
        var payments = repository.Find();
        return payments.Select(payment => new PaymentDto(payment.Id, payment.Active, payment.CreatedIn, payment.UpdatedIn, payment.UserId, payment.GameId, payment.Status));
    }

    public async Task<Result<Guid>> CreateAsync(CreatePaymentRequest request)
    {
        var payment = (Payment)request;
        repository.Add(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        
        return Result.Success(payment.Id);
    }

    public async Task<Result> UpdateAsync(UpdatePaymentRequest request)
    {
        var payment = repository.Find(request.Id);
        if (payment is null)
            return Result.Error(new Exception("Payment not found."));
        
        payment.Update(request.Status);
        repository.Update(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var payment = repository.Find(id);
        if (payment is null)
            return Result.Error(new Exception("Payment not found."));
        
        if (!payment.Active)
            return Result.Error(new Exception("Payment has been deleted."));
        
        payment.MarkAsDeleted();
        repository.Update(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        
        return Result.Success();
    }
}