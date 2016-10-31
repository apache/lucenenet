using System;
using System.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Core;
using System.IO;
using Lucene.Net.Search;
using System.Globalization;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Net.Analysis.Sinks
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
    /// tests for the TestTeeSinkTokenFilter
    /// </summary>
    public class TestTeeSinkTokenFilter : BaseTokenStreamTestCase
    {
        protected internal StringBuilder buffer1;
        protected internal StringBuilder buffer2;
        protected internal string[] tokens1;
        protected internal string[] tokens2;

        public override void SetUp()
        {
            base.SetUp();
            tokens1 = new string[] { "The", "quick", "Burgundy", "Fox", "jumped", "over", "the", "lazy", "Red", "Dogs" };
            tokens2 = new string[] { "The", "Lazy", "Dogs", "should", "stay", "on", "the", "porch" };
            buffer1 = new StringBuilder();

            for (int i = 0; i < tokens1.Length; i++)
            {
                buffer1.Append(tokens1[i]).Append(' ');
            }
            buffer2 = new StringBuilder();
            for (int i = 0; i < tokens2.Length; i++)
            {
                buffer2.Append(tokens2[i]).Append(' ');
            }
        }

        internal static readonly TeeSinkTokenFilter.SinkFilter theFilter = new SinkFilterAnonymousInnerClassHelper();

        private class SinkFilterAnonymousInnerClassHelper : TeeSinkTokenFilter.SinkFilter
        {
            public SinkFilterAnonymousInnerClassHelper()
            {
            }

            public override bool Accept(AttributeSource a)
            {
                ICharTermAttribute termAtt = a.GetAttribute<ICharTermAttribute>();
                return termAtt.ToString().Equals("The", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        internal static readonly TeeSinkTokenFilter.SinkFilter dogFilter = new SinkFilterAnonymousInnerClassHelper2();

        private class SinkFilterAnonymousInnerClassHelper2 : TeeSinkTokenFilter.SinkFilter
        {
            public SinkFilterAnonymousInnerClassHelper2()
            {
            }

            public override bool Accept(AttributeSource a)
            {
                ICharTermAttribute termAtt = a.GetAttribute<ICharTermAttribute>();
                return termAtt.ToString().Equals("Dogs", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        // LUCENE-1448
        // TODO: instead of testing it this way, we can test 
        // with BaseTokenStreamTestCase now...
        [Test]
        public virtual void TestEndOffsetPositionWithTeeSinkTokenFilter()
        {
            Store.Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            TokenStream tokenStream = analyzer.TokenStream("field", "abcd   ");
            TeeSinkTokenFilter tee = new TeeSinkTokenFilter(tokenStream);
            TokenStream sink = tee.NewSinkTokenStream();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            ft.StoreTermVectorPositions = true;
            Field f1 = new Field("field", tee, ft);
            Field f2 = new Field("field", sink, ft);
            doc.Add(f1);
            doc.Add(f2);
            w.AddDocument(doc);
            w.Dispose();

            IndexReader r = DirectoryReader.Open(dir);
            Terms vector = r.GetTermVectors(0).Terms("field");
            assertEquals(1, vector.Size());
            TermsEnum termsEnum = vector.Iterator(null);
            termsEnum.Next();
            assertEquals(2, termsEnum.TotalTermFreq());
            DocsAndPositionsEnum positions = termsEnum.DocsAndPositions(null, null);
            assertTrue(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            assertEquals(2, positions.Freq());
            positions.NextPosition();
            assertEquals(0, positions.StartOffset());
            assertEquals(4, positions.EndOffset());
            positions.NextPosition();
            assertEquals(8, positions.StartOffset());
            assertEquals(12, positions.EndOffset());
            assertEquals(DocIdSetIterator.NO_MORE_DOCS, positions.NextDoc());
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestGeneral()
        {
            TeeSinkTokenFilter source = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer1.ToString()), MockTokenizer.WHITESPACE, false));
            TokenStream sink1 = source.NewSinkTokenStream();
            TokenStream sink2 = source.NewSinkTokenStream(theFilter);

            source.AddAttribute<ICheckClearAttributesAttribute>();
            sink1.AddAttribute<ICheckClearAttributesAttribute>();
            sink2.AddAttribute<ICheckClearAttributesAttribute>();

            AssertTokenStreamContents(source, tokens1);
            AssertTokenStreamContents(sink1, tokens1);
            AssertTokenStreamContents(sink2, new string[] { "The", "the" });
        }

        [Test]
        public virtual void TestMultipleSources()
        {
            TeeSinkTokenFilter tee1 = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer1.ToString()), MockTokenizer.WHITESPACE, false));
            TeeSinkTokenFilter.SinkTokenStream dogDetector = tee1.NewSinkTokenStream(dogFilter);
            TeeSinkTokenFilter.SinkTokenStream theDetector = tee1.NewSinkTokenStream(theFilter);
            tee1.Reset();
            TokenStream source1 = new CachingTokenFilter(tee1);

            tee1.AddAttribute<ICheckClearAttributesAttribute>();
            dogDetector.AddAttribute<ICheckClearAttributesAttribute>();
            theDetector.AddAttribute<ICheckClearAttributesAttribute>();

            TeeSinkTokenFilter tee2 = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer2.ToString()), MockTokenizer.WHITESPACE, false));
            tee2.AddSinkTokenStream(dogDetector);
            tee2.AddSinkTokenStream(theDetector);
            TokenStream source2 = tee2;

            AssertTokenStreamContents(source1, tokens1);
            AssertTokenStreamContents(source2, tokens2);

            AssertTokenStreamContents(theDetector, new string[] { "The", "the", "The", "the" });
            AssertTokenStreamContents(dogDetector, new string[] { "Dogs", "Dogs" });

            source1.Reset();
            TokenStream lowerCasing = new LowerCaseFilter(TEST_VERSION_CURRENT, source1);
            string[] lowerCaseTokens = new string[tokens1.Length];
            for (int i = 0; i < tokens1.Length; i++)
            {
                lowerCaseTokens[i] = CultureInfo.InvariantCulture.TextInfo.ToLower(tokens1[i]);
            }
            AssertTokenStreamContents(lowerCasing, lowerCaseTokens);
        }

        /// <summary>
        /// Not an explicit test, just useful to print out some info on performance
        /// </summary>
        public virtual void Performance()
        {
            int[] tokCount = new int[] { 100, 500, 1000, 2000, 5000, 10000 };
            int[] modCounts = new int[] { 1, 2, 5, 10, 20, 50, 100, 200, 500 };
            for (int k = 0; k < tokCount.Length; k++)
            {
                StringBuilder buffer = new StringBuilder();
                Console.WriteLine("-----Tokens: " + tokCount[k] + "-----");
                for (int i = 0; i < tokCount[k]; i++)
                {
                    //buffer.Append(English.intToEnglish(i).toUpperCase(Locale.ROOT)).Append(' ');
                    buffer.Append(i.ToString(CultureInfo.InvariantCulture)).Append(' ');
                }
                //make sure we produce the same tokens
                TeeSinkTokenFilter teeStream = new TeeSinkTokenFilter(new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))));
                TokenStream sink = teeStream.NewSinkTokenStream(new ModuloSinkFilter(this, 100));
                teeStream.ConsumeAllTokens();
                TokenStream stream = new ModuloTokenFilter(this, new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))), 100);
                ICharTermAttribute tfTok = stream.AddAttribute<ICharTermAttribute>();
                ICharTermAttribute sinkTok = sink.AddAttribute<ICharTermAttribute>();
                for (int i = 0; stream.IncrementToken(); i++)
                {
                    assertTrue(sink.IncrementToken());
                    assertTrue(tfTok + " is not equal to " + sinkTok + " at token: " + i, tfTok.Equals(sinkTok) == true);
                }

                //simulate two fields, each being analyzed once, for 20 documents
                for (int j = 0; j < modCounts.Length; j++)
                {
                    int tfPos = 0;
                    //long start = DateTimeHelperClass.CurrentUnixTimeMillis();
                    long start = Environment.TickCount;
                    for (int i = 0; i < 20; i++)
                    {
                        stream = new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString())));
                        IPositionIncrementAttribute posIncrAtt = stream.GetAttribute<IPositionIncrementAttribute>();
                        while (stream.IncrementToken())
                        {
                            tfPos += posIncrAtt.PositionIncrement;
                        }
                        stream = new ModuloTokenFilter(this, new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))), modCounts[j]);
                        posIncrAtt = stream.GetAttribute<IPositionIncrementAttribute>();
                        while (stream.IncrementToken())
                        {
                            tfPos += posIncrAtt.PositionIncrement;
                        }
                    }
                    //long finish = DateTimeHelperClass.CurrentUnixTimeMillis();
                    long finish = Environment.TickCount;
                    Console.WriteLine("ModCount: " + modCounts[j] + " Two fields took " + (finish - start) + " ms");
                    int sinkPos = 0;
                    //simulate one field with one sink
                    //start = DateTimeHelperClass.CurrentUnixTimeMillis();
                    start = Environment.TickCount;
                    for (int i = 0; i < 20; i++)
                    {
                        teeStream = new TeeSinkTokenFilter(new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))));
                        sink = teeStream.NewSinkTokenStream(new ModuloSinkFilter(this, modCounts[j]));
                        IPositionIncrementAttribute posIncrAtt = teeStream.GetAttribute<IPositionIncrementAttribute>();
                        while (teeStream.IncrementToken())
                        {
                            sinkPos += posIncrAtt.PositionIncrement;
                        }
                        //System.out.println("Modulo--------");
                        posIncrAtt = sink.GetAttribute<IPositionIncrementAttribute>();
                        while (sink.IncrementToken())
                        {
                            sinkPos += posIncrAtt.PositionIncrement;
                        }
                    }
                    //finish = DateTimeHelperClass.CurrentUnixTimeMillis();
                    finish = Environment.TickCount;
                    Console.WriteLine("ModCount: " + modCounts[j] + " Tee fields took " + (finish - start) + " ms");
                    assertTrue(sinkPos + " does not equal: " + tfPos, sinkPos == tfPos);

                }
                Console.WriteLine("- End Tokens: " + tokCount[k] + "-----");
            }

        }


        internal class ModuloTokenFilter : TokenFilter
        {
            private readonly TestTeeSinkTokenFilter outerInstance;


            internal int modCount;

            internal ModuloTokenFilter(TestTeeSinkTokenFilter outerInstance, TokenStream input, int mc) : base(input)
            {
                this.outerInstance = outerInstance;
                modCount = mc;
            }

            internal int count = 0;

            //return every 100 tokens
            public override sealed bool IncrementToken()
            {
                bool hasNext;
                for (hasNext = input.IncrementToken(); hasNext && count % modCount != 0; hasNext = input.IncrementToken())
                {
                    count++;
                }
                count++;
                return hasNext;
            }
        }

        internal class ModuloSinkFilter : TeeSinkTokenFilter.SinkFilter
        {
            private readonly TestTeeSinkTokenFilter outerInstance;

            internal int count = 0;
            internal int modCount;

            internal ModuloSinkFilter(TestTeeSinkTokenFilter outerInstance, int mc)
            {
                this.outerInstance = outerInstance;
                modCount = mc;
            }

            public override bool Accept(AttributeSource a)
            {
                bool b = (a != null && count % modCount == 0);
                count++;
                return b;
            }

        }
    }
}