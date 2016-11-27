using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Search.Highlight
{
    public sealed class OffsetLimitTokenFilter : TokenFilter
    {
        private int offsetCount;
        private readonly IOffsetAttribute offsetAttrib;
        private readonly int offsetLimit;

        public OffsetLimitTokenFilter(TokenStream input, int offsetLimit) : base(input)
        {
            this.offsetLimit = offsetLimit;
            offsetAttrib = GetAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {
            if (offsetCount < offsetLimit && input.IncrementToken())
            {
                int offsetLength = offsetAttrib.EndOffset() - offsetAttrib.StartOffset();
                offsetCount += offsetLength;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            offsetCount = 0;
        }

    }
}
