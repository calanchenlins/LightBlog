using KaneBlake.Basis.Domain.Entities;
using KaneBlake.Basis.Domain.Uow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Domain.Repositories
{

    /// <summary>
    /// 聚合根实现泛型仓储接口
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<TEntity, TPrimaryKey> : IUnitOfWork where TEntity : IEntity<TPrimaryKey>
    {
        IQueryable<TEntity> Get();

        TEntity Get(TPrimaryKey key);

        void Add(TEntity entity);

        void Remove(TPrimaryKey ID);

        void Remove(TEntity entity);

        int Count();
    }
}
