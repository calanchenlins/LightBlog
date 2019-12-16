using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Domain.Entities
{
    /// <summary>
    /// 聚合根接口
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public interface IAggregateRoot<TPrimaryKey> : IEntity<TPrimaryKey>
    {
    }
}
