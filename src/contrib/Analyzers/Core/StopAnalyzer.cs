using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Core
{
    public sealed class StopAnalyzer : StopwordAnalyzerBase
    {
        public static readonly CharArraySet ENGLISH_STOP_WORDS_SET;

        static StopAnalyzer()
        {
            string[] stopWords = new string[] {
              "a", "an", "and", "are", "as", "at", "be", "but", "by",
              "for", "if", "in", "into", "is", "it",
              "no", "not", "of", "on", "or", "such",
              "that", "the", "their", "then", "there", "these",
              "they", "this", "to", "was", "will", "with"
            };
            CharArraySet stopSet = new CharArraySet(Version.LUCENE_CURRENT, stopWords, false);
            ENGLISH_STOP_WORDS_SET = CharArraySet.UnmodifiableSet(stopSet);
        }

        public StopAnalyzer(Version? matchVersion)
            : this(matchVersion, ENGLISH_STOP_WORDS_SET)
        {
        }

        public StopAnalyzer(Version? matchVersion, CharArraySet stopWords)
            : base(matchVersion, stopWords)
        {
        }

        public StopAnalyzer(Version? matchVersion, Stream stopwordsFile)
            : this(matchVersion, LoadStopwordSet(stopwordsFile, matchVersion))
        {
        }

        public StopAnalyzer(Version? matchVersion, TextReader stopwords)
            : this(matchVersion, LoadStopwordSet(stopwords, matchVersion))
        {
        }

        public override Analyzer.TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new LowerCaseTokenizer(matchVersion, reader);
            return new TokenStreamComponents(source, new StopFilter(matchVersion,
                  source, stopwords));
        }
    }
}
