using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.CommonGrams;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Ngram;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Wikipedia;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Core
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

    public class TestBugInSomething : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void Test()
        {
            CharArraySet cas = new CharArraySet(TEST_VERSION_CURRENT, 3, false);
            cas.add("jjp");
            cas.add("wlmwoknt");
            cas.add("tcgyreo");

            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            builder.Add("mtqlpi", "");
            builder.Add("mwoknt", "jjp");
            builder.Add("tcgyreo", "zpfpajyws");
            NormalizeCharMap map = builder.Build();

            Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, cas, map);
            CheckAnalysisConsistency(Random(), a, false, "wmgddzunizdomqyj");
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestBugInSomething outerInstance;

            private CharArraySet cas;
            private NormalizeCharMap map;

            public AnalyzerAnonymousInnerClassHelper(TestBugInSomething outerInstance, CharArraySet cas, NormalizeCharMap map)
            {
                this.outerInstance = outerInstance;
                this.cas = cas;
                this.map = map;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(new /* TestRandomChains. */ CheckThatYouDidntReadAnythingReaderWrapper(reader), MockTokenFilter.ENGLISH_STOPSET, false, -65);
                TokenFilter f = new CommonGramsFilter(TEST_VERSION_CURRENT, t, cas);
                return new TokenStreamComponents(t, f);
            }

            public override TextReader InitReader(string fieldName, TextReader reader)
            {
                reader = new MockCharFilter(reader, 0);
                reader = new MappingCharFilter(map, reader);
                return reader;
            }
        }

        internal CharFilter wrappedStream = new CharFilterAnonymousInnerClassHelper(new StringReader("bogus"));

        private class CharFilterAnonymousInnerClassHelper : CharFilter
        {
            public CharFilterAnonymousInnerClassHelper(StringReader java) : base(java)
            {
            }


            public override void Mark(int readAheadLimit)
            {
                throw new System.NotSupportedException("mark(int)");
            }

            public override bool IsMarkSupported
            {
                get
                {
                    throw new System.NotSupportedException("IsMarkSupported");
                }
            }

            public override int Read()
            {
                throw new System.NotSupportedException("read()");
            }

            // public override int Read(char[] cbuf)
            // {
            //throw new System.NotSupportedException("read(char[])");
            // }

            //public override int read(CharBuffer target)
            //{
            //    throw new System.NotSupportedException("read(CharBuffer)");
            //}

            public override bool Ready()
            {
                throw new System.NotSupportedException("ready()");
            }

            public override void Reset()
            {
                throw new System.NotSupportedException("reset()");
            }

            public override long Skip(int n)
            {
                throw new System.NotSupportedException("skip(long)");
            }

            protected override int Correct(int currentOff)
            {
                throw new System.NotSupportedException("correct(int)");
            }

            public override void Close()
            {
                throw new System.NotSupportedException("close()");
            }

            public override int Read(char[] arg0, int arg1, int arg2)
            {
                throw new System.NotSupportedException("read(char[], int, int)");
            }
        }

        [Test]
        public virtual void TestWrapping()
        {
            CharFilter cs = new /* TestRandomChains. */ CheckThatYouDidntReadAnythingReaderWrapper(wrappedStream);
            try
            {
                cs.Mark(1);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("mark(int)", e.Message);
            }

            try
            {
                var supported = cs.IsMarkSupported;
                fail();
            }
            catch (Exception e)
            {
                assertEquals("markSupported()", e.Message);
            }

            try
            {
                cs.Read();
                fail();
            }
            catch (Exception e)
            {
                assertEquals("read()", e.Message);
            }

            try
            {
                cs.read(new char[0]);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("read(char[])", e.Message);
            }

            try
            {
                cs.read(new char[0]);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("read(CharBuffer)", e.Message);
            }

            try
            {
                cs.Reset();
                fail();
            }
            catch (Exception e)
            {
                assertEquals("reset()", e.Message);
            }

            try
            {
                cs.Skip(1);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("skip(long)", e.Message);
            }

            try
            {
                cs.CorrectOffset(1);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("correct(int)", e.Message);
            }

            try
            {
                cs.Close();
                fail();
            }
            catch (Exception e)
            {
                assertEquals("close()", e.Message);
            }

            try
            {
                cs.Read(new char[0], 0, 0);
                fail();
            }
            catch (Exception e)
            {
                assertEquals("read(char[], int, int)", e.Message);
            }
        }

        // todo: test framework?

        internal sealed class SopTokenFilter : TokenFilter
        {

            internal SopTokenFilter(TokenStream input) : base(input)
            {
            }

            public override bool IncrementToken()
            {
                if (input.IncrementToken())
                {
                    Console.WriteLine(input.GetType().Name + "->" + this.ReflectAsString(false));
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override void End()
            {
                base.End();
                Console.WriteLine(input.GetType().Name + ".end()");
            }

            public override void Dispose()
            {
                base.Dispose();
                Console.WriteLine(input.GetType().Name + ".close()");
            }

            public override void Reset()
            {
                base.Reset();
                Console.WriteLine(input.GetType().Name + ".reset()");
            }
        }

        // LUCENE-5269
        [Test]
        public virtual void TestUnicodeShinglesAndNgrams()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper100(this);
            CheckRandomData(Random(), analyzer, 2000);
        }

        private class AnalyzerAnonymousInnerClassHelper100 : Analyzer
        {
            private readonly TestBugInSomething outerInstance;

            public AnalyzerAnonymousInnerClassHelper100(TestBugInSomething outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, reader, 2, 94);
                //TokenStream stream = new SopTokenFilter(tokenizer);
                TokenStream stream = new ShingleFilter(tokenizer, 5);
                //stream = new SopTokenFilter(stream);
                stream = new NGramTokenFilter(TEST_VERSION_CURRENT, stream, 55, 83);
                //stream = new SopTokenFilter(stream);
                return new TokenStreamComponents(tokenizer, stream);
            }
        }

        [Test]
        public virtual void TestCuriousWikipediaString()
        {
            CharArraySet protWords = new CharArraySet(TEST_VERSION_CURRENT, new HashSet<object>(Arrays.AsList("rrdpafa", "pupmmlu", "xlq", "dyy", "zqrxrrck", "o", "hsrlfvcha")), false);
            sbyte[] table = new sbyte[] { -57, 26, 1, 48, 63, -23, 55, -84, 18, 120, -97, 103, 58, 13, 84, 89, 57, -13, -63, 5, 28, 97, -54, -94, 102, -108, -5, 5, 46, 40, 43, 78, 43, -72, 36, 29, 124, -106, -22, -51, 65, 5, 31, -42, 6, -99, 97, 14, 81, -128, 74, 100, 54, -55, -25, 53, -71, -98, 44, 33, 86, 106, -42, 47, 115, -89, -18, -26, 22, -95, -43, 83, -125, 105, -104, -24, 106, -16, 126, 115, -105, 97, 65, -33, 57, 44, -1, 123, -68, 100, 13, -41, -64, -119, 0, 92, 94, -36, 53, -9, -102, -18, 90, 94, -26, 31, 71, -20 };
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, protWords, table);
            CheckAnalysisConsistency(Random(), a, false, "B\u28c3\ue0f8[ \ud800\udfc2 </p> jb");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestBugInSomething outerInstance;

            private CharArraySet protWords;
            private sbyte[] table;

            public AnalyzerAnonymousInnerClassHelper2(TestBugInSomething outerInstance, CharArraySet protWords, sbyte[] table)
            {
                this.outerInstance = outerInstance;
                this.protWords = protWords;
                this.table = table;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new WikipediaTokenizer(reader);
                TokenStream stream = new SopTokenFilter(tokenizer);
                stream = new WordDelimiterFilter(TEST_VERSION_CURRENT, stream, table, -50, protWords);
                stream = new SopTokenFilter(stream);
                return new TokenStreamComponents(tokenizer, stream);
            }
        }



        // LUCENENET NOTE: Borrowed this class from the TestRandomChains class. It was in a commented section
        // that said "ignore". But it is required for this test. If/when TestRandomChains is ported, we can
        // use it there.
        private class CheckThatYouDidntReadAnythingReaderWrapper : CharFilter
        {
            bool readSomething;

            public CheckThatYouDidntReadAnythingReaderWrapper(TextReader @in)
                : base(GetBufferedReader(@in))
            { }

            /// <summary>
            /// LUCENENET: Copied this method from the WordlistLoader class - this class requires readers
            /// with a Reset() method (which .NET readers don't support). So, we use the Java BufferedReader
            /// as a wrapper for whatever reader the user passes (unless it is already a BufferedReader).
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            private static BufferedCharFilter GetBufferedReader(TextReader reader)
            {
                return (reader is BufferedCharFilter) ? (BufferedCharFilter)reader : new BufferedCharFilter(reader);
            }

            private BufferedCharFilter Input
            {
                get { return (BufferedCharFilter)this.input; }
            }

            protected override int Correct(int currentOff)
            {
                return currentOff; // we don't change any offsets
            }

            public override int Read(char[] cbuf, int off, int len)
            {
                readSomething = true;
                return input.Read(cbuf, off, len);
            }

            public override int Read()
            {
                readSomething = true;
                return input.Read();
            }

            public int read(char[] cbuf)
            {
                readSomething = true;
                return input.read(cbuf);
            }

            public override long Skip(int n)
            {
                readSomething = true;
                return Input.Skip(n);
            }

            public override void Mark(int readAheadLimit)
            {
                Input.Mark(readAheadLimit);
            }

            public override bool IsMarkSupported
            {
                get
                {
                    return Input.IsMarkSupported;
                }
            }

            public override bool Ready()
            {
                return Input.Ready();
            }

            public void reset()
            {
                Input.Reset();
            }
        }
    }
}