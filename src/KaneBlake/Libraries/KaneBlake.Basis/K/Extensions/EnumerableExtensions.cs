﻿using System;
using System.Collections.Generic;
using System.Text;

namespace K.Extensions
{
    /// <summary>
    /// https://github.com/morelinq/MoreLINQ
    /// </summary>
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Distinct<T, K>(this IEnumerable<T> source, Func<T, K> predicate)
        {
            HashSet<K> sets = new HashSet<K>();
            foreach (var item in source)
            {
                if (sets.Add(predicate(item)))
                {
                    yield return item;
                }
            }
        }
    }
}
