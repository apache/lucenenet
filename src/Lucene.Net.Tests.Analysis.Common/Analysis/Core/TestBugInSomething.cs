// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.CommonGrams;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Wikipedia;
using NUnit.Framework;
using System;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

    [SuppressCodecs("Direct")]
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

            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(new TestRandomChains.CheckThatYouDidntReadAnythingReaderWrapper(reader), MockTokenFilter.ENGLISH_STOPSET, false, -65);
                TokenFilter f = new CommonGramsFilter(TEST_VERSION_CURRENT, t, cas);
                return new TokenStreamComponents(t, f);
            }, initReader: (fieldName, reader) =>
            {
                reader = new MockCharFilter(reader, 0);
                reader = new MappingCharFilter(map, reader);
                return reader;            
            });
            CheckAnalysisConsistency(Random, a, false, "wmgddzunizdomqyj");
        }

        internal CharFilter wrappedStream = new CharFilterAnonymousClass(new StringReader("bogus"));

        private sealed class CharFilterAnonymousClass : CharFilter
        {
            public CharFilterAnonymousClass(StringReader java) : base(java)
            {
            }


            public override void Mark(int readAheadLimit)
            {
                throw UnsupportedOperationException.Create("Mark(int)");
            }

            public override bool IsMarkSupported => throw UnsupportedOperationException.Create("IsMarkSupported");

            public override int Read()
            {
                throw UnsupportedOperationException.Create("Read()");
            }

            // LUCENENET: We don't support these overloads in .NET
            // public override int Read(char[] cbuf)
            // {
            //throw UnsupportedOperationException.Create("Read(char[])");
            // }

            //public override int read(CharBuffer target)
            //{
            //    throw UnsupportedOperationException.Create("Read(CharBuffer)");
            //}

            public override bool IsReady => throw UnsupportedOperationException.Create("Ready()");

            public override void Reset()
            {
                throw UnsupportedOperationException.Create("Reset()");
            }

            public override long Skip(int n)
            {
                throw UnsupportedOperationException.Create("Skip(long)");
            }

            protected override int Correct(int currentOff)
            {
                throw UnsupportedOperationException.Create("Correct(int)");
            }

            protected override void Dispose(bool disposing)
            {
                throw UnsupportedOperationException.Create("Close()");
            }

            public override int Read(char[] arg0, int arg1, int arg2)
            {
                throw UnsupportedOperationException.Create("Read(char[], int, int)");
            }
        }

        [Test]
        public virtual void TestWrapping()
        {
            CharFilter cs = new TestRandomChains.CheckThatYouDidntReadAnythingReaderWrapper(wrappedStream);
            try
            {
                cs.Mark(1);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Mark(int)", e.Message);
            }

            try
            {
                var supported = cs.IsMarkSupported;
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("IsMarkSupported", e.Message);
            }

            try
            {
                cs.Read();
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Read()", e.Message);
            }

            try
            {
                cs.read(new char[0]);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                // LUCENENET NOTE: TextReader doesn't support an overload that doesn't supply
                // index and count. We have an extension method that does in test environment,
                // but the error will be for the cascaded overload
                //assertEquals("Read(char[])", e.Message);
                assertEquals("Read(char[], int, int)", e.Message);
            }

            // LUCENENET NOTE: We don't have a CharBuffer type in Lucene.Net,
            // nor do we have an overload that accepts it.
            //try
            //{
            //    cs.read(CharBuffer.wrap(new char[0]));
            //    fail();
            //}
            //catch (Exception e) when (e.IsException())
            //{
            //    assertEquals("Read(CharBuffer)", e.Message);
            //}

            try
            {
                cs.Reset();
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Reset()", e.Message);
            }

            try
            {
                cs.Skip(1);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Skip(long)", e.Message);
            }

            try
            {
                cs.CorrectOffset(1);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Correct(int)", e.Message);
            }

            try
            {
                cs.Dispose();
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Close()", e.Message);
            }

            try
            {
                cs.Read(new char[0], 0, 0);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertEquals("Read(char[], int, int)", e.Message);
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
                if (m_input.IncrementToken())
                {
                    Console.WriteLine(m_input.GetType().Name + "->" + this.ReflectAsString(false));
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
                Console.WriteLine(m_input.GetType().Name + ".end()");
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    Console.WriteLine(m_input.GetType().Name + ".close()");
                }
            }

            public override void Reset()
            {
                base.Reset();
                Console.WriteLine(m_input.GetType().Name + ".reset()");
            }
        }

        // LUCENE-5269
        [Test]
        [Slow]
        public virtual void TestUnicodeShinglesAndNgrams()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, reader, 2, 94);
                //TokenStream stream = new SopTokenFilter(tokenizer);
                TokenStream stream = new ShingleFilter(tokenizer, 5);
                //stream = new SopTokenFilter(stream);
                stream = new NGramTokenFilter(TEST_VERSION_CURRENT, stream, 55, 83);
                //stream = new SopTokenFilter(stream);
                return new TokenStreamComponents(tokenizer, stream);
            });
            CheckRandomData(Random, analyzer, 2000);
        }

        [Test]
        public virtual void TestCuriousWikipediaString()
        {
            CharArraySet protWords = new CharArraySet(TEST_VERSION_CURRENT, new JCG.HashSet<string> { "rrdpafa", "pupmmlu", "xlq", "dyy", "zqrxrrck", "o", "hsrlfvcha" }, false);
            byte[] table = (byte[])(Array)new sbyte[] { -57, 26, 1, 48, 63, -23, 55, -84, 18, 120, -97, 103, 58, 13, 84, 89, 57, -13, -63, 5, 28, 97, -54, -94, 102, -108, -5, 5, 46, 40, 43, 78, 43, -72, 36, 29, 124, -106, -22, -51, 65, 5, 31, -42, 6, -99, 97, 14, 81, -128, 74, 100, 54, -55, -25, 53, -71, -98, 44, 33, 86, 106, -42, 47, 115, -89, -18, -26, 22, -95, -43, 83, -125, 105, -104, -24, 106, -16, 126, 115, -105, 97, 65, -33, 57, 44, -1, 123, -68, 100, 13, -41, -64, -119, 0, 92, 94, -36, 53, -9, -102, -18, 90, 94, -26, 31, 71, -20 };
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new WikipediaTokenizer(reader);
                TokenStream stream = new SopTokenFilter(tokenizer);
                stream = new WordDelimiterFilter(TEST_VERSION_CURRENT, stream, table, (WordDelimiterFlags)(object)-50, protWords);
                stream = new SopTokenFilter(stream);
                return new TokenStreamComponents(tokenizer, stream);
            });
            CheckAnalysisConsistency(Random, a, false, "B\u28c3\ue0f8[ \ud800\udfc2 </p> jb");
        }
    }
}