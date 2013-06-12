using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public static class MathUtil
    {
        public static int Log(long x, int @base)
        {
            if (@base <= 1)
            {
                throw new ArgumentException("base must be > 1");
            }
            int ret = 0;
            while (x >= @base)
            {
                x /= @base;
                ret++;
            }
            return ret;
        }
    }
}
