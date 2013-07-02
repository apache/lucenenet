using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class NumberExtensions
    {
        public static int Signum(this long i)
        {
            if (i == 0)
                return 0;

            return i > 0 ? 1 : -1;
        }
    }
}
