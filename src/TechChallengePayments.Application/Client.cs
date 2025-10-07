using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TechChallengePayments.Domain.Dto;

namespace TechChallengePayments.Application
{
    public class Client(HttpClient httpClient, IConfiguration configuration) : IClient
    {
        private readonly string? _gameUrl = configuration["Microservices:GameUrl"];
        private readonly string? _userUrl = configuration["Microservices:UserUrl"];
        private readonly string? _email = configuration["Credentials:Email"];
        private readonly string? _password = configuration["Credentials:Password"];
        
        public async Task<LoginDto?> LoginAsync()
        {
            var response = await httpClient.PostAsJsonAsync($"{_userUrl}/api/users/signin", new { Email = _email, Password = _password });
            if (!response.IsSuccessStatusCode) return null;
            var loginDto = await response.Content.ReadFromJsonAsync<LoginDto>();
            return loginDto;
        }

        public async Task<UserDto?> GetUserAsync(Guid userId, string jwtToken)
        {
            if (!httpClient.DefaultRequestHeaders.Contains("Authorization"))
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
            
            var response = await httpClient.GetFromJsonAsync<UserDto>($"{_userUrl}/api/users/{userId}");
            return response;
        }

        public async Task<GameDto?> GetGameAsync(Guid gameId, string jwtToken)
        {
            if (!httpClient.DefaultRequestHeaders.Contains("Authorization"))
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
            
            var response = await httpClient.GetFromJsonAsync<GameDto>($"{_gameUrl}/api/games/{gameId}");
            return response;
        }
    }
}

