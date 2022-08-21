using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;

namespace K.DataAnnotations
{
    /// <summary>
    /// CheckConstraint validation attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class CheckConstraintInAttribute : ValidationAttribute
    {
        private readonly object[] _validValues;

        /// <summary>
        /// 初始化 <see cref="CheckConstraintInAttribute"/> 类的实例.
        /// </summary>
        /// <param name="validValues"></param>
        public CheckConstraintInAttribute(params object[] validValues)
            : base(@"The field {0} conflicted with the  CheckConstraint.")
        {
            _validValues = validValues ?? throw new ArgumentNullException(nameof(validValues));
        }

        /// <summary>
        /// 格式化字段验证失败时的错误消息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string FormatErrorMessage(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);
        }

        /// <summary>
        /// 使用 Equals 方法判断字段是否合法, 非基础类型需要重写 Equals 方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool IsValid(object value)
        {
            return _validValues.Any(v => v.Equals(value));
        }
    }
}
