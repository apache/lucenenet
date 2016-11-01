using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Index
{
    using Attributes;
    using System.IO;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestCodecHoldsOpenFiles : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewField("foo", "bar", TextField.TYPE_NOT_STORED));
                w.AddDocument(doc);
            }

            IndexReader r = w.Reader;
            w.Dispose();

            foreach (string fileName in d.ListAll())
            {
                try
                {
                    d.DeleteFile(fileName);
                }
                catch (IOException ioe)
                {
                    // ignore: this means codec (correctly) is holding
                    // the file open
                }
            }

            foreach (AtomicReaderContext cxt in r.Leaves)
            {
                TestUtil.CheckReader(cxt.Reader);
            }

            r.Dispose();
            d.Dispose();
        }

        [Test, LuceneNetSpecific] // Apparently added to LUCENENET for debugging
        public virtual void TestExposeUnclosedFiles()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            //int numDocs = AtLeast(100);
            int numDocs = 5;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewField("foo", "bar", TextField.TYPE_NOT_STORED));
                w.AddDocument(doc);
            }

            IndexReader r = w.Reader;
            w.Dispose();

            foreach (string fileName in d.ListAll())
            {
                try
                {
                    d.DeleteFile(fileName);
                }
                catch (IOException ioe)
                {
                    // ignore: this means codec (correctly) is holding
                    // the file open
                }
            }

            foreach (AtomicReaderContext cxt in r.Leaves)
            {
                TestUtil.CheckReader(cxt.Reader);
            }

            r.Dispose();
            d.Dispose();
        }
    }
}