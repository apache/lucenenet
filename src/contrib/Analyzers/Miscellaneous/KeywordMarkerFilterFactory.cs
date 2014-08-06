using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class KeywordMarkerFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public static readonly String PROTECTED_TOKENS = "protected";
        public static readonly String PATTERN = "pattern";
        private readonly String wordFiles;
        private readonly String stringPattern;
        private readonly bool ignoreCase;

        private Regex pattern;
        private CharArraySet protectedWords;

        public KeywordMarkerFilterFactory(IDictionary<string, string> args) : base(args)
        {
            wordFiles = Get(args, PROTECTED_TOKENS);
            stringPattern = Get(args, PATTERN);
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            if (pattern != null)
            {
                input = new PatternKeywordMarkerFilter(input, pattern);
            }
            if (protectedWords != null)
            {
                input = new SetKeywordMarkerFilter(input, protectedWords);
            }
            return input;
        }

        public void Inform(IResourceLoader loader)
        {
            if (wordFiles != null)
            {
                protectedWords = GetWordSet(loader, wordFiles, ignoreCase);
            }
            if (stringPattern != null)
            {
                pattern = ignoreCase ? new Regex(stringPattern, RegexOptions.IgnoreCase) : new Regex(stringPattern);
            }
        }
    }
}
