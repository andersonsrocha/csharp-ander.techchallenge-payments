using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TechChallengePayments.Application.Payments.Commands;
using TechChallengePayments.Application.Payments.Interfaces;

namespace TechChallengePayments.Application.Consumer.Handlers;

public class ConsumerService(ILogger<ConsumerService> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    private readonly string _hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
    private readonly string _userName = configuration["RabbitMQ:UserName"] ?? "admin";
    private readonly string _password = configuration["RabbitMQ:Password"] ?? "";
    private readonly string _queueName = configuration["RabbitMQ:QueueName"] ?? "queue";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consuming queue: {GameId}", _queueName);
        logger.LogInformation("Creating a RabbitMQ Connection with HostName = {HostName}, UserName = {UserName}, Password = {Password}, QueueName = {QueueName}", _hostName, _userName, _password, _queueName);
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password,
        };
        
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(null, stoppingToken);
        
        await channel.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        
        var consumer = new AsyncEventingBasicConsumer(channel);
    
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            logger.LogInformation("Received purchase request: {Message}", message);
                
            using var scope = serviceProvider.CreateScope();
            var request = JsonSerializer.Deserialize<CreatePaymentRequest>(message);
            if (request is null)
            {
                logger.LogInformation("Received null request");
                return;
            }
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var response = await paymentService.CreateAsync(request);
            if (response.IsSuccess)
                logger.LogInformation("Payment processed successfully for PaymentId: {PurchaseId}", response.Value);
            else
                logger.LogInformation("Payment processed failed");
        };

        await channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
    }
}