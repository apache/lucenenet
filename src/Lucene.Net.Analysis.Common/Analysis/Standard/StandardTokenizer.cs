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
using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using org.apache.lucene.analysis.standard;
using Version = Lucene.Net.Util.LuceneVersion;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Standard
{
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
    /// <a name="version"/>
    /// </para>
    /// <para>You must specify the required <seealso cref="Version"/>
    /// compatibility when creating StandardTokenizer:
    /// <ul>
    ///   <li> As of 3.4, Hiragana and Han characters are no longer wrongly split
    ///   from their combining characters. If you use a previous version number,
    ///   you get the exact broken behavior for backwards compatibility.
    ///   <li> As of 3.1, StandardTokenizer implements Unicode text segmentation.
    ///   If you use a previous version number, you get the exact behavior of
    ///   <seealso cref="ClassicTokenizer"/> for backwards compatibility.
    /// </ul>
    /// </para>
    /// </summary>

    public sealed class StandardTokenizer : Tokenizer
    {
        /// <summary>
        /// A private instance of the JFlex-constructed scanner </summary>
        private StandardTokenizerInterface scanner;

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
        public static readonly string[] TOKEN_TYPES = { "<ALPHANUM>", "<APOSTROPHE>", "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", "<ACRONYM_DEP>", "<SOUTHEAST_ASIAN>", "<IDEOGRAPHIC>", "<HIRAGANA>", "<KATAKANA>", "<HANGUL>" };

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
        /// Creates a new instance of the <seealso cref="StandardTokenizer"/>.  Attaches
        /// the <code>input</code> to the newly created JFlex scanner.
        /// </summary>
        /// <param name="input"> The input reader
        /// 
        /// See http://issues.apache.org/jira/browse/LUCENE-1068 </param>
        public StandardTokenizer(Version matchVersion, Reader input)
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();

            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new StandardTokenizer with a given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> 
        /// </summary>
        public StandardTokenizer(Version matchVersion, AttributeFactory factory, Reader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        private void Init(Version matchVersion)
        {
            if (matchVersion.OnOrAfter(Version.LUCENE_47))
            {
                this.scanner = new StandardTokenizerImpl(input);
            }
            else if (matchVersion.OnOrAfter(Version.LUCENE_40))
            {
                this.scanner = new StandardTokenizerImpl40(input);
            }
            else if (matchVersion.OnOrAfter(Version.LUCENE_34))
            {
                this.scanner = new StandardTokenizerImpl34(input);
            }
            else if (matchVersion.OnOrAfter(Version.LUCENE_31))
            {
                this.scanner = new StandardTokenizerImpl31(input);
            }
            else
            {
                this.scanner = new ClassicTokenizerImpl(input);
            }
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly ITypeAttribute typeAtt;

        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.analysis.TokenStream#next()
         */
        public override bool IncrementToken()
        {
            ClearAttributes();
            skippedPositions = 0;

            while (true)
            {
                int tokenType = scanner.NextToken;

                if (tokenType == StandardTokenizerInterface_Fields.YYEOF)
                {
                    return false;
                }

                if (scanner.yylength() <= maxTokenLength)
                {
                    posIncrAtt.PositionIncrement = skippedPositions + 1;
                    scanner.getText(termAtt);
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int start = scanner.yychar();
                    int start = scanner.yychar();
                    offsetAtt.SetOffset(correctOffset(start), correctOffset(start + termAtt.length()));
                    // This 'if' should be removed in the next release. For now, it converts
                    // invalid acronyms to HOST. When removed, only the 'else' part should
                    // remain.
                    if (tokenType == StandardTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HOST];
                        termAtt.Length = termAtt.length() - 1; // remove extra '.'
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

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(scanner.yychar() + scanner.yylength());
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // adjust any skipped tokens
            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
        }

        public override void Dispose()
        {
            base.Dispose();
            scanner.yyreset(input);
        }

        public override void Reset()
        {
            base.Reset();
            scanner.yyreset(input);
            skippedPositions = 0;
        }
    }

}