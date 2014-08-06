using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class CapitalizationFilterFactory : TokenFilterFactory
    {
        public static readonly String KEEP = "keep";
        public static readonly String KEEP_IGNORE_CASE = "keepIgnoreCase";
        public static readonly String OK_PREFIX = "okPrefix";
        public static readonly String MIN_WORD_LENGTH = "minWordLength";
        public static readonly String MAX_WORD_COUNT = "maxWordCount";
        public static readonly String MAX_TOKEN_LENGTH = "maxTokenLength";
        public static readonly String ONLY_FIRST_WORD = "onlyFirstWord";
        public static readonly String FORCE_FIRST_LETTER = "forceFirstLetter";

        private CharArraySet keep;

        private Collection<char[]> okPrefix = new Collection<char[]>(); // for Example: McK

        private readonly int minWordLength; // don't modify capitalization for words shorter then this
        private readonly int maxWordCount;
        private readonly int maxTokenLength;
        private readonly bool onlyFirstWord;
        private readonly bool forceFirstLetter; // make sure the first letter is capital even if it is in the keep list

        /// <summary>
        /// Factory for CapitalizationFilter.
        /// </summary>
        /// <param name="args"></param>
        public CapitalizationFilterFactory(IDictionary<string, string> args) : base(args)
        {
            AssureMatchVersion();
            var ignoreCase = GetBoolean(args, KEEP_IGNORE_CASE, false);
            var k = GetSet(args, KEEP);
            if (k != null)
            {
                keep = new CharArraySet(luceneMatchVersion, 10, ignoreCase);
                keep.AddAll(k);
            }

            k = GetSet(args, OK_PREFIX);
            if (k != null)
            {
                okPrefix = new Collection<char[]>();
                foreach (String item in k)
                {
                    okPrefix.Add(item.ToCharArray());
                }
            }
            minWordLength = GetInt(args, MIN_WORD_LENGTH, 0);
            maxWordCount = GetInt(args, MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT);
            maxTokenLength = GetInt(args, MAX_TOKEN_LENGTH, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);
            onlyFirstWord = GetBoolean(args, ONLY_FIRST_WORD, true);
            forceFirstLetter = GetBoolean(args, FORCE_FIRST_LETTER, true);
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CapitalizationFilter(input, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
        }
    }
}
