using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class StopFilter : FilteringTokenFilter
    {
        private readonly CharArraySet stopWords;
        private readonly ICharTermAttribute termAtt; // = addAttribute(CharTermAttribute.class);

        public StopFilter(Version? matchVersion, TokenStream input, CharArraySet stopWords)
            : base(true, input)
        {
            this.stopWords = stopWords;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public static CharArraySet MakeStopSet(Version? matchVersion, params String[] stopWords)
        {
            return MakeStopSet(matchVersion, stopWords, false);
        }

        public static CharArraySet MakeStopSet(Version? matchVersion, List<object> stopWords)
        {
            return MakeStopSet(matchVersion, stopWords, false);
        }

        public static CharArraySet MakeStopSet(Version? matchVersion, String[] stopWords, bool ignoreCase)
        {
            CharArraySet stopSet = new CharArraySet(matchVersion, stopWords.Length, ignoreCase);
            stopSet.AddAll(stopWords);
            return stopSet;
        }

        public static CharArraySet MakeStopSet(Version? matchVersion, List<object> stopWords, bool ignoreCase)
        {
            CharArraySet stopSet = new CharArraySet(matchVersion, stopWords.Count, ignoreCase);
            stopSet.AddAll(stopWords);
            return stopSet;
        }

        protected override bool Accept()
        {
            return !stopWords.Contains(termAtt.Buffer, 0, termAtt.Length);
        }
    }
}
