using TechChallengePayments.Domain.Interfaces;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Data.Repositories;

public class PaymentRepository(TechChallengePaymentsContext context) : Repository<Payment>(context), IPaymentRepository;