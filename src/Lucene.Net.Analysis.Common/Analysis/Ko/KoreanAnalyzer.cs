// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Ko.Dict;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Ko
{
    public class KoreanAnalyzer: Analyzer
    {
        private readonly UserDictionary userDict;
        private readonly KoreanTokenizer.DecompoundMode mode;
        private readonly HashSet<POS.Tag> stopTags;
        private readonly bool outputUnknownUnigrams;


        public KoreanAnalyzer()
            : this(null, KoreanTokenizer.DEFAULT_DECOMPOUND, KoreanPartOfSpeechStopFilter.DEFAULT_STOP_TAGS, false)
        {
        }

        public KoreanAnalyzer(UserDictionary userDict, KoreanTokenizer.DecompoundMode mode, HashSet<POS.Tag> stopTags, bool outputUnknownUnigrams)
            : base()
        {
            this.userDict = userDict;
            this.mode = mode;
            this.stopTags = stopTags;
            this.outputUnknownUnigrams = outputUnknownUnigrams;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new KoreanTokenizer(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, userDict, mode, outputUnknownUnigrams);
            TokenStream stream = new KoreanPartOfSpeechStopFilter(LuceneVersion.LUCENE_48, tokenizer, stopTags);
            stream = new KoreanReadingFormFilter(stream);
            stream = new LowerCaseFilter(LuceneVersion.LUCENE_48, stream);
            return new TokenStreamComponents(tokenizer, stream);
        }
    }
}