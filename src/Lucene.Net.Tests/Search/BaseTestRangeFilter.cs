using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class BaseTestRangeFilter : LuceneTestCase
    {
        public const bool F = false;
        public const bool T = true;

        /// <summary>
        /// Collation interacts badly with hyphens -- collation produces different
        /// ordering than Unicode code-point ordering -- so two indexes are created:
        /// one which can't have negative random integers, for testing collated ranges,
        /// and the other which can have negative random integers, for all other tests.
        /// </summary>
        internal class TestIndex
        {
            internal int maxR;
            internal int minR;
            internal bool allowNegativeRandomInts;
            internal Directory index;

            internal TestIndex(Random random, int minR, int maxR, bool allowNegativeRandomInts)
            {
                this.minR = minR;
                this.maxR = maxR;
                this.allowNegativeRandomInts = allowNegativeRandomInts;
                index = NewDirectory(random);
            }
        }

        internal static IndexReader signedIndexReader;
        internal static IndexReader unsignedIndexReader;

        internal static TestIndex signedIndexDir;
        internal static TestIndex unsignedIndexDir;

        internal static int minId = 0;
        internal static int maxId;

        internal static readonly int intLength = Convert.ToString(int.MaxValue).Length;

        /// <summary>
        /// a simple padding function that should work with any int
        /// </summary>
        public static string Pad(int n)
        {
            StringBuilder b = new StringBuilder(40);
            string p = "0";
            if (n < 0)
            {
                p = "-";
                n = int.MaxValue + n + 1;
            }
            b.Append(p);
            string s = Convert.ToString(n);
            for (int i = s.Length; i <= intLength; i++)
            {
                b.Append('0');
            }
            b.Append(s);

            return b.ToString();
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because <see cref="Build(Random, TestIndex)"/> is no
        /// longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific: renamed from BeforeClassBaseTestRangeFilter() so we can override to control the order of execution
        {
            base.BeforeClass();

            maxId = AtLeast(500);
            signedIndexDir = new TestIndex(Random, int.MaxValue, int.MinValue, true);
            unsignedIndexDir = new TestIndex(Random, int.MaxValue, 0, false);
            signedIndexReader = Build(Random, signedIndexDir);
            unsignedIndexReader = Build(Random, unsignedIndexDir);
        }

        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific: renamed from AfterClassBaseTestRangeFilter() so we can override to control the order of execution
        {
            signedIndexReader?.Dispose();
            unsignedIndexReader?.Dispose();
            signedIndexDir?.index?.Dispose();
            unsignedIndexDir?.index?.Dispose();
            signedIndexReader = null;
            unsignedIndexReader = null;
            signedIndexDir = null;
            unsignedIndexDir = null;
            base.AfterClass();
        }

        /// <summary>
        /// LUCENENET specific
        /// Passed in because NewStringField and NewIndexWriterConfig are no
        /// longer static.
        /// </summary>
        private IndexReader Build(Random random, TestIndex index)
        {
            /* build an index */

            Document doc = new Document();
            Field idField = NewStringField(random, "id", "", Field.Store.YES);
            Field randField = NewStringField(random, "rand", "", Field.Store.YES);
            Field bodyField = NewStringField(random, "body", "", Field.Store.NO);
            doc.Add(idField);
            doc.Add(randField);
            doc.Add(bodyField);

            RandomIndexWriter writer = new RandomIndexWriter(random, index.index, NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(TestUtil.NextInt32(random, 50, 1000)).SetMergePolicy(NewLogMergePolicy()));
            TestUtil.ReduceOpenFiles(writer.IndexWriter);

            while (true)
            {
                int minCount = 0;
                int maxCount = 0;

                for (int d = minId; d <= maxId; d++)
                {
                    idField.SetStringValue(Pad(d));
                    int r = index.allowNegativeRandomInts ? random.Next() : random.Next(int.MaxValue);
                    if (index.maxR < r)
                    {
                        index.maxR = r;
                        maxCount = 1;
                    }
                    else if (index.maxR == r)
                    {
                        maxCount++;
                    }

                    if (r < index.minR)
                    {
                        index.minR = r;
                        minCount = 1;
                    }
                    else if (r == index.minR)
                    {
                        minCount++;
                    }
                    randField.SetStringValue(Pad(r));
                    bodyField.SetStringValue("body");
                    writer.AddDocument(doc);
                }

                if (minCount == 1 && maxCount == 1)
                {
                    // our subclasses rely on only 1 doc having the min or
                    // max, so, we loop until we satisfy that.  it should be
                    // exceedingly rare (Yonik calculates 1 in ~429,000)
                    // times) that this loop requires more than one try:
                    IndexReader ir = writer.GetReader();
                    writer.Dispose();
                    return ir;
                }

                // try again
                writer.DeleteAll();
            }
        }

        [Test]
        public virtual void TestPad()
        {
            int[] tests = new int[] { -9999999, -99560, -100, -3, -1, 0, 3, 9, 10, 1000, 999999999 };
            for (int i = 0; i < tests.Length - 1; i++)
            {
                int a = tests[i];
                int b = tests[i + 1];
                string aa = Pad(a);
                string bb = Pad(b);
                string label = a + ":" + aa + " vs " + b + ":" + bb;
                Assert.AreEqual(aa.Length, bb.Length, "i=" + i + ": length of " + label);
                Assert.IsTrue(System.String.Compare(aa, bb, System.StringComparison.Ordinal) < 0, "i=" + i + ": compare less than " + label);
            }
        }
    }
}