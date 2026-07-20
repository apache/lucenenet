using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

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
    /// Causes a bunch of fake OOM and checks that no other exceptions are delivered instead,
    /// no index corruption is ever created.
    /// </summary>
    [TestFixture]
    public class TestIndexWriterOutOfMemory : LuceneTestCase
    {
        // just one thread, serial merge policy, hopefully debuggable
        [Test]
        public void TestBasics()
        {
            // log all exceptions we hit, in case we fail (for debugging)
            MemoryStream exceptionLog = new MemoryStream();
            TextWriter exceptionStream = new StreamWriter(exceptionLog, Encoding.UTF8);
            //PrintStream exceptionStream = System.out;

            long analyzerSeed = Random.NextInt64();
            Analyzer analyzer = Analyzer.NewAnonymous((fieldName, reader) =>
            {
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                tokenizer.EnableChecks = false; // we are gonna make it angry
                TokenStream stream = tokenizer;
                // emit some payloads
                if (fieldName.Contains("payloads"))
                {
                    stream = new MockVariableLengthPayloadFilter(new J2N.Randomizer(analyzerSeed), stream); // LUCENENET specific - use J2N.Randomizer for long seed
                }

                return new TokenStreamComponents(tokenizer, stream);
            });

            MockDirectoryWrapper dir = null;

            int numIterations = TestNightly ? AtLeast(500) : AtLeast(20);

            // LUCENENET: STARTOVER label moved to end of for block, using `goto` for jump
            for (int iter = 0; iter < numIterations; iter++)
            {
                try
                {
                    // close from last run
                    if (dir != null)
                    {
                        dir.Dispose();
                    }

                    // disable slow things: we don't rely upon sleeps here.
                    dir = NewMockDirectory();
                    dir.Throttling = Throttling.NEVER;
                    dir.UseSlowOpenClosers = false;

                    IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
                    // just for now, try to keep this test reproducible
                    conf.MergeScheduler = new SerialMergeScheduler();

                    // test never makes it this far...
                    int numDocs = AtLeast(2000);

                    IndexWriter iw = new IndexWriter(dir, conf);

                    J2N.Randomizer r = new J2N.Randomizer(Random.NextInt64()); // LUCENENET: use J2N.Randomizer for long seed
                    dir.FailOn(new TestBasicsFailureAnonymousClass(r));

                    for (int i = 0; i < numDocs; i++)
                    {
                        Document doc = new Document();
                        doc.Add(NewStringField("id", i.ToString(CultureInfo.InvariantCulture), Field.Store.NO));
                        doc.Add(new NumericDocValuesField("dv", i));
                        doc.Add(new BinaryDocValuesField("dv2", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                        doc.Add(new SortedDocValuesField("dv3", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                        if (DefaultCodecSupportsSortedSet)
                        {
                            doc.Add(new SortedSetDocValuesField("dv4", new BytesRef(i.ToString(CultureInfo.InvariantCulture))));
                            doc.Add(new SortedSetDocValuesField("dv4", new BytesRef((i-1).ToString(CultureInfo.InvariantCulture))));
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
                            catch (Exception e) when (e.IsOutOfMemoryError())
                            {
                                if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError", StringComparison.Ordinal))
                                {
                                    exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
                                    e.PrintStackTrace(exceptionStream);
                                    try
                                    {
                                        iw.Rollback();
                                    }
                                    catch (Exception t) when (t.IsThrowable()) { }

                                    goto STARTOVER;
                                }
                                else
                                {
                                    throw; // Rethrow.rethrow(e);
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
                                if (Random.NextBoolean())
                                {
                                    iw.DeleteDocuments(new Term("id", i.ToString(CultureInfo.InvariantCulture)), new Term("id", (-i).ToString(CultureInfo.InvariantCulture)));
                                }
                            }
                            catch (Exception e) when (e.IsOutOfMemoryError())
                            {
                                if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError", StringComparison.Ordinal))
                                {
                                    exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
                                    e.PrintStackTrace(exceptionStream);
                                }
                                else
                                {
                                    throw; // Rethrow.rethrow(e);
                                }

                                try
                                {
                                    iw.Rollback();
                                }
                                catch (Exception t) when (t.IsThrowable()) { }

                                goto STARTOVER;
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
                            catch (Exception e) when (e.IsOutOfMemoryError())
                            {
                                if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError", StringComparison.Ordinal))
                                {
                                    exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
                                    e.PrintStackTrace(exceptionStream);
                                }
                                else
                                {
                                    throw; // Rethrow.rethrow(e);
                                }

                                try
                                {
                                    iw.Rollback();
                                }
                                catch (Exception t) when (t.IsThrowable()) { }

                                goto STARTOVER;
                            }
                        }
                    }

                    try
                    {
                        iw.Dispose();
                    }
                    catch (Exception e) when (e.IsOutOfMemoryError())
                    {
                        if (e.Message != null && e.Message.StartsWith("Fake OutOfMemoryError", StringComparison.Ordinal))
                        {
                            exceptionStream.WriteLine("\nTEST: got expected fake exc:" + e.Message);
                            e.PrintStackTrace(exceptionStream);
                            try
                            {
                                iw.Rollback();
                            }
                            catch (Exception t) when (t.IsThrowable()) {}
                            goto STARTOVER;
                        }
                        else
                        {
                            throw;  // Rethrow.rethrow(e);
                        }
                    }
                }
                catch (Exception ex) when (ex.IsThrowable())
                {
                    Console.WriteLine("Unexpected exception: dumping fake-exception-log:...");
                    exceptionStream.Flush();
                    Console.WriteLine(Encoding.UTF8.GetString(exceptionLog.ToArray()));
                    Console.Out.Flush();
                    throw; // Rethrow.ReThrow(ex);
                }

                STARTOVER:
                // ReSharper disable once RedundantJumpStatement - needed for label
                continue;
            }

            dir?.Dispose();
            if (Verbose)
            {
                Console.WriteLine("TEST PASSED: dumping fake-exception-log:...");
                Console.WriteLine(Encoding.UTF8.GetString(exceptionLog.ToArray()));
            }
        }

        private class TestBasicsFailureAnonymousClass(Random r) : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET: adjusted logic to use StackTrace instead of Exception
                StackTrace e = new();
                StackFrame[] stack = e.GetFrames();
                bool ok = false;
                for (int i = 0; i < stack.Length; i++)
                {
                    if (stack[i].GetMethod()?.DeclaringType?.Equals(typeof(IndexWriter)) == true)
                    {
                        ok = true;
                        // don't make life difficult though
                        if (stack[i].GetMethod()?.Name.Equals(nameof(IndexWriter.Rollback)) == true)
                        {
                            return;
                        }
                    }
                }
                if (ok && r.Next(3000) == 0)
                {
                    throw OutOfMemoryError.Create("Fake OutOfMemoryError");
                }
            }
        }
    }
}
