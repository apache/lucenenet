using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Asserting;
using Lucene.Net.Codecs.Cranky;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
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
    /// Causes a bunch of non-aborting and aborting exceptions and checks that
    /// no index corruption is ever created
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    public class TestIndexWriterExceptions2 : LuceneTestCase
    {
        [Test]
        public void TestBasics()
        {
            // disable slow things: we don't rely upon sleeps here.
            Directory dir = NewDirectory();
            if (dir is MockDirectoryWrapper wrapper) {
                wrapper.Throttling = Throttling.NEVER;
                wrapper.UseSlowOpenClosers = false;
            }

            // log all exceptions we hit, in case we fail (for debugging)
            ByteArrayOutputStream exceptionLog = new ByteArrayOutputStream();
            TextWriter exceptionStream = new StreamWriter(exceptionLog, Encoding.UTF8);
            //PrintStream exceptionStream = System.out;

            // create lots of non-aborting exceptions with a broken analyzer
            long analyzerSeed = Random.NextInt64();
            Analyzer analyzer = Analyzer.NewAnonymous((fieldName, reader) =>
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, false);
                tokenizer.EnableChecks = false; // TODO: can we turn this on? our filter is probably too evil
                TokenStream stream = tokenizer;
                // emit some payloads
                if (fieldName.Contains("payloads"))
                {
                    stream = new MockVariableLengthPayloadFilter(new Random((int)analyzerSeed), stream); // LUCENENET specific - cast seed to int
                }
                stream = new CrankyTokenFilter(stream, new Random((int)analyzerSeed)); // LUCENENET specific - cast seed to int
                return new TokenStreamComponents(tokenizer, stream);
            });

            // create lots of aborting exceptions with a broken codec
            // we don't need a random codec, as we aren't trying to find bugs in the codec here.
            Codec inner = RandomMultiplier > 1 ? Codec.Default : new AssertingCodec();
            Codec codec = new CrankyCodec(inner, new Random(Random.Next())); // LUCENENET specific - use int for seed

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            // just for now, try to keep this test reproducible
            conf.MergeScheduler = new SerialMergeScheduler();
            conf.Codec = codec;

            int numDocs = AtLeast(2000);
            IndexWriter iw = new IndexWriter(dir, conf);
            try
            {
                for (int i = 0; i < numDocs; i++)
                {
                    // TODO: add crankyDocValuesFields, etc
                    Document doc = new Document();
                    doc.Add(NewStringField("id", i.ToString(CultureInfo.InvariantCulture), Field.Store.NO));
                    doc.Add(new NumericDocValuesField("dv", i));
                    doc.Add(new BinaryDocValuesField("dv2", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                    doc.Add(new SortedDocValuesField("dv3", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                    if (DefaultCodecSupportsSortedSet)
                    {
                        doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                        doc.Add(new SortedSetDocValuesField("dv4", new BytesRef((i - 1).ToString(CultureInfo.InvariantCulture))));
                    }

                    doc.Add(NewTextField("text1", TestUtil.RandomAnalysisString(Random, 20, true), Field.Store.NO));
                    // ensure we store something
                    doc.Add(new StoredField("stored1", "foo"));
                    doc.Add(new StoredField("stored1", "bar"));
                    // ensure we get some payloads
                    doc.Add(NewTextField("text_payloads", TestUtil.RandomAnalysisString(Random, 6, true), Field.Store.NO));
                    // ensure we get some vectors
                    FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                    ft.StoreTermVectors = true;
                    doc.Add(NewField("text_vectors", TestUtil.RandomAnalysisString(Random, 6, true), ft));

                    if (Random.Next(10) > 0)
                    {
                        // single doc
                        try
                        {
                            iw.AddDocument(doc);
                            // we made it, sometimes delete our doc, or update a dv
                            int thingToDo = Random.Next(4);
                            if (thingToDo == 0)
                            {
                                iw.DeleteDocuments(new Term("id", i.ToString(CultureInfo.InvariantCulture)));
                            }
                            else if (thingToDo == 1 && DefaultCodecSupportsFieldUpdates)
                            {
                                iw.UpdateNumericDocValue(new Term("id", i.ToString(CultureInfo.InvariantCulture)), "dv", i + 1L);
                            }
                            else if (thingToDo == 2 && DefaultCodecSupportsFieldUpdates)
                            {
                                iw.UpdateBinaryDocValue(new Term("id", i.ToString(CultureInfo.InvariantCulture)), "dv2", new BytesRef((i + 1).ToString(CultureInfo.InvariantCulture)));
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.Message != null && e.Message.StartsWith("Fake IOException", StringComparison.Ordinal))
                            {
                                exceptionStream.WriteLine($"\nTEST: got expected fake exc:{e.Message}");
                                e.PrintStackTrace(exceptionStream);
                            }
                            else
                            {
                                throw; // LUCENENET: was Rethrow.rethrow(e);
                            }
                        }
                    }
                    else
                    {
                        // block docs
                        Document doc2 = new Document();
                        doc2.Add(NewStringField("id", (-i).ToString(CultureInfo.InvariantCulture), Field.Store.NO));
                        doc2.Add(NewTextField("text1", TestUtil.RandomAnalysisString(Random, 20, true), Field.Store.NO));
                        doc2.Add(new StoredField("stored1", "foo"));
                        doc2.Add(new StoredField("stored1", "bar"));
                        doc2.Add(NewField("text_vectors", TestUtil.RandomAnalysisString(Random, 6, true), ft));

                        try
                        {
                            iw.AddDocuments([doc, doc2]);
                            // we made it, sometimes delete our docs
                            if (Random.nextBoolean())
                            {
                                iw.DeleteDocuments(new Term("id", i.ToString(CultureInfo.InvariantCulture)), new Term("id", (-i).ToString(CultureInfo.InvariantCulture)));
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.Message != null && e.Message.StartsWith("Fake IOException", StringComparison.Ordinal))
                            {
                                exceptionStream.WriteLine($"\nTEST: got expected fake exc:{e.Message}");
                                e.PrintStackTrace(exceptionStream);
                            }
                            else
                            {
                                throw; // LUCENENET: was Rethrow.rethrow(e);
                            }
                        }
                    }

                    if (Random.Next(10) == 0)
                    {
                        // trigger flush:
                        try
                        {
                            if (Random.NextBoolean())
                            {
                                DirectoryReader ir = null;
                                try
                                {
                                    ir = DirectoryReader.Open(iw, Random.NextBoolean());
                                    TestUtil.CheckReader(ir);
                                }
                                finally
                                {
                                    IOUtils.DisposeWhileHandlingException(ir);
                                }
                            }
                            else
                            {
                                iw.Commit();
                            }

                            if (DirectoryReader.IndexExists(dir))
                            {
                                TestUtil.CheckIndex(dir);
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.Message != null && e.Message.StartsWith("Fake IOException", StringComparison.Ordinal))
                            {
                                exceptionStream.WriteLine($"\nTEST: got expected fake exc:{e.Message}");
                                e.PrintStackTrace(exceptionStream);
                            }
                            else
                            {
                                throw; // LUCENENET: was Rethrow.rethrow(e);
                            }
                        }
                    }
                }

                try
                {
                    iw.Dispose();
                }
                catch (Exception e)
                {
                    if (e.Message != null && e.Message.StartsWith("Fake IOException", StringComparison.Ordinal))
                    {
                        exceptionStream.WriteLine($"\nTEST: got expected fake exc:{e.Message}");
                        e.PrintStackTrace(exceptionStream);
                        try
                        {
                            iw.Rollback();
                        }
                        catch (Exception t) when (t.IsThrowable()) { }
                    }
                    else
                    {
                        throw; // LUCENENET: was Rethrow.rethrow(e);
                    }
                }

                dir.Dispose();
            }
            catch (Exception t) when (t.IsThrowable())
            {
                Console.Out.WriteLine("Unexpected exception: dumping fake-exception-log:...");
                exceptionStream.Flush();
                Console.Out.WriteLine(exceptionLog.ToString());
                Console.Out.Flush();
                throw; // LUCENENET: was Rethrow.rethrow(t);
            }

            if (Verbose)
            {
                Console.Out.WriteLine("TEST PASSED: dumping fake-exception-log:...");
                Console.Out.WriteLine(exceptionLog.ToString());
            }
        }
    }
}
