// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard.Std31;
using Lucene.Net.Analysis.Standard.Std34;
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
    /// A grammar-based tokenizer constructed with JFlex.
    /// <para>
    /// As of Lucene version 3.1, this class implements the Word Break rules from the
    /// Unicode Text Segmentation algorithm, as specified in 
    /// <a href="http://unicode.org/reports/tr29/">Unicode Standard Annex #29</a>.
    /// <p/>
    /// </para>
    /// <para>Many applications have specific tokenizer needs.  If this tokenizer does
    /// not suit your application, please consider copying this source code
    /// directory to your project and maintaining your own grammar-based tokenizer.
    /// 
    /// </para>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="StandardTokenizer"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///         from their combining characters. If you use a previous version number,
    ///         you get the exact broken behavior for backwards compatibility.</description></item>
    ///     <item><description> As of 3.1, StandardTokenizer implements Unicode text segmentation.
    ///         If you use a previous version number, you get the exact behavior of
    ///         <see cref="ClassicTokenizer"/> for backwards compatibility.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class StandardTokenizer : Tokenizer
    {
        /// <summary>
        /// A private instance of the JFlex-constructed scanner </summary>
        private IStandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int APOSTROPHE = 1;
        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int ACRONYM = 2;
        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int HOST = 5;
        public const int NUM = 6;
        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int CJ = 7;

        /// @deprecated (3.1) 
        [Obsolete("(3.1)")]
        public const int ACRONYM_DEP = 8;

        public const int SOUTHEAST_ASIAN = 9;
        public const int IDEOGRAPHIC = 10;
        public const int HIRAGANA = 11;
        public const int KATAKANA = 12;
        public const int HANGUL = 13;

        /// <summary>
        /// String token types that correspond to token type int constants </summary>
        public static readonly string[] TOKEN_TYPES = {
            "<ALPHANUM>",
            "<APOSTROPHE>",
            "<ACRONYM>",
            "<COMPANY>",
            "<EMAIL>",
            "<HOST>",
            "<NUM>",
            "<CJ>",
            "<ACRONYM_DEP>",
            "<SOUTHEAST_ASIAN>",
            "<IDEOGRAPHIC>",
            "<HIRAGANA>",
            "<KATAKANA>",
            "<HANGUL>"
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
                    throw new ArgumentException("maxTokenLength must be greater than zero");
                }
                this.maxTokenLength = value;
            }
        }


        /// <summary>
        /// Creates a new instance of the <see cref="StandardTokenizer"/>.  Attaches
        /// the <paramref name="input"/> to the newly created JFlex-generated (then ported to .NET) scanner.
        /// </summary>
        /// <param name="matchVersion"> Lucene compatibility version - See <see cref="StandardTokenizer"/> </param>
        /// <param name="input"> The input reader
        /// 
        /// See http://issues.apache.org/jira/browse/LUCENE-1068 </param>
        public StandardTokenizer(LuceneVersion matchVersion, TextReader input)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new <see cref="StandardTokenizer"/> with a given <see cref="AttributeSource.AttributeFactory"/> 
        /// </summary>
        public StandardTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        private void Init(LuceneVersion matchVersion)
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_47))
            {
                this.scanner = new StandardTokenizerImpl(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_40))
            {
                this.scanner = new StandardTokenizerImpl40(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_34))
            {
                this.scanner = new StandardTokenizerImpl34(m_input);
            }
            else if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
            {
                this.scanner = new StandardTokenizerImpl31(m_input);
            }
#pragma warning restore 612, 618
            else
            {
                this.scanner = new ClassicTokenizerImpl(m_input);
            }

            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
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
                    // This 'if' should be removed in the next release. For now, it converts
                    // invalid acronyms to HOST. When removed, only the 'else' part should
                    // remain.
#pragma warning disable 612, 618
                    if (tokenType == StandardTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HOST];
#pragma warning restore 612, 618
                        termAtt.Length = termAtt.Length - 1; // remove extra '.'
                    }
                    else
                    {
                        typeAtt.Type = StandardTokenizer.TOKEN_TYPES[tokenType];
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