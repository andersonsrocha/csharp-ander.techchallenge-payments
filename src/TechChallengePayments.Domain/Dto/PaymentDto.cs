using TechChallengePayments.Domain.Enums;

namespace TechChallengePayments.Domain.Dto;

public record PaymentDto(Guid Id, bool Active, DateTime CreatedIn, DateTime? UpdatedIn, Guid UserId, Guid GameId, Status Status);