using KaneBlake.Basis.Domain.Entities;
using KaneBlake.Basis.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Infrastruct.EntityFrameworkCore
{
    /// <summary>
    /// EntityFrameworkCore 对 IRepository 的实现
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public abstract class EFRepository<TEntity> : IRepository<TEntity, int> where TEntity : class, IEntity<int>
    {
        public virtual DbContext Context { get; private set; }

        public virtual DbSet<TEntity> Table => Context.Set<TEntity>();

        public EFRepository(DbContext dbContext)
        {
            Context = dbContext;
        }

        public virtual IQueryable<TEntity> Get()
        {
            return Table;
        }

        public TEntity Get(int key)
        {
            return Table.Where(x => x.Id == key).Single();
        }

        public void Add(TEntity entity)
        {
            Table.Add(entity);
        }

        public void Remove(int ID)
        {
            Table.Remove(Get(ID));
        }

        public void Remove(TEntity entity)
        {
            Table.Remove(entity);
        }

        public int Count()
        {
            return Table.Count();
        }

        public void Complete()
        {
            Context.SaveChanges();
        }

        public Task<int> CompleteAsync()
        {
            return Context.SaveChangesAsync();
        }

        private bool disposedValue = false;

        public void Dispose()
        {
            if (!disposedValue)
            {
                Context.Dispose();
            }
            disposedValue = true;
        }
    }
}
