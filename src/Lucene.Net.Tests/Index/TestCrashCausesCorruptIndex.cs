using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FilterDirectory = Lucene.Net.Store.FilterDirectory;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestCrashCausesCorruptIndex : LuceneTestCase
    {
        internal DirectoryInfo path;

        /// <summary>
        /// LUCENE-3627: this test fails.
        /// </summary>
        [Test]
        public virtual void TestCrashCorruptsIndexing()
        {
            path = CreateTempDir("testCrashCorruptsIndexing");

            IndexAndCrashOnCreateOutputSegments2();

            SearchForFleas(2);

            IndexAfterRestart();

            SearchForFleas(3);
        }

        /// <summary>
        /// index 1 document and commit.
        /// prepare for crashing.
        /// index 1 more document, and upon commit, creation of segments_2 will crash.
        /// </summary>
        private void IndexAndCrashOnCreateOutputSegments2()
        {
            Directory realDirectory = FSDirectory.Open(path);
            CrashAfterCreateOutput crashAfterCreateOutput = new CrashAfterCreateOutput(realDirectory);

            // NOTE: cannot use RandomIndexWriter because it
            // sometimes commits:
            IndexWriter indexWriter = new IndexWriter(crashAfterCreateOutput, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            indexWriter.AddDocument(Document);
            // writes segments_1:
            indexWriter.Commit();

            crashAfterCreateOutput.GetCrashAfterCreateOutput = "segments_2";
            indexWriter.AddDocument(Document);
            try
            {
                // tries to write segments_2 but hits fake exc:
                indexWriter.Commit();
                Assert.Fail("should have hit CrashingException");
            }
#pragma warning disable 168
            catch (CrashingException e)
#pragma warning restore 168
            {
                // expected
            }
            // writes segments_3
            indexWriter.Dispose();
            Assert.IsFalse(SlowFileExists(realDirectory, "segments_2"));
            crashAfterCreateOutput.Dispose();
        }

        /// <summary>
        /// Attempts to index another 1 document.
        /// </summary>
        private void IndexAfterRestart()
        {
            Directory realDirectory = NewFSDirectory(path);

            // LUCENE-3627 (before the fix): this line fails because
            // it doesn't know what to do with the created but empty
            // segments_2 file
            IndexWriter indexWriter = new IndexWriter(realDirectory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            // currently the test fails above.
            // however, to test the fix, the following lines should pass as well.
            indexWriter.AddDocument(Document);
            indexWriter.Dispose();
            Assert.IsFalse(SlowFileExists(realDirectory, "segments_2"));
            realDirectory.Dispose();
        }

        /// <summary>
        /// Run an example search.
        /// </summary>
        private void SearchForFleas(int expectedTotalHits)
        {
            Directory realDirectory = NewFSDirectory(path);
            IndexReader indexReader = DirectoryReader.Open(realDirectory);
            IndexSearcher indexSearcher = NewSearcher(indexReader);
            TopDocs topDocs = indexSearcher.Search(new TermQuery(new Term(TEXT_FIELD, "fleas")), 10);
            Assert.IsNotNull(topDocs);
            Assert.AreEqual(expectedTotalHits, topDocs.TotalHits);
            indexReader.Dispose();
            realDirectory.Dispose();
        }

        private const string TEXT_FIELD = "text";

        /// <summary>
        /// Gets a document with content "my dog has fleas".
        /// </summary>
        private Document Document
        {
            get
            {
                Document document = new Document();
                document.Add(NewTextField(TEXT_FIELD, "my dog has fleas", Field.Store.NO));
                return document;
            }
        }

        /// <summary>
        /// The marker RuntimeException that we use in lieu of an
        /// actual machine crash.
        /// </summary>
        private class CrashingException : Exception, IRuntimeException // LUCENENET specific: Added IRuntimeException for identification of the Java superclass in .NET
        {
            public CrashingException(string msg)
                : base(msg)
            {
            }
        }

        /// <summary>
        /// this test class provides direct access to "simulating" a crash right after
        /// realDirectory.CreateOutput(..) has been called on a certain specified name.
        /// </summary>
        private class CrashAfterCreateOutput : FilterDirectory
        {
            internal string crashAfterCreateOutput;

            public CrashAfterCreateOutput(Directory realDirectory)
                : base(realDirectory)
            {
                SetLockFactory(realDirectory.LockFactory);
            }

            public virtual string GetCrashAfterCreateOutput
            {
                set => this.crashAfterCreateOutput = value;
            }

            public override IndexOutput CreateOutput(string name, IOContext cxt)
            {
                IndexOutput indexOutput = m_input.CreateOutput(name, cxt);
                if (null != crashAfterCreateOutput && name.Equals(crashAfterCreateOutput, StringComparison.Ordinal))
                {
                    // CRASH!
                    indexOutput.Dispose();
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: now crash");
                        Console.WriteLine(new Exception().StackTrace);
                    }
                    throw new CrashingException("crashAfterCreateOutput " + crashAfterCreateOutput);
                }
                return indexOutput;
            }
        }
    }
}