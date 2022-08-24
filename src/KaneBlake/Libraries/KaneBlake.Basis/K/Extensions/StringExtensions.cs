using System;
using System.Collections.Generic;
using System.Text;

namespace K.Extensions
{
    public static class StringExtensions
    {
        public static bool TrimEquals(this string left, string right)
        {
            if (right == null)
            {
                return false;
            }
            if (left.Length == right.Length)
            {
                return left.Equals(right, StringComparison.Ordinal);
            }
            return left.TrimEnd().Equals(right.TrimEnd(), StringComparison.Ordinal);
        }
    }
}
