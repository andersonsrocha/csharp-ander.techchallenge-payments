using Microsoft.EntityFrameworkCore;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Data;

public class TechChallengePaymentsContext(DbContextOptions<TechChallengePaymentsContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TechChallengePaymentsContext).Assembly);
    }
}