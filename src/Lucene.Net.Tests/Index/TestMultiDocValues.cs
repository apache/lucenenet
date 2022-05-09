using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IBits = Lucene.Net.Util.IBits;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using SortedDocValuesField = SortedDocValuesField;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests MultiDocValues versus ordinary segment merging </summary>
    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestMultiDocValues : LuceneTestCase
    {
        [Test]
        public virtual void TestNumerics()
        {
            Directory dir = NewDirectory();
            Document doc = new Document();
            Field field = new NumericDocValuesField("numbers", 0);
            doc.Add(field);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                field.SetInt64Value(Random.NextInt64());
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            NumericDocValues multi = MultiDocValues.GetNumericValues(ir, "numbers");
            NumericDocValues single = merged.GetNumericDocValues("numbers");
            for (int i = 0; i < numDocs; i++)
            {
                Assert.AreEqual(single.Get(i), multi.Get(i));
            }
            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBinary()
        {
            Directory dir = NewDirectory();
            Document doc = new Document();
            BytesRef @ref = new BytesRef();
            Field field = new BinaryDocValuesField("bytes", @ref);
            doc.Add(field);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                @ref.CopyChars(TestUtil.RandomUnicodeString(Random));
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            BinaryDocValues multi = MultiDocValues.GetBinaryValues(ir, "bytes");
            BinaryDocValues single = merged.GetBinaryDocValues("bytes");
            BytesRef actual = new BytesRef();
            BytesRef expected = new BytesRef();
            for (int i = 0; i < numDocs; i++)
            {
                single.Get(i, expected);
                multi.Get(i, actual);
                Assert.AreEqual(expected, actual);
            }
            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSorted()
        {
            Directory dir = NewDirectory();
            Document doc = new Document();
            BytesRef @ref = new BytesRef();
            Field field = new SortedDocValuesField("bytes", @ref);
            doc.Add(field);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                @ref.CopyChars(TestUtil.RandomUnicodeString(Random));
                if (DefaultCodecSupportsDocsWithField && Random.Next(7) == 0)
                {
                    iw.AddDocument(new Document());
                }
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
            SortedDocValues single = merged.GetSortedDocValues("bytes");
            Assert.AreEqual(single.ValueCount, multi.ValueCount);
            BytesRef actual = new BytesRef();
            BytesRef expected = new BytesRef();
            for (int i = 0; i < numDocs; i++)
            {
                // check ord
                Assert.AreEqual(single.GetOrd(i), multi.GetOrd(i));
                // check value
                single.Get(i, expected);
                multi.Get(i, actual);
                Assert.AreEqual(expected, actual);
            }
            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        // tries to make more dups than testSorted
        [Test]
        public virtual void TestSortedWithLotsOfDups()
        {
            Directory dir = NewDirectory();
            Document doc = new Document();
            BytesRef @ref = new BytesRef();
            Field field = new SortedDocValuesField("bytes", @ref);
            doc.Add(field);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                @ref.CopyChars(TestUtil.RandomSimpleString(Random, 2));
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            SortedDocValues multi = MultiDocValues.GetSortedValues(ir, "bytes");
            SortedDocValues single = merged.GetSortedDocValues("bytes");
            Assert.AreEqual(single.ValueCount, multi.ValueCount);
            BytesRef actual = new BytesRef();
            BytesRef expected = new BytesRef();
            for (int i = 0; i < numDocs; i++)
            {
                // check ord
                Assert.AreEqual(single.GetOrd(i), multi.GetOrd(i));
                // check ord value
                single.Get(i, expected);
                multi.Get(i, actual);
                Assert.AreEqual(expected, actual);
            }
            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSortedSet()
        {
            AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            Directory dir = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int numValues = Random.Next(5);
                for (int j = 0; j < numValues; j++)
                {
                    doc.Add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.RandomUnicodeString(Random))));
                }
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
            SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
            if (multi is null)
            {
                Assert.IsNull(single);
            }
            else
            {
                Assert.AreEqual(single.ValueCount, multi.ValueCount);
                BytesRef actual = new BytesRef();
                BytesRef expected = new BytesRef();
                // check values
                for (long i = 0; i < single.ValueCount; i++)
                {
                    single.LookupOrd(i, expected);
                    multi.LookupOrd(i, actual);
                    Assert.AreEqual(expected, actual);
                }
                // check ord list
                for (int i = 0; i < numDocs; i++)
                {
                    single.SetDocument(i);
                    IList<long> expectedList = new JCG.List<long>();
                    long ord;
                    while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        expectedList.Add(ord);
                    }

                    multi.SetDocument(i);
                    int upto = 0;
                    while ((ord = multi.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        Assert.AreEqual(expectedList[upto], ord);
                        upto++;
                    }
                    Assert.AreEqual(expectedList.Count, upto);
                }
            }

            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        // tries to make more dups than testSortedSet
        [Test]
        public virtual void TestSortedSetWithDups()
        {
            AssumeTrue("codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            Directory dir = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                int numValues = Random.Next(5);
                for (int j = 0; j < numValues; j++)
                {
                    doc.Add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.RandomSimpleString(Random, 2))));
                }
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            SortedSetDocValues multi = MultiDocValues.GetSortedSetValues(ir, "bytes");
            SortedSetDocValues single = merged.GetSortedSetDocValues("bytes");
            if (multi is null)
            {
                Assert.IsNull(single);
            }
            else
            {
                Assert.AreEqual(single.ValueCount, multi.ValueCount);
                BytesRef actual = new BytesRef();
                BytesRef expected = new BytesRef();
                // check values
                for (long i = 0; i < single.ValueCount; i++)
                {
                    single.LookupOrd(i, expected);
                    multi.LookupOrd(i, actual);
                    Assert.AreEqual(expected, actual);
                }
                // check ord list
                for (int i = 0; i < numDocs; i++)
                {
                    single.SetDocument(i);
                    IList<long> expectedList = new JCG.List<long>();
                    long ord;
                    while ((ord = single.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        expectedList.Add(ord);
                    }

                    multi.SetDocument(i);
                    int upto = 0;
                    while ((ord = multi.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        Assert.AreEqual(expectedList[upto], ord);
                        upto++;
                    }
                    Assert.AreEqual(expectedList.Count, upto);
                }
            }

            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocsWithField()
        {
            AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField);
            Directory dir = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                if (Random.Next(4) >= 0)
                {
                    doc.Add(new NumericDocValuesField("numbers", Random.NextInt64()));
                }
                doc.Add(new NumericDocValuesField("numbersAlways", Random.NextInt64()));
                iw.AddDocument(doc);
                if (Random.Next(17) == 0)
                {
                    iw.Commit();
                }
            }
            DirectoryReader ir = iw.GetReader();
            iw.ForceMerge(1);
            DirectoryReader ir2 = iw.GetReader();
            AtomicReader merged = GetOnlySegmentReader(ir2);
            iw.Dispose();

            IBits multi = MultiDocValues.GetDocsWithField(ir, "numbers");
            IBits single = merged.GetDocsWithField("numbers");
            if (multi is null)
            {
                Assert.IsNull(single);
            }
            else
            {
                Assert.AreEqual(single.Length, multi.Length);
                for (int i = 0; i < numDocs; i++)
                {
                    Assert.AreEqual(single.Get(i), multi.Get(i));
                }
            }

            multi = MultiDocValues.GetDocsWithField(ir, "numbersAlways");
            single = merged.GetDocsWithField("numbersAlways");
            Assert.AreEqual(single.Length, multi.Length);
            for (int i = 0; i < numDocs; i++)
            {
                Assert.AreEqual(single.Get(i), multi.Get(i));
            }
            ir.Dispose();
            ir2.Dispose();
            dir.Dispose();
        }
    }
}