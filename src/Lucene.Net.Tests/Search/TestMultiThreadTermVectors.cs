using J2N.Threading;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search
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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using English = Lucene.Net.Util.English;
    using Fields = Lucene.Net.Index.Fields;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    [TestFixture]
    public class TestMultiThreadTermVectors : LuceneTestCase
    {
        private Directory directory;
        public int numDocs = 100;
        public int numThreads = 3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            //writer.setNoCFSRatio(0.0);
            //writer.infoStream = System.out;
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IsTokenized = false;
            customType.StoreTermVectors = true;
            for (int i = 0; i < numDocs; i++)
            {
                Documents.Document doc = new Documents.Document();
                Field fld = NewField("field", English.Int32ToEnglish(i), customType);
                doc.Add(fld);
                writer.AddDocument(doc);
            }
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            directory.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            IndexReader reader = null;

            try
            {
                reader = DirectoryReader.Open(directory);
                for (int i = 1; i <= numThreads; i++)
                {
                    TestTermPositionVectors(reader, i);
                }
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                Assert.Fail(ioe.Message);
            }
            finally
            {
                if (reader != null)
                {
                    try
                    {
                        /// <summary>
                        /// close the opened reader </summary>
                        reader.Dispose();
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        Console.WriteLine(ioe.ToString());
                        Console.Write(ioe.StackTrace);
                    }
                }
            }
        }

        public virtual void TestTermPositionVectors(IndexReader reader, int threadCount)
        {
            MultiThreadTermVectorsReader[] mtr = new MultiThreadTermVectorsReader[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                mtr[i] = new MultiThreadTermVectorsReader();
                mtr[i].Init(reader);
            }

            // run until all threads finished
            int threadsAlive = mtr.Length;
            while (threadsAlive > 0)
            {
                //System.out.println("Threads alive");
                Thread.Sleep(10);
                threadsAlive = mtr.Length;
                for (int i = 0; i < mtr.Length; i++)
                {
                    if (mtr[i].Alive == true)
                    {
                        break;
                    }

                    threadsAlive--;
                }
            }

            long totalTime = 0L;
            for (int i = 0; i < mtr.Length; i++)
            {
                totalTime += mtr[i].timeElapsed;
                mtr[i] = null;
            }

            //System.out.println("threadcount: " + mtr.Length + " average term vector time: " + totalTime/mtr.Length);
        }
    }

    internal class MultiThreadTermVectorsReader //: IThreadRunnable
    {
        private IndexReader reader = null;
        private ThreadJob t = null;

        private readonly int runsToDo = 100;
        internal long timeElapsed = 0;

        public virtual void Init(IndexReader reader)
        {
            this.reader = reader;
            timeElapsed = 0;
            t = new ThreadJob(new System.Threading.ThreadStart(this.Run));
            t.Start();
        }

        public virtual bool Alive
        {
            get
            {
                if (t is null)
                {
                    return false;
                }

                return t.IsAlive;
            }
        }

        public void Run()
        {
            try
            {
                // run the test 100 times
                for (int i = 0; i < runsToDo; i++)
                {
                    TestTermVectors();
                }
            }
            catch (Exception e) when (e.IsException())
            {
                e.printStackTrace();
            }
            return;
        }

        private void TestTermVectors()
        {
            // check:
            int numDocs = reader.NumDocs;
            long start = 0L;
            for (int docId = 0; docId < numDocs; docId++)
            {
                start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                Fields vectors = reader.GetTermVectors(docId);
                timeElapsed += (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

                // verify vectors result
                VerifyVectors(vectors, docId);

                start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                Terms vector = reader.GetTermVectors(docId).GetTerms("field");
                timeElapsed += (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

                VerifyVector(vector.GetEnumerator(), docId);
            }
        }

        private void VerifyVectors(Fields vectors, int num)
        {
            foreach (string field in vectors)
            {
                Terms terms = vectors.GetTerms(field);
                if (Debugging.AssertsEnabled) Debugging.Assert(terms != null);
                VerifyVector(terms.GetEnumerator(), num);
            }
        }

        private void VerifyVector(TermsEnum vector, int num)
        {
            StringBuilder temp = new StringBuilder();
            while (vector.MoveNext())
            {
                temp.Append(vector.Term.Utf8ToString());
            }
            if (!English.Int32ToEnglish(num).Trim().Equals(temp.ToString().Trim(), StringComparison.Ordinal))
            {
                Console.WriteLine("wrong term result");
            }
        }
    }
}