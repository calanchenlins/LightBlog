using System;
using System.Collections.Generic;
using System.Text;

namespace K.Domain.Entities
{
    /// <summary>
    /// 聚合根接口
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public interface IAggregateRoot<TPrimaryKey> : IEntity<TPrimaryKey>
    {
    }
}
