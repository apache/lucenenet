using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
    /// A filter to apply normal capitalization rules to Tokens.  It will make the first letter capital and the rest lower case.
    /// This filter is particularly useful to build nice looking facet parameters.  This filter is not appropriate if you intend to use a prefix query.
    /// </summary>
    public sealed class CapitalizationFilter : TokenFilter
    {
        public static readonly int DEFAULT_MAX_WORD_COUNT = int.MaxValue;
        public static readonly int DEFAULT_MAX_TOKEN_LENGTH = int.MaxValue;

        private readonly bool onlyFirstWord;
        private readonly CharArraySet keep;
        private readonly bool forceFirstLetter;
        private readonly ICollection<char[]> okPrefix;

        private readonly int minWordLength;
        private readonly int maxWordCount;
        private readonly int maxTokenLength;

        private readonly ICharTermAttribute termAtt;

        public CapitalizationFilter(TokenStream input)
            : this(input, true, null, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }

        public CapitalizationFilter(TokenStream input, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter,
            ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength) : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            this.onlyFirstWord = onlyFirstWord;
            this.keep = keep;
            this.forceFirstLetter = forceFirstLetter;
            this.okPrefix = okPrefix;
            this.minWordLength = minWordLength;
            this.maxWordCount = maxWordCount;
            this.maxTokenLength = maxTokenLength;
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken()) return false;

            char[] termBuffer = termAtt.Buffer;
            int termBufferLength = termAtt.Length;
            char[] backup = null;

            if (maxWordCount < DEFAULT_MAX_WORD_COUNT)
            {
                //make a backup in case we exceed the word count
                backup = new char[termBufferLength];
                Array.Copy(termBuffer, 0, backup, 0, termBufferLength);
            }

            if (termBufferLength < maxTokenLength)
            {
                int wordCount = 0;
                int lastWordStart = 0;
                for (int i = 0; i < termBufferLength; i++)
                {
                    char c = termBuffer[i];
                    if (c <= ' ' || c == '.')
                    {
                        int len = i - lastWordStart;
                        if (len > 0)
                        {
                            ProcessWord(termBuffer, lastWordStart, len, wordCount++);
                            lastWordStart = i + 1;
                            i++;
                        }
                    }
                }

                // process the last word
                if (lastWordStart < termBufferLength)
                {
                    ProcessWord(termBuffer, lastWordStart, termBufferLength - lastWordStart, wordCount++);
                }

                if (wordCount > maxWordCount)
                {
                    termAtt.CopyBuffer(backup, 0, termBufferLength);
                }
            }
            return true;
        }

        private void ProcessWord(char[] buffer, int offset, int length, int wordCount)
        {
            if (length < 1)
            {
                return;
            }

            if (onlyFirstWord && wordCount > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    buffer[offset + i] = char.ToLower(buffer[offset + i]);

                }
                return;
            }

            if (keep != null && keep.Contains(buffer, offset, length))
            {
                if (wordCount == 0 && forceFirstLetter)
                {
                    buffer[offset] = char.ToUpper(buffer[offset]);
                }
                return;
            }

            if (length < minWordLength)
            {
                return;
            }

            if (okPrefix != null)
            {
                foreach (char[] prefix in okPrefix)
                {
                    if (length >= prefix.Length)
                    {
                        //don't bother checking if the buffer length is less than the prefix
                        bool match = true;
                        for (int i = 0; i < prefix.Length; i++)
                        {
                            if (prefix[i] != buffer[offset + i])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match == true)
                        {
                            return;
                        }
                    }
                }
            }
            // We know it has at least one character
            /*char[] chars = w.toCharArray();
            StringBuilder word = new StringBuilder( w.length() );
            word.append( Character.toUpperCase( chars[0] ) );*/
            buffer[offset] = char.ToUpper(buffer[offset]);

            for (int i = 1; i < length; i++)
            {
                buffer[offset + i] = char.ToLower(buffer[offset + i]);
            }
        }
    }
}
