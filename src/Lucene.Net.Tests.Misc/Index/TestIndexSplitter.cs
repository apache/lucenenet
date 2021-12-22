using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

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
    public class TestIndexSplitter : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            DirectoryInfo dir = CreateTempDir(GetType().Name);
            DirectoryInfo destDir = CreateTempDir(GetType().Name);
            Store.Directory fsDir = NewFSDirectory(dir);
            // IndexSplitter.split makes its own commit directly with SIPC/SegmentInfos,
            // so the unreferenced files are expected.
            if (fsDir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)fsDir).AssertNoUnreferencedFilesOnDispose = (false);
            }

            MergePolicy mergePolicy = new LogByteSizeMergePolicy();
            mergePolicy.NoCFSRatio = 1.0;
            mergePolicy.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            IndexWriter iw = new IndexWriter(
                fsDir,
                new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).
                    SetOpenMode(OpenMode.CREATE).
                    SetMergePolicy(mergePolicy)
            );
            for (int x = 0; x < 100; x++)
            {
                Document doc = DocHelper.CreateDocument(x, "index", 5);
                iw.AddDocument(doc);
            }
            iw.Commit();
            for (int x = 100; x < 150; x++)
            {
                Document doc = DocHelper.CreateDocument(x, "index2", 5);
                iw.AddDocument(doc);
            }
            iw.Commit();
            for (int x = 150; x < 200; x++)
            {
                Document doc = DocHelper.CreateDocument(x, "index3", 5);
                iw.AddDocument(doc);
            }
            iw.Commit();
            DirectoryReader iwReader = iw.GetReader();
            assertEquals(3, iwReader.Leaves.Count);
            iwReader.Dispose();
            iw.Dispose();
            // we should have 2 segments now
            IndexSplitter @is = new IndexSplitter(dir);
            string splitSegName = @is.Infos[1].Info.Name;
            @is.Split(destDir, new string[] { splitSegName });
            Store.Directory fsDirDest = NewFSDirectory(destDir);
            DirectoryReader r = DirectoryReader.Open(fsDirDest);
            assertEquals(50, r.MaxDoc);
            r.Dispose();
            fsDirDest.Dispose();

            // now test cmdline
            DirectoryInfo destDir2 = CreateTempDir(GetType().Name);
            IndexSplitter.Main(new String[] { dir.FullName, destDir2.FullName, splitSegName });
            assertEquals(5, destDir2.GetFiles().Length);
            Store.Directory fsDirDest2 = NewFSDirectory(destDir2);
            r = DirectoryReader.Open(fsDirDest2);
            assertEquals(50, r.MaxDoc);
            r.Dispose();
            fsDirDest2.Dispose();

            // now remove the copied segment from src
            IndexSplitter.Main(new String[] { dir.FullName, "-d", splitSegName });
            r = DirectoryReader.Open(fsDir);
            assertEquals(2, r.Leaves.size());
            r.Dispose();
            fsDir.Dispose();
        }
    }
}
