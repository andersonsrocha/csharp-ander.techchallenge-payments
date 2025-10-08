using Microsoft.Extensions.Logging;
using OperationResult;
using TechChallengePayments.Application.Payments.Commands;
using TechChallengePayments.Application.Payments.Interfaces;
using TechChallengePayments.Domain.Dto;
using TechChallengePayments.Domain.Interfaces;
using TechChallengePayments.Domain.Models;
using TechChallengePayments.Elasticsearch;

namespace TechChallengePayments.Application.Payments.Handlers;

public class PaymentService(IPaymentRepository repository, IClient client, IElasticClient<PaymentLog> elastic, IUnitOfWork unitOfWork, ILogger<PaymentService> logger) : IPaymentService
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
        logger.LogInformation("Log in to the user service");
        var login = await client.LoginAsync();
        if (login is null)
            return Result.Error<Guid>(new Exception("Invalid credentials."));
        
        logger.LogInformation("Log in successful. Retrieving user information.");
        var user = await client.GetUserAsync(request.UserId, login.Token);
        if (user is null)
            return Result.Error<Guid>(new Exception("User not found."));

        logger.LogInformation("User successfully retrieved. Retrieving game information.");
        var game = await client.GetGameAsync(request.GameId, login.Token);
        if (game is null)
            return Result.Error<Guid>(new Exception("User not found."));
        
        logger.LogInformation("Game successfully retrieved. Verifying existing payment for this user and game.");
        var existingPayment = repository.FindUserAndGame(request.UserId, request.GameId);
        if (existingPayment is not null)
            return Result.Error<Guid>(new Exception("Payment already exists for this user and game."));
        
        logger.LogInformation("No existing payment found. Creating new payment.");
        var payment = (Payment)request;
        repository.Add(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        
        logger.LogInformation("Sending payment event to azure function.");
        if (await client.SendToPayment(payment, game) is null)
        {
            logger.LogError("Error sending payment to Azure Function.");
            return Result.Error<Guid>(new Exception("Error processing payment."));
        }
        
        logger.LogInformation("Payment successfully created. Saving payment log to Elasticsearch.");
        await elastic.AddOrUpdate(new PaymentLog(payment.Id, payment.UserId, payment.GameId), nameof(PaymentLog).ToLower());
        
        return Result.Success(payment.Id);
    }

    public async Task<Result> UpdateAsync(UpdatePaymentRequest request)
    {
        logger.LogInformation("Updating payment with ID: {PaymentId}", request.Id);
        var payment = repository.Find(request.Id);
        if (payment is null)
            return Result.Error(new Exception("Payment not found."));
        
        logger.LogInformation("Payment found. Updating status to: {Status}", request.Status);
        payment.Update(request.Status);
        repository.Update(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        logger.LogInformation("Payment successfully updated. Updating payment log in Elasticsearch.");
        await elastic.AddOrUpdate(new PaymentLog(payment.Id, payment.UserId, payment.GameId), nameof(PaymentLog).ToLower());
        
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        logger.LogInformation("Deleting payment with ID: {PaymentId}", id);
        var payment = repository.Find(id);
        if (payment is null)
            return Result.Error(new Exception("Payment not found."));
        
        logger.LogInformation("Payment found, check if it is already inactive.");
        if (!payment.Active)
            return Result.Error(new Exception("Payment has been deleted."));
        
        logger.LogInformation("Marking payment as deleted.");
        payment.MarkAsDeleted();
        repository.Update(payment);
        await unitOfWork.CommitAsync(CancellationToken.None);
        
        return Result.Success();
    }
}