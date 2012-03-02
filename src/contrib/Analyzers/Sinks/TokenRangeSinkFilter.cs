using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Sinks
{
    public class TokenRangeSinkFilter : TeeSinkTokenFilter.SinkFilter
    {
        private int lower;
        private int upper;
        private int count;

        public TokenRangeSinkFilter(int lower, int upper)
        {
            this.lower = lower;
            this.upper = upper;
        }

        public override bool Accept(AttributeSource source)
        {
            try
            {
                if (count >= lower && count < upper)
                {
                    return true;
                }
                return false;
            }
            finally
            {
                count++;
            }
        }

        public override void Reset()
        {
            count = 0;
        }
    }
}
