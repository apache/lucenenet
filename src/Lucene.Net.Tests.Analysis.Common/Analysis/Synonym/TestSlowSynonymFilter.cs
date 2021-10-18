// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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


    //using org.apache.lucene.analysis.tokenattributes;

    /// @deprecated Remove this test in Lucene 5.0 
    [Obsolete("Remove this test in Lucene 5.0")]
    public class TestSlowSynonymFilter : BaseTokenStreamTestCase
    {

        internal static IList<string> Strings(string str)
        {
            return str.Split(' ').TrimEnd();
        }

        internal static void AssertTokenizesTo(SlowSynonymMap dict, string input, string[] expected)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
            AssertTokenStreamContents(stream, expected);
        }

        internal static void AssertTokenizesTo(SlowSynonymMap dict, string input, string[] expected, int[] posIncs)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
            AssertTokenStreamContents(stream, expected, posIncs);
        }

        internal static void AssertTokenizesTo(SlowSynonymMap dict, IList<Token> input, string[] expected, int[] posIncs)
        {
            TokenStream tokenizer = new IterTokenStream(input);
            SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
            AssertTokenStreamContents(stream, expected, posIncs);
        }

        internal static void AssertTokenizesTo(SlowSynonymMap dict, IList<Token> input, string[] expected, int[] startOffsets, int[] endOffsets, int[] posIncs)
        {
            TokenStream tokenizer = new IterTokenStream(input);
            SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
            AssertTokenStreamContents(stream, expected, startOffsets, endOffsets, posIncs);
        }

        [Test]
        public virtual void TestMatching()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = false;
            bool merge = true;
            map.Add(Strings("a b"), Tokens("ab"), orig, merge);
            map.Add(Strings("a c"), Tokens("ac"), orig, merge);
            map.Add(Strings("a"), Tokens("aa"), orig, merge);
            map.Add(Strings("b"), Tokens("bb"), orig, merge);
            map.Add(Strings("z x c v"), Tokens("zxcv"), orig, merge);
            map.Add(Strings("x c"), Tokens("xc"), orig, merge);

            AssertTokenizesTo(map, "$", new string[] { "$" });
            AssertTokenizesTo(map, "a", new string[] { "aa" });
            AssertTokenizesTo(map, "a $", new string[] { "aa", "$" });
            AssertTokenizesTo(map, "$ a", new string[] { "$", "aa" });
            AssertTokenizesTo(map, "a a", new string[] { "aa", "aa" });
            AssertTokenizesTo(map, "b", new string[] { "bb" });
            AssertTokenizesTo(map, "z x c v", new string[] { "zxcv" });
            AssertTokenizesTo(map, "z x c $", new string[] { "z", "xc", "$" });

            // repeats
            map.Add(Strings("a b"), Tokens("ab"), orig, merge);
            map.Add(Strings("a b"), Tokens("ab"), orig, merge);

            // FIXME: the below test intended to be { "ab" }
            AssertTokenizesTo(map, "a b", new string[] { "ab", "ab", "ab" });

            // check for lack of recursion
            map.Add(Strings("zoo"), Tokens("zoo"), orig, merge);
            AssertTokenizesTo(map, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "$", "zoo" });
            map.Add(Strings("zoo"), Tokens("zoo zoo"), orig, merge);
            // FIXME: the below test intended to be { "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo" }
            // maybe this was just a typo in the old test????
            AssertTokenizesTo(map, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo" });
        }

        [Test]
        public virtual void TestIncludeOrig()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = true;
            bool merge = true;
            map.Add(Strings("a b"), Tokens("ab"), orig, merge);
            map.Add(Strings("a c"), Tokens("ac"), orig, merge);
            map.Add(Strings("a"), Tokens("aa"), orig, merge);
            map.Add(Strings("b"), Tokens("bb"), orig, merge);
            map.Add(Strings("z x c v"), Tokens("zxcv"), orig, merge);
            map.Add(Strings("x c"), Tokens("xc"), orig, merge);

            AssertTokenizesTo(map, "$", new string[] { "$" }, new int[] { 1 });
            AssertTokenizesTo(map, "a", new string[] { "a", "aa" }, new int[] { 1, 0 });
            AssertTokenizesTo(map, "a", new string[] { "a", "aa" }, new int[] { 1, 0 });
            AssertTokenizesTo(map, "$ a", new string[] { "$", "a", "aa" }, new int[] { 1, 1, 0 });
            AssertTokenizesTo(map, "a $", new string[] { "a", "aa", "$" }, new int[] { 1, 0, 1 });
            AssertTokenizesTo(map, "$ a !", new string[] { "$", "a", "aa", "!" }, new int[] { 1, 1, 0, 1 });
            AssertTokenizesTo(map, "a a", new string[] { "a", "aa", "a", "aa" }, new int[] { 1, 0, 1, 0 });
            AssertTokenizesTo(map, "b", new string[] { "b", "bb" }, new int[] { 1, 0 });
            AssertTokenizesTo(map, "z x c v", new string[] { "z", "zxcv", "x", "c", "v" }, new int[] { 1, 0, 1, 1, 1 });
            AssertTokenizesTo(map, "z x c $", new string[] { "z", "x", "xc", "c", "$" }, new int[] { 1, 1, 0, 1, 1 });

            // check for lack of recursion
            map.Add(Strings("zoo zoo"), Tokens("zoo"), orig, merge);
            // CHECKME: I think the previous test (with 4 zoo's), was just a typo.
            AssertTokenizesTo(map, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "$", "zoo" }, new int[] { 1, 0, 1, 1, 1 });

            map.Add(Strings("zoo"), Tokens("zoo zoo"), orig, merge);
            AssertTokenizesTo(map, "zoo zoo $ zoo", new string[] { "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo" }, new int[] { 1, 0, 1, 1, 1, 0, 1 });
        }


        [Test]
        public virtual void TestMapMerge()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = false;
            bool merge = true;
            map.Add(Strings("a"), Tokens("a5,5"), orig, merge);
            map.Add(Strings("a"), Tokens("a3,3"), orig, merge);

            AssertTokenizesTo(map, "a", new string[] { "a3", "a5" }, new int[] { 1, 2 });

            map.Add(Strings("b"), Tokens("b3,3"), orig, merge);
            map.Add(Strings("b"), Tokens("b5,5"), orig, merge);

            AssertTokenizesTo(map, "b", new string[] { "b3", "b5" }, new int[] { 1, 2 });

            map.Add(Strings("a"), Tokens("A3,3"), orig, merge);
            map.Add(Strings("a"), Tokens("A5,5"), orig, merge);

            AssertTokenizesTo(map, "a", new string[] { "a3", "A3", "a5", "A5" }, new int[] { 1, 0, 2, 0 });

            map.Add(Strings("a"), Tokens("a1"), orig, merge);
            AssertTokenizesTo(map, "a", new string[] { "a1", "a3", "A3", "a5", "A5" }, new int[] { 1, 2, 0, 2, 0 });

            map.Add(Strings("a"), Tokens("a2,2"), orig, merge);
            map.Add(Strings("a"), Tokens("a4,4 a6,2"), orig, merge);
            AssertTokenizesTo(map, "a", new string[] { "a1", "a2", "a3", "A3", "a4", "a5", "A5", "a6" }, new int[] { 1, 1, 1, 0, 1, 1, 0, 1 });
        }


        [Test]
        public virtual void TestOverlap()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = false;
            bool merge = true;
            map.Add(Strings("qwe"), Tokens("qq/ww/ee"), orig, merge);
            map.Add(Strings("qwe"), Tokens("xx"), orig, merge);
            map.Add(Strings("qwe"), Tokens("yy"), orig, merge);
            map.Add(Strings("qwe"), Tokens("zz"), orig, merge);
            AssertTokenizesTo(map, "$", new string[] { "$" });
            AssertTokenizesTo(map, "qwe", new string[] { "qq", "ww", "ee", "xx", "yy", "zz" }, new int[] { 1, 0, 0, 0, 0, 0 });

            // test merging within the map

            map.Add(Strings("a"), Tokens("a5,5 a8,3 a10,2"), orig, merge);
            map.Add(Strings("a"), Tokens("a3,3 a7,4 a9,2 a11,2 a111,100"), orig, merge);
            AssertTokenizesTo(map, "a", new string[] { "a3", "a5", "a7", "a8", "a9", "a10", "a11", "a111" }, new int[] { 1, 2, 2, 1, 1, 1, 1, 100 });
        }

        [Test]
        public virtual void TestPositionIncrements()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = false;
            bool merge = true;

            // test that generated tokens start at the same posInc as the original
            map.Add(Strings("a"), Tokens("aa"), orig, merge);
            AssertTokenizesTo(map, Tokens("a,5"), new string[] { "aa" }, new int[] { 5 });
            AssertTokenizesTo(map, Tokens("b,1 a,0"), new string[] { "b", "aa" }, new int[] { 1, 0 });

            // test that offset of first replacement is ignored (always takes the orig offset)
            map.Add(Strings("b"), Tokens("bb,100"), orig, merge);
            AssertTokenizesTo(map, Tokens("b,5"), new string[] { "bb" }, new int[] { 5 });
            AssertTokenizesTo(map, Tokens("c,1 b,0"), new string[] { "c", "bb" }, new int[] { 1, 0 });

            // test that subsequent tokens are adjusted accordingly
            map.Add(Strings("c"), Tokens("cc,100 c2,2"), orig, merge);
            AssertTokenizesTo(map, Tokens("c,5"), new string[] { "cc", "c2" }, new int[] { 5, 2 });
            AssertTokenizesTo(map, Tokens("d,1 c,0"), new string[] { "d", "cc", "c2" }, new int[] { 1, 0, 2 });
        }


        [Test]
        public virtual void TestPositionIncrementsWithOrig()
        {
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = true;
            bool merge = true;

            // test that generated tokens start at the same offset as the original
            map.Add(Strings("a"), Tokens("aa"), orig, merge);
            AssertTokenizesTo(map, Tokens("a,5"), new string[] { "a", "aa" }, new int[] { 5, 0 });
            AssertTokenizesTo(map, Tokens("b,1 a,0"), new string[] { "b", "a", "aa" }, new int[] { 1, 0, 0 });

            // test that offset of first replacement is ignored (always takes the orig offset)
            map.Add(Strings("b"), Tokens("bb,100"), orig, merge);
            AssertTokenizesTo(map, Tokens("b,5"), new string[] { "b", "bb" }, new int[] { 5, 0 });
            AssertTokenizesTo(map, Tokens("c,1 b,0"), new string[] { "c", "b", "bb" }, new int[] { 1, 0, 0 });

            // test that subsequent tokens are adjusted accordingly
            map.Add(Strings("c"), Tokens("cc,100 c2,2"), orig, merge);
            AssertTokenizesTo(map, Tokens("c,5"), new string[] { "c", "cc", "c2" }, new int[] { 5, 0, 2 });
            AssertTokenizesTo(map, Tokens("d,1 c,0"), new string[] { "d", "c", "cc", "c2" }, new int[] { 1, 0, 0, 2 });
        }


        [Test]
        public virtual void TestOffsetBug()
        {
            // With the following rules:
            // a a=>b
            // x=>y
            // analysing "a x" causes "y" to have a bad offset (end less than start)
            // SOLR-167
            SlowSynonymMap map = new SlowSynonymMap();

            bool orig = false;
            bool merge = true;

            map.Add(Strings("a a"), Tokens("b"), orig, merge);
            map.Add(Strings("x"), Tokens("y"), orig, merge);

            // "a a x" => "b y"
            AssertTokenizesTo(map, Tokens("a,1,0,1 a,1,2,3 x,1,4,5"), new string[] { "b", "y" }, new int[] { 0, 4 }, new int[] { 3, 5 }, new int[] { 1, 1 });
        }


        /// <summary>
        ///*
        /// Return a list of tokens according to a test string format:
        /// a b c  =>  returns List<Token> [a,b,c]
        /// a/b   => tokens a and b share the same spot (b.positionIncrement=0)
        /// a,3/b/c => a,b,c all share same position (a.positionIncrement=3, b.positionIncrement=0, c.positionIncrement=0)
        /// a,1,10,11  => "a" with positionIncrement=1, startOffset=10, endOffset=11 </summary>
        /// @deprecated (3.0) does not support attributes api 
        [Obsolete("(3.0) does not support attributes api")]
        private IList<Token> Tokens(string str)
        {
            string[] arr = str.Split(' ').TrimEnd();
            IList<Token> result = new JCG.List<Token>();
            for (int i = 0; i < arr.Length; i++)
            {
                string[] toks = arr[i].Split('/').TrimEnd();
                string[] @params = toks[0].Split(',').TrimEnd();

                int posInc;
                int start;
                int end;

                if (@params.Length > 1)
                {
                    posInc = int.Parse(@params[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    posInc = 1;
                }

                if (@params.Length > 2)
                {
                    start = int.Parse(@params[2], CultureInfo.InvariantCulture);
                }
                else
                {
                    start = 0;
                }

                if (@params.Length > 3)
                {
                    end = int.Parse(@params[3], CultureInfo.InvariantCulture);
                }
                else
                {
                    end = start + @params[0].Length;
                }

                Token t = new Token(@params[0], start, end, "TEST");
                t.PositionIncrement = posInc;

                result.Add(t);
                for (int j = 1; j < toks.Length; j++)
                {
                    t = new Token(toks[j], 0, 0, "TEST");
                    t.PositionIncrement = 0;
                    result.Add(t);
                }
            }
            return result;
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

            public IterTokenStream(params Token[] tokens) : base()
            {
                this.tokens = tokens;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
                flagsAtt = AddAttribute<IFlagsAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public IterTokenStream(ICollection<Token> tokens) : this(tokens.ToArray())
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
    }
}