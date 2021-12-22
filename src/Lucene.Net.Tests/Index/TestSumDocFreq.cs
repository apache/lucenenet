using System;
using Lucene.Net.Documents;
using NUnit.Framework;
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
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests <seealso cref="Terms#getSumDocFreq()"/>
    /// @lucene.experimental
    /// </summary>
    [TestFixture]
    public class TestSumDocFreq : LuceneTestCase
    {
        [Test]
        public virtual void TestSumDocFreq_Mem()
        {
            int numDocs = AtLeast(500);

            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            Field id = NewStringField("id", "", Field.Store.NO);
            Field field1 = NewTextField("foo", "", Field.Store.NO);
            Field field2 = NewTextField("bar", "", Field.Store.NO);
            doc.Add(id);
            doc.Add(field1);
            doc.Add(field2);
            for (int i = 0; i < numDocs; i++)
            {
                id.SetStringValue("" + i);
                char ch1 = (char)TestUtil.NextInt32(Random, 'a', 'z');
                char ch2 = (char)TestUtil.NextInt32(Random, 'a', 'z');
                field1.SetStringValue("" + ch1 + " " + ch2);
                ch1 = (char)TestUtil.NextInt32(Random, 'a', 'z');
                ch2 = (char)TestUtil.NextInt32(Random, 'a', 'z');
                field2.SetStringValue("" + ch1 + " " + ch2);
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();

            AssertSumDocFreq(ir);
            ir.Dispose();

            int numDeletions = AtLeast(20);
            for (int i = 0; i < numDeletions; i++)
            {
                writer.DeleteDocuments(new Term("id", "" + Random.Next(numDocs)));
            }
            writer.ForceMerge(1);
            writer.Dispose();

            ir = DirectoryReader.Open(dir);
            AssertSumDocFreq(ir);
            ir.Dispose();
            dir.Dispose();
        }

        private void AssertSumDocFreq(IndexReader ir)
        {
            // compute sumDocFreq across all fields
            Fields fields = MultiFields.GetFields(ir);

            foreach (string f in fields)
            {
                Terms terms = fields.GetTerms(f);
                long sumDocFreq = terms.SumDocFreq;
                if (sumDocFreq == -1)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("skipping field: " + f + ", codec does not support sumDocFreq");
                    }
                    continue;
                }

                long computedSumDocFreq = 0;
                TermsEnum termsEnum = terms.GetEnumerator();
                while (termsEnum.MoveNext())
                {
                    computedSumDocFreq += termsEnum.DocFreq;
                }
                Assert.AreEqual(computedSumDocFreq, sumDocFreq);
            }
        }
    }
}