// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Standard
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A grammar-based tokenizer constructed with JFlex (and then ported to .NET)
    /// 
    /// <para> This should be a good tokenizer for most European-language documents:
    /// 
    /// <list type="bullet">
    ///     <item><description>Splits words at punctuation characters, removing punctuation. However, a 
    ///         dot that's not followed by whitespace is considered part of a token.</description></item>
    ///     <item><description>Splits words at hyphens, unless there's a number in the token, in which case
    ///         the whole token is interpreted as a product number and is not split.</description></item>
    ///     <item><description>Recognizes email addresses and internet hostnames as one token.</description></item>
    /// </list>
    /// 
    /// </para>
    /// <para>Many applications have specific tokenizer needs.  If this tokenizer does
    /// not suit your application, please consider copying this source code
    /// directory to your project and maintaining your own grammar-based tokenizer.
    /// 
    /// <see cref="ClassicTokenizer"/> was named <see cref="StandardTokenizer"/> in Lucene versions prior to 3.1.
    /// As of 3.1, <see cref="StandardTokenizer"/> implements Unicode text segmentation,
    /// as specified by UAX#29.
    /// </para>
    /// </summary>
    public sealed class ClassicTokenizer : Tokenizer
    {
        /// <summary>
        /// A private instance of the JFlex-constructed scanner </summary>
        private IStandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        public const int APOSTROPHE = 1;
        public const int ACRONYM = 2;
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        public const int HOST = 5;
        public const int NUM = 6;
        public const int CJ = 7;

        public const int ACRONYM_DEP = 8;

        /// <summary>
        /// String token types that correspond to token type int constants </summary>
        public static readonly string[] TOKEN_TYPES = new string[] {
            "<ALPHANUM>",
            "<APOSTROPHE>",
            "<ACRONYM>",
            "<COMPANY>",
            "<EMAIL>",
            "<HOST>",
            "<NUM>",
            "<CJ>",
            "<ACRONYM_DEP>"
        };

        private int skippedPositions;

        private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// Set the max allowed token length.  Any token longer
        ///  than this is skipped. 
        /// </summary>
        public int MaxTokenLength
        {
            get => maxTokenLength;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxTokenLength), "maxTokenLength must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

                this.maxTokenLength = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ClassicTokenizer"/>.  Attaches
        /// the <paramref name="input"/> to the newly created JFlex scanner.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="input"> The input reader
        /// 
        /// See http://issues.apache.org/jira/browse/LUCENE-1068 </param>
        public ClassicTokenizer(LuceneVersion matchVersion, Reader input)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new <see cref="ClassicTokenizer"/> with a given <see cref="AttributeSource.AttributeFactory"/> 
        /// </summary>
        public ClassicTokenizer(LuceneVersion matchVersion, AttributeFactory factory, Reader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        private void Init(LuceneVersion matchVersion)
        {
            this.scanner = new ClassicTokenizerImpl(m_input);
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private ITypeAttribute typeAtt;
        
        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.analysis.TokenStream#next()
         */
        public override sealed bool IncrementToken()
        {
            ClearAttributes();
            skippedPositions = 0;

            while (true)
            {
                int tokenType = scanner.GetNextToken();

                if (tokenType == StandardTokenizerInterface.YYEOF)
                {
                    return false;
                }

                if (scanner.YyLength <= maxTokenLength)
                {
                    posIncrAtt.PositionIncrement = skippedPositions + 1;
                    scanner.GetText(termAtt);

                    int start = scanner.YyChar;
                    offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + termAtt.Length));

                    if (tokenType == ClassicTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.HOST];
                        termAtt.Length = termAtt.Length - 1; // remove extra '.'
                    }
                    else
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[tokenType];
                    }
                    return true;
                }
                else
                // When we skip a too-long term, we still increment the
                // position increment
                {
                    skippedPositions++;
                }
            }
        }

        public override sealed void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(scanner.YyChar + scanner.YyLength);
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // adjust any skipped tokens
            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                scanner.YyReset(m_input);
            }
        }

        public override void Reset()
        {
            base.Reset();
            scanner.YyReset(m_input);
            skippedPositions = 0;
        }
    }
}