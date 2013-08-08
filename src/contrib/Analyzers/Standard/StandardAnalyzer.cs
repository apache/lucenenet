using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Standard
{
    public sealed class StandardAnalyzer : StopwordAnalyzerBase
    {
        public const int DEFAULT_MAX_TOKEN_LENGTH = 255;

        private int maxTokenLength = DEFAULT_MAX_TOKEN_LENGTH;

        public static readonly CharArraySet STOP_WORDS_SET = StopAnalyzer.ENGLISH_STOP_WORDS_SET;

        public StandardAnalyzer(Version? matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        public StandardAnalyzer(Version? matchVersion)
            : this(matchVersion, STOP_WORDS_SET)
        {
        }

        public StandardAnalyzer(Version? matchVersion, TextReader stopwords)
            : this(matchVersion, LoadStopwordSet(stopwords, matchVersion))
        {
        }

        public int MaxTokenLength
        {
            get { return maxTokenLength; }
            set { maxTokenLength = value; }
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            StandardTokenizer src = new StandardTokenizer(matchVersion, reader);
            src.MaxTokenLength = maxTokenLength;
            TokenStream tok = new StandardFilter(matchVersion, src);
            tok = new LowerCaseFilter(matchVersion, tok);
            tok = new StopFilter(matchVersion, tok, stopwords);
            return new AnonymousTokenStreamComponents(this, src, tok);
        }

        private sealed class AnonymousTokenStreamComponents : TokenStreamComponents
        {
            private readonly StandardTokenizer src;
            private readonly StandardAnalyzer parent;

            public AnonymousTokenStreamComponents(StandardAnalyzer parent, StandardTokenizer src, TokenStream tok)
                : base(src, tok)
            {
                this.parent = parent;
                this.src = src;
            }

            public override void SetReader(TextReader reader)
            {
                src.MaxTokenLength = parent.maxTokenLength;
                base.SetReader(reader);
            }
        }
    }
}
