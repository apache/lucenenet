using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System.Collections.Generic;
using RandomizedTesting.Generators;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Test = NUnit.Framework.TestAttribute;

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

    /// <summary>
    /// Extends <see cref="BaseDocValuesFormatTestCase"/> to add compression checks. </summary>
    public abstract class BaseCompressingDocValuesFormatTestCase : BaseDocValuesFormatTestCase
    {
        internal static long DirSize(Directory d)
        {
            long size = 0;
            foreach (string file in d.ListAll())
            {
                size += d.FileLength(file);
            }
            return size;
        }

        [Test]
        public virtual void TestUniqueValuesCompression()
        {
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using Directory dir = new RAMDirectory();
            using IndexWriter iwriter = new IndexWriter(dir, iwc);
            int uniqueValueCount = TestUtil.NextInt32(Random, 1, 256);
            IList<long> values = new JCG.List<long>();

            Document doc = new Document();
            NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
            doc.Add(dvf);
            for (int i = 0; i < 300; ++i)
            {
                long value;
                if (values.Count < uniqueValueCount)
                {
                    value = Random.NextInt64();
                    values.Add(value);
                }
                else
                {
                    value = RandomPicks.RandomFrom(Random, values);
                }
                dvf.SetInt64Value(value);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size1 = DirSize(dir);
            for (int i = 0; i < 20; ++i)
            {
                dvf.SetInt64Value(RandomPicks.RandomFrom(Random, values));
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size2 = DirSize(dir);
            // make sure the new longs did not cost 8 bytes each
            Assert.IsTrue(size2 < size1 + 8 * 20);
        }

        [Test]
        public virtual void TestDateCompression()
        {
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using Directory dir = new RAMDirectory();
            using IndexWriter iwriter = new IndexWriter(dir, iwc);
            const long @base = 13; // prime
            long day = 1000L * 60 * 60 * 24;

            Document doc = new Document();
            NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
            doc.Add(dvf);
            for (int i = 0; i < 300; ++i)
            {
                dvf.SetInt64Value(@base + Random.Next(1000) * day);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size1 = DirSize(dir);
            for (int i = 0; i < 50; ++i)
            {
                dvf.SetInt64Value(@base + Random.Next(1000) * day);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size2 = DirSize(dir);
            // make sure the new longs costed less than if they had only been packed
            Assert.IsTrue(size2 < size1 + (PackedInt32s.BitsRequired(day) * 50) / 8);
        }

        [Test]
        public virtual void TestSingleBigValueCompression()
        {
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            using Directory dir = new RAMDirectory();
            using IndexWriter iwriter = new IndexWriter(dir, iwc);
            Document doc = new Document();
            NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
            doc.Add(dvf);
            for (int i = 0; i < 20000; ++i)
            {
                dvf.SetInt64Value(i & 1023);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size1 = DirSize(dir);
            dvf.SetInt64Value(long.MaxValue);
            iwriter.AddDocument(doc);
            iwriter.ForceMerge(1);
            long size2 = DirSize(dir);
            // make sure the new value did not grow the bpv for every other value
            Assert.IsTrue(size2 < size1 + (20000 * (63 - 10)) / 8);
        }
    }
}