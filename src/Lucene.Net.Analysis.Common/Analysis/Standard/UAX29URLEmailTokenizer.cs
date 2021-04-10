// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard.Std31;
using Lucene.Net.Analysis.Standard.Std34;
using Lucene.Net.Analysis.Standard.Std36;
using Lucene.Net.Analysis.Standard.Std40;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.IO;

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
    /// This class implements Word Break rules from the Unicode Text Segmentation 
    /// algorithm, as specified in                 `
    /// <a href="http://unicode.org/reports/tr29/">Unicode Standard Annex #29</a> 
    /// URLs and email addresses are also tokenized according to the relevant RFCs.
    /// <para/>
    /// Tokens produced are of the following types:
    /// <list type="bullet">
    ///     <item><description>&lt;ALPHANUM&gt;: A sequence of alphabetic and numeric characters</description></item>
    ///     <item><description>&lt;NUM&gt;: A number</description></item>
    ///     <item><description>&lt;URL&gt;: A URL</description></item>
    ///     <item><description>&lt;EMAIL&gt;: An email address</description></item>
    ///     <item><description>&lt;SOUTHEAST_ASIAN&gt;: A sequence of characters from South and Southeast
    ///         Asian languages, including Thai, Lao, Myanmar, and Khmer</description></item>
    ///     <item><description>&lt;IDEOGRAPHIC&gt;: A single CJKV ideographic character</description></item>
    ///     <item><description>&lt;HIRAGANA&gt;: A single hiragana character</description></item>
    /// </list>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="UAX29URLEmailTokenizer"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///         from their combining characters. If you use a previous version number,
    ///         you get the exact broken behavior for backwards compatibility.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class UAX29URLEmailTokenizer : Tokenizer
    {
        /// <summary>
        /// A private instance of the JFlex-constructed scanner </summary>
        private IStandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        public const int NUM = 1;
        public const int SOUTHEAST_ASIAN = 2;
        public const int IDEOGRAPHIC = 3;
        public const int HIRAGANA = 4;
        public const int KATAKANA = 5;
        public const int HANGUL = 6;
        public const int URL = 7;
        public const int EMAIL = 8;

        /// <summary>
        /// String token types that correspond to token type int constants </summary>
        public static readonly string[] TOKEN_TYPES = new string[] {
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.ALPHANUM],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.NUM],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.SOUTHEAST_ASIAN],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA],
            StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL],
            "<URL>",
            "<EMAIL>"
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
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxTokenLength), "maxTokenLength must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.maxTokenLength = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="UAX29URLEmailTokenizer"/>.  Attaches
        /// the <paramref name="input"/> to the newly created JFlex scanner.
        /// </summary>
        /// <param name="matchVersion"> Lucene compatibility version </param>
        /// <param name="input"> The input reader </param>
        public UAX29URLEmailTokenizer(LuceneVersion matchVersion, TextReader input)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new <see cref="UAX29URLEmailTokenizer"/> with a given <see cref="AttributeSource.AttributeFactory"/>
        /// </summary>
        public UAX29URLEmailTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// LUCENENET specific: This method was added in .NET to prevent having to repeat code in the constructors.
        /// </summary>
        /// <param name="matchVersion"></param>
        private void Init(LuceneVersion matchVersion)
        {
            this.scanner = GetScannerFor(matchVersion);
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
        }

        private IStandardTokenizerInterface GetScannerFor(LuceneVersion matchVersion)
        {
            // best effort NPE if you dont call reset
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_47))
            {
                return new UAX29URLEmailTokenizerImpl(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_40))
            {
                return new UAX29URLEmailTokenizerImpl40(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
            {
                return new UAX29URLEmailTokenizerImpl36(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_34))
            {
                return new UAX29URLEmailTokenizerImpl34(m_input);
            }
            else
            {
                return new UAX29URLEmailTokenizerImpl31(m_input);
            }
#pragma warning restore 612, 618
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private ITypeAttribute typeAtt;

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
                    typeAtt.Type = TOKEN_TYPES[tokenType];
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