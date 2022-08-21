using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K.Globalization
{
    /// <summary>
    /// 数据格式约定
    /// </summary>
    public class Convention
    {
        /// <summary>
        /// 时间
        /// </summary>
        public const DateTimeStyles SupportedDateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
    }
}
