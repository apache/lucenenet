using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class WhitespaceTokenizer : CharTokenizer
    {
        public WhitespaceTokenizer(Version? matchVersion, TextReader input)
            : base(matchVersion, input)
        {
        }

        public WhitespaceTokenizer(Version? matchVersion, AttributeFactory factory, TextReader input)
            : base(matchVersion, factory, input)
        {
        }

        protected override bool IsTokenChar(int c)
        {
            return !char.IsWhiteSpace((char)c);
        }
    }
}
