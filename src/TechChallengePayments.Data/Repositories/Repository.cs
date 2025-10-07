using Microsoft.EntityFrameworkCore;
using TechChallengePayments.Domain.Interfaces;
using TechChallengePayments.Domain.Models;

namespace TechChallengePayments.Data.Repositories;

public abstract class Repository<TEntity>(TechChallengePaymentsContext usersContext) : IRepository<TEntity> where TEntity : Entity
{
    protected readonly DbSet<TEntity> _dbSet = usersContext.Set<TEntity>();
    
    public TEntity? Find(Guid id)
        => _dbSet.AsNoTracking().SingleOrDefault(x => x.Id == id);

    public IEnumerable<TEntity> Find()
        => _dbSet.AsNoTracking().ToList();

    public void Add(TEntity entity)
        => _dbSet.Add(entity);

    public void Update(TEntity entity)
        => _dbSet.Update(entity);

    public void Delete(TEntity entity)
        => _dbSet.Remove(entity);
}