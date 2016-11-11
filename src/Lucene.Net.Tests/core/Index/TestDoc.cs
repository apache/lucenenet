using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using System.IO;
    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using TextField = TextField;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    /// JUnit adaptation of an older test case DocTest. </summary>
    [TestFixture]
    public class TestDoc : LuceneTestCase
    {
        private DirectoryInfo WorkDir;
        private DirectoryInfo IndexDir;
        private LinkedList<FileInfo> Files;

        /// <summary>
        /// Set the test case. this test case needs
        ///  a few text files created in the current working directory.
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            if (VERBOSE)
            {
                Console.WriteLine("TEST: setUp");
            }
            WorkDir = CreateTempDir("TestDoc");

            IndexDir = CreateTempDir("testIndex");

            Directory directory = NewFSDirectory(IndexDir);
            directory.Dispose();

            Files = new LinkedList<FileInfo>();
            Files.AddLast(CreateOutput("test.txt", "this is the first test file"));

            Files.AddLast(CreateOutput("test2.txt", "this is the second test file"));
        }

        private FileInfo CreateOutput(string name, string text)
        {
            //TextWriter fw = null;
            StreamWriter pw = null;

            try
            {
                FileInfo f = new FileInfo(Path.Combine(WorkDir.FullName, name));
                if (f.Exists)
                {
                    f.Delete();
                }

                //fw = new StreamWriter(new FileOutputStream(f), IOUtils.CHARSET_UTF_8);
                pw = new StreamWriter(File.Open(f.FullName, FileMode.OpenOrCreate));
                pw.WriteLine(text);
                return f;
            }
            finally
            {
                if (pw != null)
                {
                    pw.Dispose();
                }
                /*if (fw != null)
                {
                    fw.Dispose();
                }*/
            }
        }

        /// <summary>
        /// this test executes a number of merges and compares the contents of
        ///  the segments created when using compound file or not using one.
        ///
        ///  TODO: the original test used to print the segment contents to System.out
        ///        for visual validation. To have the same effect, a new method
        ///        checkSegment(String name, ...) should be created that would
        ///        assert various things about the segment.
        /// </summary>
        [Test]
        public virtual void TestIndexAndMerge()
        {
            MemoryStream sw = new MemoryStream();
            StreamWriter @out = new StreamWriter(sw);

            Directory directory = NewFSDirectory(IndexDir, null);

            MockDirectoryWrapper wrapper = directory as MockDirectoryWrapper;
            if (wrapper != null)
            {
                // We create unreferenced files (we don't even write
                // a segments file):
                wrapper.AssertNoUnrefencedFilesOnClose = false;
            }

            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(10)));

            SegmentCommitInfo si1 = IndexDoc(writer, "test.txt");
            PrintSegment(@out, si1);

            SegmentCommitInfo si2 = IndexDoc(writer, "test2.txt");
            PrintSegment(@out, si2);
            writer.Dispose();

            SegmentCommitInfo siMerge = Merge(directory, si1, si2, "_merge", false);
            PrintSegment(@out, siMerge);

            SegmentCommitInfo siMerge2 = Merge(directory, si1, si2, "_merge2", false);
            PrintSegment(@out, siMerge2);

            SegmentCommitInfo siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", false);
            PrintSegment(@out, siMerge3);

            directory.Dispose();
            @out.Dispose();
            sw.Dispose();

            string multiFileOutput = sw.ToString();
            //System.out.println(multiFileOutput);

            sw = new MemoryStream();
            @out = new StreamWriter(sw);

            directory = NewFSDirectory(IndexDir, null);

            wrapper = directory as MockDirectoryWrapper;
            if (wrapper != null)
            {
                // We create unreferenced files (we don't even write
                // a segments file):
                wrapper.AssertNoUnrefencedFilesOnClose = false;
            }

            writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(10)));

            si1 = IndexDoc(writer, "test.txt");
            PrintSegment(@out, si1);

            si2 = IndexDoc(writer, "test2.txt");
            PrintSegment(@out, si2);
            writer.Dispose();

            siMerge = Merge(directory, si1, si2, "_merge", true);
            PrintSegment(@out, siMerge);

            siMerge2 = Merge(directory, si1, si2, "_merge2", true);
            PrintSegment(@out, siMerge2);

            siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", true);
            PrintSegment(@out, siMerge3);

            directory.Dispose();
            @out.Dispose();
            sw.Dispose();
            string singleFileOutput = sw.ToString();

            Assert.AreEqual(multiFileOutput, singleFileOutput);
        }

        private SegmentCommitInfo IndexDoc(IndexWriter writer, string fileName)
        {
            FileInfo file = new FileInfo(Path.Combine(WorkDir.FullName, fileName));
            Document doc = new Document();
            StreamReader @is = new StreamReader(File.Open(file.FullName, FileMode.Open));
            doc.Add(new TextField("contents", @is));
            writer.AddDocument(doc);
            writer.Commit();
            @is.Dispose();
            return writer.NewestSegment();
        }

        private SegmentCommitInfo Merge(Directory dir, SegmentCommitInfo si1, SegmentCommitInfo si2, string merged, bool useCompoundFile)
        {
            IOContext context = NewIOContext(Random());
            SegmentReader r1 = new SegmentReader(si1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, context);
            SegmentReader r2 = new SegmentReader(si2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, context);

            Codec codec = Codec.Default;
            TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(si1.Info.Dir);
            SegmentInfo si = new SegmentInfo(si1.Info.Dir, Constants.LUCENE_MAIN_VERSION, merged, -1, false, codec, null);

            SegmentMerger merger = new SegmentMerger(Arrays.AsList<AtomicReader>(r1, r2), si, InfoStream.Default, trackingDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), context, true);

            MergeState mergeState = merger.Merge();
            r1.Dispose();
            r2.Dispose();
            SegmentInfo info = new SegmentInfo(si1.Info.Dir, Constants.LUCENE_MAIN_VERSION, merged, si1.Info.DocCount + si2.Info.DocCount, false, codec, null);
            info.Files = new HashSet<string>(trackingDir.CreatedFiles);

            if (useCompoundFile)
            {
                ICollection<string> filesToDelete = IndexWriter.CreateCompoundFile(InfoStream.Default, dir, MergeState.CheckAbort.NONE, info, NewIOContext(Random()));
                info.UseCompoundFile = true;
                foreach (String fileToDelete in filesToDelete)
                {
                    si1.Info.Dir.DeleteFile(fileToDelete);
                }
            }

            return new SegmentCommitInfo(info, 0, -1L, -1L);
        }

        private void PrintSegment(StreamWriter @out, SegmentCommitInfo si)
        {
            SegmentReader reader = new SegmentReader(si, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));

            for (int i = 0; i < reader.NumDocs; i++)
            {
                @out.WriteLine(reader.Document(i));
            }

            Fields fields = reader.Fields;
            foreach (string field in fields)
            {
                Terms terms = fields.Terms(field);
                Assert.IsNotNull(terms);
                TermsEnum tis = terms.Iterator(null);
                while (tis.Next() != null)
                {
                    @out.Write("  term=" + field + ":" + tis.Term());
                    @out.WriteLine("    DF=" + tis.DocFreq());

                    DocsAndPositionsEnum positions = tis.DocsAndPositions(reader.LiveDocs, null);

                    while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        @out.Write(" doc=" + positions.DocID());
                        @out.Write(" TF=" + positions.Freq());
                        @out.Write(" pos=");
                        @out.Write(positions.NextPosition());
                        for (int j = 1; j < positions.Freq(); j++)
                        {
                            @out.Write("," + positions.NextPosition());
                        }
                        @out.WriteLine("");
                    }
                }
            }
            reader.Dispose();
        }
    }
}