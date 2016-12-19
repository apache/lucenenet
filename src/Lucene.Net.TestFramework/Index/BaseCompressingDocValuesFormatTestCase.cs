using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;

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
    using NumericDocValuesField = NumericDocValuesField;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Extends <seealso cref="BaseDocValuesFormatTestCase"/> to add compression checks. </summary>
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

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestUniqueValuesCompression()
        {
            Directory dir = new RAMDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter iwriter = new IndexWriter(dir, iwc);

            int uniqueValueCount = TestUtil.NextInt(Random(), 1, 256);
            IList<long> values = new List<long>();

            Document doc = new Document();
            NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
            doc.Add(dvf);
            for (int i = 0; i < 300; ++i)
            {
                long value;
                if (values.Count < uniqueValueCount)
                {
                    value = Random().NextLong();
                    values.Add(value);
                }
                else
                {
                    value = RandomInts.RandomFrom(Random(), values);
                }
                dvf.SetInt64Value(value);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size1 = DirSize(dir);
            for (int i = 0; i < 20; ++i)
            {
                dvf.SetInt64Value(RandomInts.RandomFrom(Random(), values));
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size2 = DirSize(dir);
            // make sure the new longs did not cost 8 bytes each
            Assert.IsTrue(size2 < size1 + 8 * 20);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDateCompression()
        {
            Directory dir = new RAMDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter iwriter = new IndexWriter(dir, iwc);

            const long @base = 13; // prime
            long day = 1000L * 60 * 60 * 24;

            Document doc = new Document();
            NumericDocValuesField dvf = new NumericDocValuesField("dv", 0);
            doc.Add(dvf);
            for (int i = 0; i < 300; ++i)
            {
                dvf.SetInt64Value(@base + Random().Next(1000) * day);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size1 = DirSize(dir);
            for (int i = 0; i < 50; ++i)
            {
                dvf.SetInt64Value(@base + Random().Next(1000) * day);
                iwriter.AddDocument(doc);
            }
            iwriter.ForceMerge(1);
            long size2 = DirSize(dir);
            // make sure the new longs costed less than if they had only been packed
            Assert.IsTrue(size2 < size1 + (PackedInts.BitsRequired(day) * 50) / 8);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestSingleBigValueCompression()
        {
            Directory dir = new RAMDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            IndexWriter iwriter = new IndexWriter(dir, iwc);

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