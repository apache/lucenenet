using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.IO;
using Lucene.Net.Analysis.Standard.Std31;
using Lucene.Net.Analysis.Standard.Std34;
using Lucene.Net.Analysis.Standard.Std36;
using Lucene.Net.Analysis.Standard.Std40;

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
    /// <p/>
    /// Tokens produced are of the following types:
    /// <ul>
    ///   <li>&lt;ALPHANUM&gt;: A sequence of alphabetic and numeric characters</li>
    ///   <li>&lt;NUM&gt;: A number</li>
    ///   <li>&lt;URL&gt;: A URL</li>
    ///   <li>&lt;EMAIL&gt;: An email address</li>
    ///   <li>&lt;SOUTHEAST_ASIAN&gt;: A sequence of characters from South and Southeast
    ///       Asian languages, including Thai, Lao, Myanmar, and Khmer</li>
    ///   <li>&lt;IDEOGRAPHIC&gt;: A single CJKV ideographic character</li>
    ///   <li>&lt;HIRAGANA&gt;: A single hiragana character</li>
    /// </ul>
    /// <a name="version"/>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating UAX29URLEmailTokenizer:
    /// <ul>
    ///   <li> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///   from their combining characters. If you use a previous version number,
    ///   you get the exact broken behavior for backwards compatibility.
    /// </ul>
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
        public static readonly string[] TOKEN_TYPES = new string[] { StandardTokenizer.TOKEN_TYPES[StandardTokenizer.ALPHANUM], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.NUM], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.SOUTHEAST_ASIAN], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL], "<URL>", "<EMAIL>" };

        private int skippedPositions;

        private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// Set the max allowed token length.  Any token longer
        ///  than this is skipped. 
        /// </summary>
        public int MaxTokenLength
        {
            set
            {
                if (value < 1)
                {
                    throw new System.ArgumentException("maxTokenLength must be greater than zero");
                }
                this.maxTokenLength = value;
            }
            get
            {
                return maxTokenLength;
            }
        }


        /// <summary>
        /// Creates a new instance of the UAX29URLEmailTokenizer.  Attaches
        /// the <code>input</code> to the newly created JFlex scanner.
        /// </summary>
        /// <param name="input"> The input reader </param>
        public UAX29URLEmailTokenizer(LuceneVersion matchVersion, TextReader input)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new UAX29URLEmailTokenizer with a given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/>
        /// </summary>
        public UAX29URLEmailTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// LUCENENET: This method was added in .NET to prevent having to repeat code in the constructors.
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
                return new UAX29URLEmailTokenizerImpl(input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_40))
            {
                return new UAX29URLEmailTokenizerImpl40(input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_36))
            {
                return new UAX29URLEmailTokenizerImpl36(input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_34))
            {
                return new UAX29URLEmailTokenizerImpl34(input);
            }
            else
            {
                return new UAX29URLEmailTokenizerImpl31(input);
            }
#pragma warning restore 612, 618
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private ICharTermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private ITypeAttribute typeAtt;

        public override bool IncrementToken()
        {
            ClearAttributes();
            skippedPositions = 0;

            while (true)
            {
                int tokenType = scanner.GetNextToken();

                if (tokenType == StandardTokenizerInterface_Fields.YYEOF)
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

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(scanner.YyChar + scanner.YyLength);
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // adjust any skipped tokens
            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
        }

        public override void Dispose()
        {
            base.Dispose();
            scanner.YyReset(input);
        }

        public override void Reset()
        {
            base.Reset();
            scanner.YyReset(input);
            skippedPositions = 0;
        }
    }
}