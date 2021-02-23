// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Analysis.Miscellaneous
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
#pragma warning disable 612, 618
    public class TestTrimFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestTrim()
        {
            char[] a = " a ".ToCharArray();
            char[] b = "b   ".ToCharArray();
            char[] ccc = "cCc".ToCharArray();
            char[] whitespace = "   ".ToCharArray();
            char[] empty = "".ToCharArray();

            TokenStream ts = new IterTokenStream(new Token(a, 0, a.Length, 1, 5), new Token(b, 0, b.Length, 6, 10), new Token(ccc, 0, ccc.Length, 11, 15), new Token(whitespace, 0, whitespace.Length, 16, 20), new Token(empty, 0, empty.Length, 21, 21));
            ts = new TrimFilter(TEST_VERSION_CURRENT, ts, false);

            AssertTokenStreamContents(ts, new string[] { "a", "b", "cCc", "", "" });

            a = " a".ToCharArray();
            b = "b ".ToCharArray();
            ccc = " c ".ToCharArray();
            whitespace = "   ".ToCharArray();
            ts = new IterTokenStream(new Token(a, 0, a.Length, 0, 2), new Token(b, 0, b.Length, 0, 2), new Token(ccc, 0, ccc.Length, 0, 3), new Token(whitespace, 0, whitespace.Length, 0, 3));
            ts = new TrimFilter(LuceneVersion.LUCENE_43, ts, true);

            AssertTokenStreamContents(ts, new string[] { "a", "b", "c", "" }, new int[] { 1, 0, 1, 3 }, new int[] { 2, 1, 2, 3 }, null, new int[] { 1, 1, 1, 1 }, null, null, false);
        }

        /// @deprecated (3.0) does not support custom attributes 
        [Obsolete("(3.0) does not support custom attributes")]
        private sealed class IterTokenStream : TokenStream
        {
            internal readonly Token[] tokens;
            internal int index = 0;
            internal ICharTermAttribute termAtt;
            internal IOffsetAttribute offsetAtt;
            internal IPositionIncrementAttribute posIncAtt;
            internal IFlagsAttribute flagsAtt;
            internal ITypeAttribute typeAtt;
            internal IPayloadAttribute payloadAtt;

            public IterTokenStream(params Token[] tokens)
                    : base()
            {
                this.tokens = tokens;
                this.termAtt = AddAttribute<ICharTermAttribute>();
                this.offsetAtt = AddAttribute<IOffsetAttribute>();
                this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
                this.flagsAtt = AddAttribute<IFlagsAttribute>();
                this.typeAtt = AddAttribute<ITypeAttribute>();
                this.payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public IterTokenStream(ICollection<Token> tokens)
                    : this(tokens.ToArray())
            {
            }

            public override sealed bool IncrementToken()
            {
                if (index >= tokens.Length)
                {
                    return false;
                }
                else
                {
                    ClearAttributes();
                    Token token = tokens[index++];
                    termAtt.SetEmpty().Append(token);
                    offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                    posIncAtt.PositionIncrement = token.PositionIncrement;
                    flagsAtt.Flags = token.Flags;
                    typeAtt.Type = token.Type;
                    payloadAtt.Payload = token.Payload;
                    return true;
                }
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
                return new TokenStreamComponents(tokenizer, new TrimFilter(LuceneVersion.LUCENE_43, tokenizer, true));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
                return new TokenStreamComponents(tokenizer, new TrimFilter(TEST_VERSION_CURRENT, tokenizer, false));
            });
            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                bool updateOffsets = Random.nextBoolean();
                LuceneVersion version = updateOffsets ? LuceneVersion.LUCENE_43 : TEST_VERSION_CURRENT;
                return new TokenStreamComponents(tokenizer, new TrimFilter(version, tokenizer, updateOffsets));
            });
            CheckOneTerm(a, "", "");
        }
    }
#pragma warning restore 612, 618
}