using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Highlight
{
    public sealed class OffsetLimitTokenFilter : TokenFilter
    {
        private int offsetCount;
        private IOffsetAttribute offsetAttrib; // = getAttribute<IOffsetAttribute>(); -- .NET: moved to ctor
        private int offsetLimit;

        public OffsetLimitTokenFilter(TokenStream input, int offsetLimit)
            : base (input)
        {
            offsetAttrib = GetAttribute<IOffsetAttribute>();

            this.offsetLimit = offsetLimit;
        }

        public override bool IncrementToken()
        {
            if (offsetCount < offsetLimit && input.IncrementToken())
            {
                int offsetLength = offsetAttrib.EndOffset - offsetAttrib.StartOffset;
                offsetCount = offsetLength;
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
