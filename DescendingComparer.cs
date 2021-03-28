using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PrScraper
{
    internal sealed class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
    {
        public int Compare([AllowNull] T x, [AllowNull] T y)
        {
            if (y is null)
            {
                return x is null ? 0 : -1;
            }

            if (x is null)
            {
                return 1;
            }

            return y.CompareTo(x);
        }
    }
}
