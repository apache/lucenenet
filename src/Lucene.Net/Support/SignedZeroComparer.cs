using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    /// <summary>
    /// LUCENENET specific comparer to handle the special case
    /// of comparing negative zero with positive zero.
    /// <para/>
    /// For IEEE floating-point numbers, there is a distinction of negative and positive zero.
    /// Reference: http://stackoverflow.com/a/3139636
    /// </summary>
    public class SignedZeroComparer : IComparer<double>
    {
        public int Compare(double v1, double v2)
        {
            long a = BitConverter.DoubleToInt64Bits(v1);
            long b = BitConverter.DoubleToInt64Bits(v2);
            if (a > b)
            {
                return 1;
            }
            else if (a < b)
            {
                return -1;
            }

            return 0;
        }
    }
}
