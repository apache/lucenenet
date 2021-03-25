// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestEmptyTokenStream : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestConsume()
        {
            TokenStream ts = new EmptyTokenStream();
            ts.Reset();
            assertFalse(ts.IncrementToken());
            ts.End();
            ts.Dispose();
            // try again with reuse:
            ts.Reset();
            assertFalse(ts.IncrementToken());
            ts.End();
            ts.Dispose();
        }

        [Test]
        public virtual void TestConsume2()
        {
            BaseTokenStreamTestCase.AssertTokenStreamContents(new EmptyTokenStream(), new string[0]);
        }

        [Test]
        public virtual void TestIndexWriter_LUCENE4656()
        {
            Store.Directory directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));

            TokenStream ts = new EmptyTokenStream();
            assertFalse(ts.HasAttribute<ITermToBytesRefAttribute>());

            Document doc = new Document();
            doc.Add(new StringField("id", "0", Field.Store.YES));
            doc.Add(new TextField("description", ts));

            // this should not fail because we have no TermToBytesRefAttribute
            writer.AddDocument(doc);

            assertEquals(1, writer.NumDocs);

            writer.Dispose();
            directory.Dispose();
        }
    }
}