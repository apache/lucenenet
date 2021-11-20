using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    /*
      Verify we can read the pre-2.1 file format, do searches
      against it, and add documents to it.
    */

    [TestFixture]
    public class TestIndexFileDeleter : LuceneTestCase
    {
        [Test]
        public virtual void TestDeleteLeftoverFiles()
        {
            Directory dir = NewDirectory();
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).PreventDoubleWrite = false;
            }

            MergePolicy mergePolicy = NewLogMergePolicy(true, 10);

            // this test expects all of its segments to be in CFS
            mergePolicy.NoCFSRatio = 1.0;
            mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(10).SetMergePolicy(mergePolicy).SetUseCompoundFile(true));

            int i;
            for (i = 0; i < 35; i++)
            {
                AddDoc(writer, i);
            }
            writer.Config.MergePolicy.NoCFSRatio = 0.0;
            writer.Config.SetUseCompoundFile(false);
            for (; i < 45; i++)
            {
                AddDoc(writer, i);
            }
            writer.Dispose();

            // Delete one doc so we get a .del file:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetUseCompoundFile(true));
            Term searchTerm = new Term("id", "7");
            writer.DeleteDocuments(searchTerm);
            writer.Dispose();

            // Now, artificially create an extra .del file & extra
            // .s0 file:
            string[] files = dir.ListAll();

            /*
            for(int j=0;j<files.Length;j++) {
              System.out.println(j + ": " + files[j]);
            }
            */

            // TODO: fix this test better
            string ext = Codec.Default.Name.Equals("SimpleText", StringComparison.Ordinal) ? ".liv" : ".del";

            // Create a bogus separate del file for a
            // segment that already has a separate del file:
            CopyFile(dir, "_0_1" + ext, "_0_2" + ext);

            // Create a bogus separate del file for a
            // segment that does not yet have a separate del file:
            CopyFile(dir, "_0_1" + ext, "_1_1" + ext);

            // Create a bogus separate del file for a
            // non-existent segment:
            CopyFile(dir, "_0_1" + ext, "_188_1" + ext);

            // Create a bogus segment file:
            CopyFile(dir, "_0.cfs", "_188.cfs");

            // Create a bogus fnm file when the CFS already exists:
            CopyFile(dir, "_0.cfs", "_0.fnm");

            // Create some old segments file:
            CopyFile(dir, "segments_2", "segments");
            CopyFile(dir, "segments_2", "segments_1");

            // Create a bogus cfs file shadowing a non-cfs segment:

            // TODO: assert is bogus (relies upon codec-specific filenames)
            Assert.IsTrue(SlowFileExists(dir, "_3.fdt") || SlowFileExists(dir, "_3.fld"));
            Assert.IsTrue(!SlowFileExists(dir, "_3.cfs"));
            CopyFile(dir, "_1.cfs", "_3.cfs");

            string[] filesPre = dir.ListAll();

            // Open & close a writer: it should delete the above 4
            // files and nothing more:
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            writer.Dispose();

            string[] files2 = dir.ListAll();
            dir.Dispose();

            Array.Sort(files, StringComparer.Ordinal);
            Array.Sort(files2, StringComparer.Ordinal);

            ISet<string> dif = DifFiles(files, files2);

            if (!Arrays.Equals(files, files2))
            {
                Assert.Fail("IndexFileDeleter failed to delete unreferenced extra files: should have deleted " + (filesPre.Length - files.Length) + " files but only deleted " + (filesPre.Length - files2.Length) + "; expected files:\n    " + AsString(files) + "\n  actual files:\n    " + AsString(files2) + "\ndiff: " + dif);
            }
        }

        private static ISet<string> DifFiles(string[] files1, string[] files2)
        {
            ISet<string> set1 = new JCG.HashSet<string>();
            ISet<string> set2 = new JCG.HashSet<string>();
            ISet<string> extra = new JCG.HashSet<string>();

            for (int x = 0; x < files1.Length; x++)
            {
                set1.Add(files1[x]);
            }
            for (int x = 0; x < files2.Length; x++)
            {
                set2.Add(files2[x]);
            }
            IEnumerator<string> i1 = set1.GetEnumerator();
            while (i1.MoveNext())
            {
                string o = i1.Current;
                if (!set2.Contains(o))
                {
                    extra.Add(o);
                }
            }
            IEnumerator<string> i2 = set2.GetEnumerator();
            while (i2.MoveNext())
            {
                string o = i2.Current;
                if (!set1.Contains(o))
                {
                    extra.Add(o);
                }
            }
            return extra;
        }

        private string AsString(string[] l)
        {
            string s = "";
            for (int i = 0; i < l.Length; i++)
            {
                if (i > 0)
                {
                    s += "\n    ";
                }
                s += l[i];
            }
            return s;
        }

        public virtual void CopyFile(Directory dir, string src, string dest)
        {
            IndexInput @in = dir.OpenInput(src, NewIOContext(Random));
            IndexOutput @out = dir.CreateOutput(dest, NewIOContext(Random));
            var b = new byte[1024];
            long remainder = @in.Length;
            while (remainder > 0)
            {
                int len = (int)Math.Min(b.Length, remainder);
                @in.ReadBytes(b, 0, len);
                @out.WriteBytes(b, len);
                remainder -= len;
            }
            @in.Dispose();
            @out.Dispose();
        }

        private void AddDoc(IndexWriter writer, int id)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", "aaa", Field.Store.NO));
            doc.Add(NewStringField("id", Convert.ToString(id), Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}