using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TechChallengePayments.Domain.Dto;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Application
{
    public class Client(HttpClient httpClient, IConfiguration configuration) : IClient
    {
        private readonly string? _paymentFuncUrl = configuration["Microservices:PaymentFuncUrl"];
        
        public async Task<PaymentFuncDto?> SendToPayment(Payment payment, double price)
        {
            var response = await httpClient.PostAsJsonAsync($"{_paymentFuncUrl}/api/payment", new { payment.Id, payment.UserId, payment.GameId, price });
            if (!response.IsSuccessStatusCode) return null;
            var funcDto = await response.Content.ReadFromJsonAsync<PaymentFuncDto>();
            return funcDto;
        }
    }
}

