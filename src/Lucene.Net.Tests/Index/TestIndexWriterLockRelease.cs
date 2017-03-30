using Lucene.Net.Support;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Index
{
    using Directory = Lucene.Net.Store.Directory;
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

    /// <summary>
    /// this tests the patch for issue #LUCENE-715 (IndexWriter does not
    /// release its write lock when trying to open an index which does not yet
    /// exist).
    /// </summary>
    [TestFixture]
    public class TestIndexWriterLockRelease : LuceneTestCase
    {
        [Test]
        public virtual void TestIndexWriterLockRelease_Mem()
        {
            Directory dir = NewFSDirectory(CreateTempDir("testLockRelease"));
            try
            {
                new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode.APPEND));
            }
#pragma warning disable 168
            catch (FileNotFoundException /*| NoSuchFileException*/ e)
#pragma warning restore 168
            {
                try
                {
                    new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode.APPEND));
                }
#pragma warning disable 168
                catch (FileNotFoundException /*| NoSuchFileException*/ e1)
#pragma warning restore 168
                {
                }
                // LUCENENET specific - since NoSuchDirectoryException subclasses FileNotFoundException
                // in Lucene, we need to catch it here to be on the safe side.
                catch (System.IO.DirectoryNotFoundException)
                {
                }
            }
            // LUCENENET specific - since NoSuchDirectoryException subclasses FileNotFoundException
            // in Lucene, we need to catch it here to be on the safe side.
            catch (System.IO.DirectoryNotFoundException)
            {
                try
                {
                    new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode.APPEND));
                }
#pragma warning disable 168
                catch (FileNotFoundException /*| NoSuchFileException*/ e1)
#pragma warning restore 168
                {
                }
                // LUCENENET specific - since NoSuchDirectoryException subclasses FileNotFoundException
                // in Lucene, we need to catch it here to be on the safe side.
                catch (System.IO.DirectoryNotFoundException)
                {
                }
            }
            finally
            {
                dir.Dispose();
            }
        }
    }
}