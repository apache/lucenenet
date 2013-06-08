using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary>
    /// In .NET, you can't declare static variables or nested types in interfaces like IBits.
    /// This is to match the behavior in the Bits interface in java lucene.
    /// </summary>
    public static class Bits
    {
        public static readonly IBits[] EMPTY_ARRAY = new IBits[0];

        public class MatchAllBits : IBits
        {
            internal readonly int len;

            public MatchAllBits(int len)
            {
                this.len = len;
            }

            public bool this[int index]
            {
                get { return true; }
            }

            public int Length
            {
                get { return len; }
            }
        }

        public class MatchNoBits : IBits
        {
            internal readonly int len;

            public MatchNoBits(int len)
            {
                this.len = len;
            }

            public bool this[int index]
            {
                get { return false; }
            }

            public int Length
            {
                get { return len; }
            }
        }
    }
}
