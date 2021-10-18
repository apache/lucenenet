// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Wikipedia
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
    /// Extension of <see cref="Standard.StandardTokenizer"/> that is aware of Wikipedia syntax.  It is based off of the
    /// Wikipedia tutorial available at http://en.wikipedia.org/wiki/Wikipedia:Tutorial, but it may not be complete.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class WikipediaTokenizer : Tokenizer
    {
        public const string INTERNAL_LINK = "il";
        public const string EXTERNAL_LINK = "el";
        //The URL part of the link, i.e. the first token
        public const string EXTERNAL_LINK_URL = "elu";
        public const string CITATION = "ci";
        public const string CATEGORY = "c";
        public const string BOLD = "b";
        public const string ITALICS = "i";
        public const string BOLD_ITALICS = "bi";
        public const string HEADING = "h";
        public const string SUB_HEADING = "sh";

        public const int ALPHANUM_ID = 0;
        public const int APOSTROPHE_ID = 1;
        public const int ACRONYM_ID = 2;
        public const int COMPANY_ID = 3;
        public const int EMAIL_ID = 4;
        public const int HOST_ID = 5;
        public const int NUM_ID = 6;
        public const int CJ_ID = 7;
        public const int INTERNAL_LINK_ID = 8;
        public const int EXTERNAL_LINK_ID = 9;
        public const int CITATION_ID = 10;
        public const int CATEGORY_ID = 11;
        public const int BOLD_ID = 12;
        public const int ITALICS_ID = 13;
        public const int BOLD_ITALICS_ID = 14;
        public const int HEADING_ID = 15;
        public const int SUB_HEADING_ID = 16;
        public const int EXTERNAL_LINK_URL_ID = 17;

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
            INTERNAL_LINK,
            EXTERNAL_LINK,
            CITATION,
            CATEGORY,
            BOLD,
            ITALICS,
            BOLD_ITALICS,
            HEADING,
            SUB_HEADING,
            EXTERNAL_LINK_URL
        };

        /// <summary>
        /// Only output tokens
        /// </summary>
        public const int TOKENS_ONLY = 0;
        /// <summary>
        /// Only output untokenized tokens, which are tokens that would normally be split into several tokens
        /// </summary>
        public const int UNTOKENIZED_ONLY = 1;
        /// <summary>
        /// Output the both the untokenized token and the splits
        /// </summary>
        public const int BOTH = 2;
        /// <summary>
        /// This flag is used to indicate that the produced "Token" would, if <see cref="TOKENS_ONLY"/> was used, produce multiple tokens.
        /// </summary>
        public const int UNTOKENIZED_TOKEN_FLAG = 1;
        /// <summary>
        /// A private instance of the JFlex-constructed scanner
        /// </summary>
        private readonly WikipediaTokenizerImpl scanner;

        private int tokenOutput = TOKENS_ONLY;
        private ICollection<string> untokenizedTypes = Collections.EmptySet<string>();
        private IEnumerator<AttributeSource.State> tokens = null;

        private IOffsetAttribute offsetAtt;
        private ITypeAttribute typeAtt;
        private IPositionIncrementAttribute posIncrAtt;
        private ICharTermAttribute termAtt;
        private IFlagsAttribute flagsAtt;

        private bool first;

        /// <summary>
        /// Creates a new instance of the <see cref="WikipediaTokenizer"/>. Attaches the
        /// <paramref name="input"/> to a newly created JFlex scanner.
        /// </summary>
        /// <param name="input"> The Input <see cref="TextReader"/> </param>
        public WikipediaTokenizer(TextReader input)
            : this(input, TOKENS_ONLY, Collections.EmptySet<string>())
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WikipediaTokenizer"/>.  Attaches the
        /// <paramref name="input"/> to a the newly created JFlex scanner.
        /// </summary>
        /// <param name="input"> The input </param>
        /// <param name="tokenOutput"> One of <see cref="TOKENS_ONLY"/>, <see cref="UNTOKENIZED_ONLY"/>, <see cref="BOTH"/> </param>
        /// <param name="untokenizedTypes"> Untokenized types </param>
        public WikipediaTokenizer(TextReader input, int tokenOutput, ICollection<string> untokenizedTypes)
            : base(input)
        {
            this.scanner = new WikipediaTokenizerImpl(this.m_input);
            Init(tokenOutput, untokenizedTypes);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WikipediaTokenizer"/>.  Attaches the
        /// <paramref name="input"/> to a the newly created JFlex scanner. Uses the given <see cref="AttributeSource.AttributeFactory"/>.
        /// </summary>
        /// <param name="factory"> The <see cref="AttributeSource.AttributeFactory"/> </param>
        /// <param name="input"> The input </param>
        /// <param name="tokenOutput"> One of <see cref="TOKENS_ONLY"/>, <see cref="UNTOKENIZED_ONLY"/>, <see cref="BOTH"/> </param>
        /// <param name="untokenizedTypes"> Untokenized types </param>
        public WikipediaTokenizer(AttributeFactory factory, TextReader input, int tokenOutput, ICollection<string> untokenizedTypes)
              : base(factory, input)
        {
            this.scanner = new WikipediaTokenizerImpl(this.m_input);
            Init(tokenOutput, untokenizedTypes);
        }

        private void Init(int tokenOutput, ICollection<string> untokenizedTypes)
        {
            // TODO: cutover to enum
            if (tokenOutput != TOKENS_ONLY && tokenOutput != UNTOKENIZED_ONLY && tokenOutput != BOTH)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenOutput), "tokenOutput must be TOKENS_ONLY, UNTOKENIZED_ONLY or BOTH"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.tokenOutput = tokenOutput;
            this.untokenizedTypes = untokenizedTypes;
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            termAtt = AddAttribute<ICharTermAttribute>();
            flagsAtt = AddAttribute<IFlagsAttribute>();
        }

        /// <summary>
        /// <see cref="TokenStream.IncrementToken"/>
        /// </summary>
        public override sealed bool IncrementToken()
        {
            if (tokens != null && tokens.MoveNext())
            {
                AttributeSource.State state = tokens.Current;
                RestoreState(state);
                return true;
            }
            ClearAttributes();
            int tokenType = scanner.GetNextToken();

            if (tokenType == WikipediaTokenizerImpl.YYEOF)
            {
                return false;
            }
            string type = WikipediaTokenizerImpl.TOKEN_TYPES[tokenType];
            if (tokenOutput == TOKENS_ONLY || untokenizedTypes.Contains(type) == false)
            {
                SetupToken();
            }
            else if (tokenOutput == UNTOKENIZED_ONLY && untokenizedTypes.Contains(type) == true)
            {
                CollapseTokens(tokenType);

            }
            else if (tokenOutput == BOTH)
            {
                //collapse into a single token, add it to tokens AND output the individual tokens
                //output the untokenized Token first
                CollapseAndSaveTokens(tokenType, type);
            }
            int posinc = scanner.PositionIncrement;
            if (first && posinc == 0)
            {
                posinc = 1; // don't emit posinc=0 for the first token!
            }
            posIncrAtt.PositionIncrement = posinc;
            typeAtt.Type = type;
            first = false;
            return true;
        }

        private void CollapseAndSaveTokens(int tokenType, string type)
        {
            //collapse
            StringBuilder buffer = new StringBuilder(32);
            int numAdded = scanner.SetText(buffer);
            //TODO: how to know how much whitespace to add
            int theStart = scanner.YyChar;
            int lastPos = theStart + numAdded;
            int tmpTokType;
            int numSeen = 0;
            IList<AttributeSource.State> tmp = new JCG.List<AttributeSource.State>();
            SetupSavedToken(0, type);
            tmp.Add(CaptureState());
            //while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
            while ((tmpTokType = scanner.GetNextToken()) != WikipediaTokenizerImpl.YYEOF && tmpTokType == tokenType && scanner.NumWikiTokensSeen > numSeen)
            {
                int currPos = scanner.YyChar;
                //append whitespace
                for (int i = 0; i < (currPos - lastPos); i++)
                {
                    buffer.Append(' ');
                }
                numAdded = scanner.SetText(buffer);
                SetupSavedToken(scanner.PositionIncrement, type);
                tmp.Add(CaptureState());
                numSeen++;
                lastPos = currPos + numAdded;
            }
            //trim the buffer
            // TODO: this is inefficient
            string s = buffer.ToString().Trim();
            termAtt.SetEmpty().Append(s);
            offsetAtt.SetOffset(CorrectOffset(theStart), CorrectOffset(theStart + s.Length));
            flagsAtt.Flags = UNTOKENIZED_TOKEN_FLAG;
            //The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
            if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
            {
                scanner.YyPushBack(scanner.YyLength);
            }
            tokens = tmp.GetEnumerator();
        }

        private void SetupSavedToken(int positionInc, string type)
        {
            SetupToken();
            posIncrAtt.PositionIncrement = positionInc;
            typeAtt.Type = type;
        }

        private void CollapseTokens(int tokenType)
        {
            //collapse
            StringBuilder buffer = new StringBuilder(32);
            int numAdded = scanner.SetText(buffer);
            //TODO: how to know how much whitespace to add
            int theStart = scanner.YyChar;
            int lastPos = theStart + numAdded;
            int tmpTokType;
            int numSeen = 0;
            //while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
            while ((tmpTokType = scanner.GetNextToken()) != WikipediaTokenizerImpl.YYEOF && tmpTokType == tokenType && scanner.NumWikiTokensSeen > numSeen)
            {
                int currPos = scanner.YyChar;
                //append whitespace
                for (int i = 0; i < (currPos - lastPos); i++)
                {
                    buffer.Append(' ');
                }
                numAdded = scanner.SetText(buffer);
                numSeen++;
                lastPos = currPos + numAdded;
            }
            //trim the buffer
            // TODO: this is inefficient
            string s = buffer.ToString().Trim();
            termAtt.SetEmpty().Append(s);
            offsetAtt.SetOffset(CorrectOffset(theStart), CorrectOffset(theStart + s.Length));
            flagsAtt.Flags = UNTOKENIZED_TOKEN_FLAG;
            //The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
            if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
            {
                scanner.YyPushBack(scanner.YyLength);
            }
            else
            {
                tokens = null;
            }
        }

        private void SetupToken()
        {
            scanner.GetText(termAtt);
            int start = scanner.YyChar;
            offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + termAtt.Length));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                scanner.YyReset(m_input);
            }
        }

        /// <summary>
        /// <see cref="TokenStream.Reset"/>
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            scanner.YyReset(m_input);
            tokens = null;
            scanner.Reset();
            first = true;
        }

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(scanner.YyChar + scanner.YyLength);
            this.offsetAtt.SetOffset(finalOffset, finalOffset);
        }
    }
}