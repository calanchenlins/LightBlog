using System;
using System.Collections.Generic;
using System.Text;

namespace K.Basis.Domain.Entities
{
    /// <summary>
    /// 实体接口
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public interface IEntity<TPrimaryKey>
    {
        /// <summary>
        /// 实体唯一主键
        /// </summary>
        TPrimaryKey Id { get; set; }
    }
}
