using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.Text;

namespace Lucene.Net.Store
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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;

    [TestFixture]
    public class TestWindowsMMap : LuceneTestCase
    {
        private const string ALPHABET = "abcdefghijklmnopqrstuvwzyz";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private string RandomToken()
        {
            int tl = 1 + Random.Next(7);
            StringBuilder sb = new StringBuilder();
            for (int cx = 0; cx < tl; cx++)
            {
                int c = Random.Next(25);
                sb.Append(ALPHABET.Substring(c, 1));
            }
            return sb.ToString();
        }

        private string RandomField()
        {
            int fl = 1 + Random.Next(3);
            StringBuilder fb = new StringBuilder();
            for (int fx = 0; fx < fl; fx++)
            {
                fb.Append(RandomToken());
                fb.Append(' ');
            }
            return fb.ToString();
        }

        [Test]
        public virtual void TestMmapIndex()
        {
            // sometimes the directory is not cleaned by rmDir, because on Windows it
            // may take some time until the files are finally dereferenced. So clean the
            // directory up front, or otherwise new IndexWriter will fail.
            var dirPath = CreateTempDir("testLuceneMmap");
            RmDir(dirPath.FullName);
            var dir = new MMapDirectory(dirPath, null);

            // plan to add a set of useful stopwords, consider changing some of the
            // interior filters.
            using var analyzer = new MockAnalyzer(Random);
            // TODO: something about lock timeouts and leftover locks.
            using (var writer = new IndexWriter(dir,
                new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(
                    OpenMode.CREATE)))
            {
                writer.Commit();
                using IndexReader reader = DirectoryReader.Open(dir);
                var searcher = NewSearcher(reader);
                var num = AtLeast(1000);
                for (int dx = 0; dx < num; dx++)
                {
                    var f = RandomField();
                    var doc = new Document();
                    doc.Add(NewTextField("data", f, Field.Store.YES));
                    writer.AddDocument(doc);
                }
            }

            RmDir(dirPath.FullName);
        }

        private static void RmDir(string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                return;
            }

            foreach (string f in System.IO.Directory.GetFiles(dir))
            {
                System.IO.File.Delete(f);
            }

            System.IO.Directory.Delete(dir);
        }
    }
}