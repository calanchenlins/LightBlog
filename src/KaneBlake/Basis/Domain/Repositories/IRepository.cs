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
    /// <typeparam name="TEntity">实体类</typeparam>
    /// <typeparam name="TPrimaryKey">主键类型</typeparam>
    public interface IRepository<TEntity, TPrimaryKey> : IUnitOfWork where TEntity : IEntity<TPrimaryKey>
    {
        /// <summary>
        /// 返回 IQueryable 查询
        /// </summary>
        /// <returns></returns>
        IQueryable<TEntity> Get();

        /// <summary>
        /// 根据主键查找实体
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        TEntity Get(TPrimaryKey key);

        /// <summary>
        /// 增加实体
        /// </summary>
        /// <param name="entity"></param>
        void Add(TEntity entity);

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        /// <param name="ID"></param>
        void Remove(TPrimaryKey ID);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="entity"></param>
        void Remove(TEntity entity);

        /// <summary>
        /// 获取实体数量
        /// </summary>
        /// <returns></returns>
        int Count();
    }
}
