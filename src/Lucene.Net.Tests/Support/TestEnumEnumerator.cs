/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

#pragma warning disable 612, 618
namespace Lucene.Net.Tests.Support
{
    [TestFixture]
    public class TestEnumEnumerator : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestTermsEnum()
        {
            BaseDirectory dir;

            using (dir = new RAMDirectory())
            {
                Document doc;
                IndexWriter writer;
                IndexReader reader;
                IndexWriterConfig conf = new IndexWriterConfig(
                    Util.LuceneVersion.LUCENE_CURRENT,
                    new WhitespaceAnalyzer(Util.LuceneVersion.LUCENE_CURRENT));

                using (writer = new IndexWriter(dir, conf /*new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED)*/))
                {
                    Field field = new TextField("name", "value", Field.Store.YES /*,Field.Index.ANALYZED*/);
                    doc = new Document();
                    doc.Add(field);
                    writer.AddDocument(doc);
                    writer.Commit();

                    using (reader = writer.GetReader())
                    {
                        Terms terms = MultiFields.GetTerms(reader, "name");
                        foreach (var bref in terms.GetIterator(null))
                        {
                            Assert.IsNotNull(bref);
                        }
                        Assert.AreEqual(1, terms.GetIterator(null).Count());
                    }
                }
            }
        }
    }
}
#pragma warning restore 612, 618
