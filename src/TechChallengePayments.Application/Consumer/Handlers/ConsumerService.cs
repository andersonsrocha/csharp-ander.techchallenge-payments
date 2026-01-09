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
        logger.LogInformation("Starting RabbitMQ consumer service");
        logger.LogInformation("Consuming queue: {QueueName}", _queueName);
        logger.LogInformation("Connection details - HostName: {HostName}, UserName: {UserName}, QueueName: {QueueName}", _hostName, _userName, _queueName);
        
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password,
            Port = 5672,
            VirtualHost = "/",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        logger.LogInformation("ConnectionFactory configured with AutoRecovery: {AutoRecovery}, Heartbeat: {Heartbeat}s", 
            factory.AutomaticRecoveryEnabled, factory.RequestedHeartbeat.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Attempting to connect to RabbitMQ at {HostName}:{Port}...", _hostName, factory.Port);
                
                using var connection = await factory.CreateConnectionAsync(stoppingToken);
                
                logger.LogInformation("✅ Successfully connected to RabbitMQ. Connection state: {IsOpen}", connection.IsOpen);
                
                // Connection event handlers
                connection.ConnectionShutdownAsync += async (sender, args) =>
                {
                    logger.LogWarning("🔌 RabbitMQ connection shutdown: {Reason} (Initiator: {Initiator})", 
                        args.ReplyText, args.Initiator);
                };

                connection.ConnectionBlockedAsync += async (sender, args) =>
                {
                    logger.LogWarning("🚫 RabbitMQ connection blocked: {Reason}", args.Reason);
                };

                connection.ConnectionUnblockedAsync += async (sender, args) =>
                {
                    logger.LogInformation("✅ RabbitMQ connection unblocked");
                };
                
                try
                {
                    using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    logger.LogInformation("✅ Channel created successfully. Channel number: {ChannelNumber}", channel.ChannelNumber);
                    
                    logger.LogInformation("Declaring queue '{QueueName}' with durable=true...", _queueName);
                    await channel.QueueDeclareAsync(
                        queue: _queueName, 
                        durable: true, 
                        exclusive: false, 
                        autoDelete: false, 
                        arguments: null, 
                        cancellationToken: stoppingToken);
                    
                    logger.LogInformation("✅ Queue '{QueueName}' declared successfully", _queueName);
                    
                    var consumer = new AsyncEventingBasicConsumer(channel);
                
                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            
                            logger.LogInformation("📨 Received message: {Message} (DeliveryTag: {DeliveryTag})", 
                                message, ea.DeliveryTag);
                                
                            using var scope = serviceProvider.CreateScope();
                            var request = JsonSerializer.Deserialize<CreatePaymentRequest>(message);
                            if (request is null)
                            {
                                logger.LogWarning("⚠️ Received null request, skipping message");
                                return;
                            }
                            
                            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                            var response = await paymentService.CreateAsync(request);
                            
                            if (response.IsSuccess)
                                logger.LogInformation("✅ Payment processed successfully for PaymentId: {PaymentId}", response.Value);
                            else
                                logger.LogError("❌ Payment processing failed: {Error}", response.Exception?.Message);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "❌ Error processing message from queue");
                        }
                    };

                    logger.LogInformation("Setting up consumer for queue '{QueueName}'...", _queueName);
                    await channel.BasicConsumeAsync(
                        queue: _queueName, 
                        autoAck: true, 
                        consumer: consumer, 
                        cancellationToken: stoppingToken);
                    
                    logger.LogInformation("✅ Consumer started successfully for queue '{QueueName}'", _queueName);

                    // Keep connection alive
                    while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
                    {
                        await Task.Delay(2000, stoppingToken);
                    }
                    
                    if (!connection.IsOpen)
                    {
                        logger.LogWarning("🔌 RabbitMQ connection closed unexpectedly, will attempt to reconnect");
                    }
                }
                catch (Exception channelEx)
                {
                    logger.LogError(channelEx, "❌ Error creating/using RabbitMQ channel");
                    throw;
                }
            }
            catch (RabbitMQ.Client.Exceptions.AuthenticationFailureException authEx)
            {
                logger.LogError(authEx, "❌ Authentication failed: Invalid username/password");
                logger.LogError("Credentials used - UserName: '{UserName}', Password length: {PasswordLength}", 
                    _userName, _password?.Length ?? 0);
                await Task.Delay(10000, stoppingToken); // Wait longer for auth failures
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException brokerEx)
            {
                logger.LogError(brokerEx, "❌ Cannot reach RabbitMQ broker at {HostName}:{Port}", _hostName, factory.Port);
                await Task.Delay(5000, stoppingToken);
            }
            catch (System.Net.Sockets.SocketException socketEx)
            {
                logger.LogError(socketEx, "❌ Network error connecting to RabbitMQ: {Error} (ErrorCode: {ErrorCode})", 
                    socketEx.Message, socketEx.ErrorCode);
                await Task.Delay(5000, stoppingToken);
            }
            catch (TimeoutException timeoutEx)
            {
                logger.LogError(timeoutEx, "❌ Timeout connecting to RabbitMQ");
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Unexpected error in RabbitMQ consumer: {ExceptionType}", ex.GetType().Name);
                await Task.Delay(5000, stoppingToken);
            }
            
            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("🔄 Retrying RabbitMQ connection in 5 seconds...");
            }
        }
        
        logger.LogInformation("🛑 RabbitMQ consumer service stopped");
    }
}