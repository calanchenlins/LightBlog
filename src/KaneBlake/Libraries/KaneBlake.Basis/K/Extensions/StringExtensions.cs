using System;
using System.Collections.Generic;
using System.Text;

namespace K.Extensions
{
    public static class StringExtensions
    {
        public static bool TrimEquals(this string left, string right)
        {
            if (left.Length == (right?.Length ?? -1))
            {
                return left.Equals(right, StringComparison.OrdinalIgnoreCase);
            }
            return left.Trim().Equals(right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
