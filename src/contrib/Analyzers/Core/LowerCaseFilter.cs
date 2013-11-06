using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class LowerCaseFilter : TokenFilter
    {
        private readonly CharacterUtils charUtils;
        private readonly ICharTermAttribute termAtt;

        public LowerCaseFilter(Version? matchVersion, TokenStream input)
            : base(input)
        {
            charUtils = CharacterUtils.GetInstance(matchVersion);
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                charUtils.ToLowerCase(termAtt.Buffer, 0, termAtt.Length);
                return true;
            }
            else
                return false;
        }
    }
}
