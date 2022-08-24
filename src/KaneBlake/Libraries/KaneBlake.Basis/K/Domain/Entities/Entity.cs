using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace K.Domain.Entities
{
    /// <summary>
    /// 实体抽象类
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public abstract class Entity<TPrimaryKey>: IEntity<TPrimaryKey>
    {
        [Required]
        public virtual TPrimaryKey Id { get; set; }
    }
}
