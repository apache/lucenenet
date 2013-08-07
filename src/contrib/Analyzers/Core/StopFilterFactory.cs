using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class StopFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private CharArraySet stopWords;
        private readonly String stopWordFiles;
        private readonly String format;
        private readonly bool ignoreCase;
        private readonly bool enablePositionIncrements;

        public StopFilterFactory(IDictionary<String, String> args)
            : base(args)
        {
            AssureMatchVersion();
            stopWordFiles = Get(args, "words");
            format = Get(args, "format");
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", false);
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public void Inform(IResourceLoader loader)
        {
            if (stopWordFiles != null)
            {
                if ("snowball".EqualsIgnoreCase(format))
                {
                    stopWords = GetSnowballWordSet(loader, stopWordFiles, ignoreCase);
                }
                else
                {
                    stopWords = GetWordSet(loader, stopWordFiles, ignoreCase);
                }
            }
            else
            {
                stopWords = new CharArraySet(luceneMatchVersion, StopAnalyzer.ENGLISH_STOP_WORDS_SET, ignoreCase);
            }
        }

        public bool IsEnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
        }

        public bool IsIgnoreCase
        {
            get
            {
                return ignoreCase;
            }
        }

        public CharArraySet StopWords
        {
            get
            {
                return stopWords;
            }
        }
        
        public override TokenStream Create(TokenStream input)
        {
            StopFilter stopFilter = new StopFilter(luceneMatchVersion, input, stopWords);
            stopFilter.EnablePositionIncrements = enablePositionIncrements;
            return stopFilter;
        }
    }
}
