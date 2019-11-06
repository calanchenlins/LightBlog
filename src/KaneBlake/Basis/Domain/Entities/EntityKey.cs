using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Domain.Entities
{
    /// <summary>
    /// 重写实体 ==运算符、Equals、GetHashCode()、ToString()方法
    /// </summary>
    public abstract class EntityKey
    {
        private readonly PropertyInfo[] propertyInfos;
        public EntityKey()
        {
            propertyInfos = this.GetType().GetProperties();
        }
        /// <summary>
        /// 在没有重写 == 运算符的情况下，调用 OpCodes.Ceq 指令比较堆栈中的值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(EntityKey a, EntityKey b)
        {
            if (a.Equals(b))
            {
                return true;
            }
            return false;
        }
        public static bool operator !=(EntityKey a, EntityKey b)
        {
            if (!a.Equals(b))
            {
                return true;
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            foreach (var prop in propertyInfos)
            {
                var propType = prop.PropertyType;
                if (!prop.GetValue(this).Equals(prop.GetValue(obj)))
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            var str = new StringBuilder(); ;
            foreach (var prop in propertyInfos)
            {
                var propType = prop.PropertyType;
                str.AppendLine(prop.GetValue(this).ToString());
            }
            return str.ToString().GetHashCode();
        }
        public override string ToString()
        {
            var str = new StringBuilder(); ;
            foreach (var prop in propertyInfos)
            {
                var propType = prop.PropertyType;
                str.AppendLine(prop.GetValue(this).ToString());
            }
            return str.ToString();
        }
    }
}
