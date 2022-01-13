using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Domain.Entities
{
    /// <summary>
    /// 实体抽象类
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public abstract class Entity<TPrimaryKey>: IEntity<TPrimaryKey>
    {
        [Required]
        public virtual TPrimaryKey Id { get; set; }

        //public TDestination Map<TDestination>() 
        //{
        //    System.Text.Json.JsonSerializer.Serialize(this);
        //}
    }
}
