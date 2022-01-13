using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Domain.Entities
{
    /// <summary>
    /// 聚合根抽象类
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public abstract class AggregateRoot<TPrimaryKey> : IAggregateRoot<TPrimaryKey>
    {
        public virtual TPrimaryKey Id { get; set; }
    }
}
