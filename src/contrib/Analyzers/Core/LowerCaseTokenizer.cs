using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class LowerCaseTokenizer : LetterTokenizer
    {
        public LowerCaseTokenizer(Version? matchVersion, TextReader input)
            : base(matchVersion, input)
        {
        }

        public LowerCaseTokenizer(Version? matchVersion, AttributeFactory factory, TextReader input)
            : base(matchVersion, factory, input)
        {
        }

        protected override int Normalize(int c)
        {
            return (int)char.ToLower((char)c);
        }
    }
}
