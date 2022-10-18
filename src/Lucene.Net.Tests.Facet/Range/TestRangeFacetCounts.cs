// Lucene version compatibility level 4.8.1
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Facet.Range
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using CachingWrapperFilter = Lucene.Net.Search.CachingWrapperFilter;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Lucene.Net.Documents.Document;
    using DoubleDocValues = Lucene.Net.Queries.Function.DocValues.DoubleDocValues;
    using DoubleDocValuesField = Lucene.Net.Documents.DoubleDocValuesField;
    using DoubleField = Lucene.Net.Documents.DoubleField;
    using DoubleFieldSource = Lucene.Net.Queries.Function.ValueSources.DoubleFieldSource;
    using DrillSidewaysResult = Lucene.Net.Facet.DrillSidewaysResult;
    using Field = Lucene.Net.Documents.Field;
    using Filter = Lucene.Net.Search.Filter;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using Int64Field = Lucene.Net.Documents.Int64Field;
    using Int64FieldSource = Lucene.Net.Queries.Function.ValueSources.Int64FieldSource;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using NumericDocValuesField = Lucene.Net.Documents.NumericDocValuesField;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using QueryWrapperFilter = Lucene.Net.Search.QueryWrapperFilter;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SingleDocValuesField = Lucene.Net.Documents.SingleDocValuesField;
    using SingleField = Lucene.Net.Documents.SingleField;
    using SingleFieldSource = Lucene.Net.Queries.Function.ValueSources.SingleFieldSource;
    using TaxonomyReader = Lucene.Net.Facet.Taxonomy.TaxonomyReader;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    [TestFixture]
    public class TestRangeFacetCounts : FacetTestCase
    {

        [Test]
        public virtual void TestBasicLong()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            NumericDocValuesField field = new NumericDocValuesField("field", 0L);
            doc.Add(field);
            for (long l = 0; l < 100; l++)
            {
                field.SetInt64Value(l);
                w.AddDocument(doc);
            }

            // Also add Long.MAX_VALUE
            field.SetInt64Value(long.MaxValue);
            w.AddDocument(doc);

            IndexReader r = w.GetReader();
            w.Dispose();

            FacetsCollector fc = new FacetsCollector();
            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new Int64RangeFacetCounts("field", fc,
                new Int64Range("less than 10", 0L, true, 10L, false),
                new Int64Range("less than or equal to 10", 0L, true, 10L, true),
                new Int64Range("over 90", 90L, false, 100L, false),
                new Int64Range("90 or above", 90L, true, 100L, false),
                new Int64Range("over 1000", 1000L, false, long.MaxValue, true));

            FacetResult result = facets.GetTopChildren(10, "field");

            Assert.AreEqual("dim=field path=[] value=22 childCount=5\n  less than 10 (10)\n  less than or equal to 10 (11)\n  over 90 (9)\n  90 or above (10)\n  over 1000 (1)\n", result.ToString());

            r.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestUselessRange()
        {
            try
            {
                _ = new Int64Range("useless", 7, true, 6, true);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new Int64Range("useless", 7, true, 7, false);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new DoubleRange("useless", 7.0, true, 6.0, true);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new DoubleRange("useless", 7.0, true, 7.0, false);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestLongMinMax()
        {

            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            NumericDocValuesField field = new NumericDocValuesField("field", 0L);
            doc.Add(field);
            field.SetInt64Value(long.MinValue);
            w.AddDocument(doc);
            field.SetInt64Value(0);
            w.AddDocument(doc);
            field.SetInt64Value(long.MaxValue);
            w.AddDocument(doc);

            IndexReader r = w.GetReader();
            w.Dispose();

            FacetsCollector fc = new FacetsCollector();
            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new Int64RangeFacetCounts("field", fc,
                new Int64Range("min", long.MinValue, true, long.MinValue, true),
                new Int64Range("max", long.MaxValue, true, long.MaxValue, true),
                new Int64Range("all0", long.MinValue, true, long.MaxValue, true),
                new Int64Range("all1", long.MinValue, false, long.MaxValue, true),
                new Int64Range("all2", long.MinValue, true, long.MaxValue, false),
                new Int64Range("all3", long.MinValue, false, long.MaxValue, false));

            FacetResult result = facets.GetTopChildren(10, "field");
            Assert.AreEqual("dim=field path=[] value=3 childCount=6\n  min (1)\n  max (1)\n  all0 (3)\n  all1 (2)\n  all2 (2)\n  all3 (1)\n", result.ToString());

            r.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestOverlappedEndStart()
        {
            Directory d = NewDirectory();
            var w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            NumericDocValuesField field = new NumericDocValuesField("field", 0L);
            doc.Add(field);
            for (long l = 0; l < 100; l++)
            {
                field.SetInt64Value(l);
                w.AddDocument(doc);
            }
            field.SetInt64Value(long.MaxValue);
            w.AddDocument(doc);

            IndexReader r = w.GetReader();
            w.Dispose();

            FacetsCollector fc = new FacetsCollector();
            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new Int64RangeFacetCounts("field", fc,
                new Int64Range("0-10", 0L, true, 10L, true),
                new Int64Range("10-20", 10L, true, 20L, true),
                new Int64Range("20-30", 20L, true, 30L, true),
                new Int64Range("30-40", 30L, true, 40L, true));

            FacetResult result = facets.GetTopChildren(10, "field");
            Assert.AreEqual("dim=field path=[] value=41 childCount=4\n  0-10 (11)\n  10-20 (11)\n  20-30 (11)\n  30-40 (11)\n", result.ToString());

            r.Dispose();
            d.Dispose();
        }

        /// <summary>
        /// Tests single request that mixes Range and non-Range
        ///  faceting, with <see cref="DrillSideways"/> and taxonomy. 
        /// </summary>
        [Test]
        public virtual void TestMixedRangeAndNonRangeTaxonomy()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Directory td = NewDirectory();
            DirectoryTaxonomyWriter tw = new DirectoryTaxonomyWriter(td, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();

            for (long l = 0; l < 100; l++)
            {
                Document doc = new Document();
                // For computing range facet counts:
                doc.Add(new NumericDocValuesField("field", l));
                // For drill down by numeric range:
                doc.Add(new Int64Field("field", l, Field.Store.NO));

                if ((l & 3) == 0)
                {
                    doc.Add(new FacetField("dim", "a"));
                }
                else
                {
                    doc.Add(new FacetField("dim", "b"));
                }
                w.AddDocument(config.Build(tw, doc));
            }

            IndexReader r = w.GetReader();

            var tr = new DirectoryTaxonomyReader(tw);

            IndexSearcher s = NewSearcher(r);

            if (Verbose)
            {
                Console.WriteLine("TEST: searcher=" + s);
            }

            DrillSideways ds = new DrillSidewaysAnonymousClass(this, s, config, tr);

            // First search, no drill downs:
            DrillDownQuery ddq = new DrillDownQuery(config);
            DrillSidewaysResult dsr = ds.Search(null, ddq, 10);

            Assert.AreEqual(100, dsr.Hits.TotalHits);
            Assert.AreEqual("dim=dim path=[] value=100 childCount=2\n  b (75)\n  a (25)\n", dsr.Facets.GetTopChildren(10, "dim").ToString());
            Assert.AreEqual("dim=field path=[] value=21 childCount=5\n  less than 10 (10)\n  less than or equal to 10 (11)\n  over 90 (9)\n  90 or above (10)\n  over 1000 (0)\n",
                dsr.Facets.GetTopChildren(10, "field").ToString());

            // Second search, drill down on dim=b:
            ddq = new DrillDownQuery(config);
            ddq.Add("dim", "b");
            dsr = ds.Search(null, ddq, 10);

            Assert.AreEqual(75, dsr.Hits.TotalHits);
            Assert.AreEqual("dim=dim path=[] value=100 childCount=2\n  b (75)\n  a (25)\n", dsr.Facets.GetTopChildren(10, "dim").ToString());
            Assert.AreEqual("dim=field path=[] value=16 childCount=5\n  less than 10 (7)\n  less than or equal to 10 (8)\n  over 90 (7)\n  90 or above (8)\n  over 1000 (0)\n",
                dsr.Facets.GetTopChildren(10, "field").ToString());

            // Third search, drill down on "less than or equal to 10":
            ddq = new DrillDownQuery(config);
            ddq.Add("field", NumericRangeQuery.NewInt64Range("field", 0L, 10L, true, true));
            dsr = ds.Search(null, ddq, 10);

            Assert.AreEqual(11, dsr.Hits.TotalHits);
            Assert.AreEqual("dim=dim path=[] value=11 childCount=2\n  b (8)\n  a (3)\n", dsr.Facets.GetTopChildren(10, "dim").ToString());
            Assert.AreEqual("dim=field path=[] value=21 childCount=5\n  less than 10 (10)\n  less than or equal to 10 (11)\n  over 90 (9)\n  90 or above (10)\n  over 1000 (0)\n",
                dsr.Facets.GetTopChildren(10, "field").ToString());
            IOUtils.Dispose(tw, tr, td, w, r, d);
        }

        private sealed class DrillSidewaysAnonymousClass : DrillSideways
        {
            private readonly TestRangeFacetCounts outerInstance;

            public DrillSidewaysAnonymousClass(TestRangeFacetCounts outerInstance, IndexSearcher s, FacetsConfig config, TaxonomyReader tr)
                : base(s, config, tr)
            {
                this.outerInstance = outerInstance;
            }

            protected override Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
            {
                FacetsCollector dimFC = drillDowns;
                FacetsCollector fieldFC = drillDowns;
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        string dim = drillSidewaysDims[i];
                        if (dim.Equals("field", StringComparison.Ordinal))
                        {
                            fieldFC = drillSideways[i];
                        }
                        else
                        {
                            dimFC = drillSideways[i];
                        }
                    }
                }

                IDictionary<string, Facets> byDim = new Dictionary<string, Facets>();
                byDim["field"] = new Int64RangeFacetCounts("field", fieldFC,
                    new Int64Range("less than 10", 0L, true, 10L, false),
                    new Int64Range("less than or equal to 10", 0L, true, 10L, true),
                    new Int64Range("over 90", 90L, false, 100L, false),
                    new Int64Range("90 or above", 90L, true, 100L, false),
                    new Int64Range("over 1000", 1000L, false, long.MaxValue, false));
                byDim["dim"] = outerInstance.GetTaxonomyFacetCounts(m_taxoReader, m_config, dimFC);
                return new MultiFacets(byDim, null);
            }

            protected override bool ScoreSubDocsAtOnce => Random.NextBoolean();
        }

        [Test]
        public virtual void TestBasicDouble()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            DoubleDocValuesField field = new DoubleDocValuesField("field", 0.0);
            doc.Add(field);
            for (long l = 0; l < 100; l++)
            {
                field.SetDoubleValue(l);
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();

            FacetsCollector fc = new FacetsCollector();

            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);
            Facets facets = new DoubleRangeFacetCounts("field", fc,
                new DoubleRange("less than 10", 0.0, true, 10.0, false),
                new DoubleRange("less than or equal to 10", 0.0, true, 10.0, true),
                new DoubleRange("over 90", 90.0, false, 100.0, false),
                new DoubleRange("90 or above", 90.0, true, 100.0, false),
                new DoubleRange("over 1000", 1000.0, false, double.PositiveInfinity, false));

            Assert.AreEqual("dim=field path=[] value=21 childCount=5\n  less than 10 (10)\n  less than or equal to 10 (11)\n  over 90 (9)\n  90 or above (10)\n  over 1000 (0)\n",
                facets.GetTopChildren(10, "field").ToString());

            IOUtils.Dispose(w, r, d);
        }

        [Test]
        public virtual void TestBasicFloat()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            SingleDocValuesField field = new SingleDocValuesField("field", 0.0f);
            doc.Add(field);
            for (long l = 0; l < 100; l++)
            {
                field.SetSingleValue(l);
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();

            FacetsCollector fc = new FacetsCollector();

            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);

            Facets facets = new DoubleRangeFacetCounts("field", new SingleFieldSource("field"), fc,
                new DoubleRange("less than 10", 0.0f, true, 10.0f, false),
                new DoubleRange("less than or equal to 10", 0.0f, true, 10.0f, true),
                new DoubleRange("over 90", 90.0f, false, 100.0f, false),
                new DoubleRange("90 or above", 90.0f, true, 100.0f, false),
                new DoubleRange("over 1000", 1000.0f, false, double.PositiveInfinity, false));

            Assert.AreEqual("dim=field path=[] value=21 childCount=5\n  less than 10 (10)\n  less than or equal to 10 (11)\n  over 90 (9)\n  90 or above (10)\n  over 1000 (0)\n",
                facets.GetTopChildren(10, "field").ToString());

            IOUtils.Dispose(w, r, d);
        }

        [Test]
        public virtual void TestRandomLongs()
        {
            Directory dir = NewDirectory();
            var w = new RandomIndexWriter(Random, dir);

            int numDocs = AtLeast(1000);
            if (Verbose)
            {
                Console.WriteLine("TEST: numDocs=" + numDocs);
            }
            long[] values = new long[numDocs];
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                long v = Random.NextInt64();
                values[i] = v;
                doc.Add(new NumericDocValuesField("field", v));
                doc.Add(new Int64Field("field", v, Field.Store.NO));
                w.AddDocument(doc);
                minValue = Math.Min(minValue, v);
                maxValue = Math.Max(maxValue, v);
            }
            IndexReader r = w.GetReader();

            IndexSearcher s = NewSearcher(r);
            FacetsConfig config = new FacetsConfig();

            int numIters = AtLeast(10);
            for (int iter = 0; iter < numIters; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                int numRange = TestUtil.NextInt32(Random, 1, 100);
                Int64Range[] ranges = new Int64Range[numRange];
                int[] expectedCounts = new int[numRange];
                long minAcceptedValue = long.MaxValue;
                long maxAcceptedValue = long.MinValue;
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    long min;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        Int64Range prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            min = prevRange.Min;
                        }
                        else
                        {
                            min = prevRange.Max;
                        }
                    }
                    else
                    {
                        min = Random.NextInt64();
                    }
                    long max;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        Int64Range prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            max = prevRange.Min;
                        }
                        else
                        {
                            max = prevRange.Max;
                        }
                    }
                    else
                    {
                        max = Random.NextInt64();
                    }

                    if (min > max)
                    {
                        long x = min;
                        min = max;
                        max = x;
                    }
                    bool minIncl;
                    bool maxIncl;
                    if (min == max)
                    {
                        minIncl = true;
                        maxIncl = true;
                    }
                    else
                    {
                        minIncl = Random.NextBoolean();
                        maxIncl = Random.NextBoolean();
                    }
                    ranges[rangeID] = new Int64Range("r" + rangeID, min, minIncl, max, maxIncl);
                    if (Verbose)
                    {
                        Console.WriteLine("  range " + rangeID + ": " + ranges[rangeID]);
                    }

                    // Do "slow but hopefully correct" computation of
                    // expected count:
                    for (int i = 0; i < numDocs; i++)
                    {
                        bool accept = true;
                        if (minIncl)
                        {
                            accept &= values[i] >= min;
                        }
                        else
                        {
                            accept &= values[i] > min;
                        }
                        if (maxIncl)
                        {
                            accept &= values[i] <= max;
                        }
                        else
                        {
                            accept &= values[i] < max;
                        }
                        if (accept)
                        {
                            expectedCounts[rangeID]++;
                            minAcceptedValue = Math.Min(minAcceptedValue, values[i]);
                            maxAcceptedValue = Math.Max(maxAcceptedValue, values[i]);
                        }
                    }
                }

                FacetsCollector sfc = new FacetsCollector();
                s.Search(new MatchAllDocsQuery(), sfc);
                Filter fastMatchFilter;
                if (Random.NextBoolean())
                {
                    if (Random.NextBoolean())
                    {
                        fastMatchFilter = NumericRangeFilter.NewInt64Range("field", minValue, maxValue, true, true);
                    }
                    else
                    {
                        fastMatchFilter = NumericRangeFilter.NewInt64Range("field", minAcceptedValue, maxAcceptedValue, true, true);
                    }
                }
                else
                {
                    fastMatchFilter = null;
                }
                ValueSource vs = new Int64FieldSource("field");
                Facets facets = new Int64RangeFacetCounts("field", vs, sfc, fastMatchFilter, ranges);
                FacetResult result = facets.GetTopChildren(10, "field");
                Assert.AreEqual(numRange, result.LabelValues.Length);
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  range " + rangeID + " expectedCount=" + expectedCounts[rangeID]);
                    }
                    LabelAndValue subNode = result.LabelValues[rangeID];
                    Assert.AreEqual("r" + rangeID, subNode.Label);
                    Assert.AreEqual(expectedCounts[rangeID], (int)subNode.Value);

                    Int64Range range = ranges[rangeID];

                    // Test drill-down:
                    DrillDownQuery ddq = new DrillDownQuery(config);
                    if (Random.NextBoolean())
                    {
                        if (Random.NextBoolean())
                        {
                            ddq.Add("field", NumericRangeFilter.NewInt64Range("field", range.Min, range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                        else
                        {
                            ddq.Add("field", NumericRangeQuery.NewInt64Range("field", range.Min, range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                    }
                    else
                    {
                        ddq.Add("field", range.GetFilter(fastMatchFilter, vs));
                    }
                    Assert.AreEqual(expectedCounts[rangeID], s.Search(ddq, 10).TotalHits);
                }
            }

            IOUtils.Dispose(w, r, dir);
        }

        [Test]
        public virtual void TestRandomFloats()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            int numDocs = AtLeast(1000);
            float[] values = new float[numDocs];
            float minValue = float.PositiveInfinity;
            float maxValue = float.NegativeInfinity;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                float v = Random.NextSingle();
                values[i] = v;
                doc.Add(new SingleDocValuesField("field", v));
                doc.Add(new SingleField("field", v, Field.Store.NO));
                w.AddDocument(doc);
                minValue = Math.Min(minValue, v);
                maxValue = Math.Max(maxValue, v);
            }
            IndexReader r = w.GetReader();

            IndexSearcher s = NewSearcher(r);
            FacetsConfig config = new FacetsConfig();

            int numIters = AtLeast(10);
            for (int iter = 0; iter < numIters; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                int numRange = TestUtil.NextInt32(Random, 1, 5);
                DoubleRange[] ranges = new DoubleRange[numRange];
                int[] expectedCounts = new int[numRange];
                float minAcceptedValue = float.PositiveInfinity;
                float maxAcceptedValue = float.NegativeInfinity;
                if (Verbose)
                {
                    Console.WriteLine("TEST: " + numRange + " ranges");
                }
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    double min;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        DoubleRange prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            min = prevRange.Min;
                        }
                        else
                        {
                            min = prevRange.Max;
                        }
                    }
                    else
                    {
                        min = Random.NextDouble();
                    }
                    double max;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        DoubleRange prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            max = prevRange.Min;
                        }
                        else
                        {
                            max = prevRange.Max;
                        }
                    }
                    else
                    {
                        max = Random.NextDouble();
                    }

                    if (min > max)
                    {
                        double x = min;
                        min = max;
                        max = x;
                    }

                    // Must truncate to float precision so that the
                    // drill-down counts (which use NRQ.newFloatRange)
                    // are correct:
                    min = (float)min;
                    max = (float)max;

                    bool minIncl;
                    bool maxIncl;
                    if (min == max)
                    {
                        minIncl = true;
                        maxIncl = true;
                    }
                    else
                    {
                        minIncl = Random.NextBoolean();
                        maxIncl = Random.NextBoolean();
                    }
                    ranges[rangeID] = new DoubleRange("r" + rangeID, min, minIncl, max, maxIncl);

                    if (Verbose)
                    {
                        Console.WriteLine("TEST:   range " + rangeID + ": " + ranges[rangeID]);
                    }

                    // Do "slow but hopefully correct" computation of
                    // expected count:
                    for (int i = 0; i < numDocs; i++)
                    {
                        bool accept = true;
                        if (minIncl)
                        {
                            accept &= values[i] >= min;
                        }
                        else
                        {
                            accept &= values[i] > min;
                        }
                        if (maxIncl)
                        {
                            accept &= values[i] <= max;
                        }
                        else
                        {
                            accept &= values[i] < max;
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("TEST:   check doc=" + i + " val=" + values[i] + " accept=" + accept);
                        }
                        if (accept)
                        {
                            expectedCounts[rangeID]++;
                            minAcceptedValue = Math.Min(minAcceptedValue, values[i]);
                            maxAcceptedValue = Math.Max(maxAcceptedValue, values[i]);
                        }
                    }
                }

                FacetsCollector sfc = new FacetsCollector();
                s.Search(new MatchAllDocsQuery(), sfc);
                Filter fastMatchFilter;
                if (Random.NextBoolean())
                {
                    if (Random.NextBoolean())
                    {
                        fastMatchFilter = NumericRangeFilter.NewSingleRange("field", minValue, maxValue, true, true);
                    }
                    else
                    {
                        fastMatchFilter = NumericRangeFilter.NewSingleRange("field", minAcceptedValue, maxAcceptedValue, true, true);
                    }
                }
                else
                {
                    fastMatchFilter = null;
                }
                ValueSource vs = new SingleFieldSource("field");
                Facets facets = new DoubleRangeFacetCounts("field", vs, sfc, fastMatchFilter, ranges);
                FacetResult result = facets.GetTopChildren(10, "field");
                Assert.AreEqual(numRange, result.LabelValues.Length);
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: verify range " + rangeID + " expectedCount=" + expectedCounts[rangeID]);
                    }
                    LabelAndValue subNode = result.LabelValues[rangeID];
                    Assert.AreEqual("r" + rangeID, subNode.Label);
                    Assert.AreEqual(expectedCounts[rangeID], (int)subNode.Value);

                    DoubleRange range = ranges[rangeID];

                    // Test drill-down:
                    DrillDownQuery ddq = new DrillDownQuery(config);
                    if (Random.NextBoolean())
                    {
                        if (Random.NextBoolean())
                        {
                            ddq.Add("field", NumericRangeFilter.NewSingleRange("field", (float)range.Min, (float)range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                        else
                        {
                            ddq.Add("field", NumericRangeQuery.NewSingleRange("field", (float)range.Min, (float)range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                    }
                    else
                    {
                        ddq.Add("field", range.GetFilter(fastMatchFilter, vs));
                    }
                    Assert.AreEqual(expectedCounts[rangeID], s.Search(ddq, 10).TotalHits);
                }
            }

            IOUtils.Dispose(w, r, dir);
        }

        [Test]
        public virtual void TestRandomDoubles()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            int numDocs = AtLeast(1000);
            double[] values = new double[numDocs];
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                double v = Random.NextDouble();
                values[i] = v;
                doc.Add(new DoubleDocValuesField("field", v));
                doc.Add(new DoubleField("field", v, Field.Store.NO));
                w.AddDocument(doc);
                minValue = Math.Min(minValue, v);
                maxValue = Math.Max(maxValue, v);
            }
            IndexReader r = w.GetReader();

            IndexSearcher s = NewSearcher(r);
            FacetsConfig config = new FacetsConfig();

            int numIters = AtLeast(10);
            for (int iter = 0; iter < numIters; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }
                int numRange = TestUtil.NextInt32(Random, 1, 5);
                DoubleRange[] ranges = new DoubleRange[numRange];
                int[] expectedCounts = new int[numRange];
                double minAcceptedValue = double.PositiveInfinity;
                double maxAcceptedValue = double.NegativeInfinity;
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    double min;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        DoubleRange prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            min = prevRange.Min;
                        }
                        else
                        {
                            min = prevRange.Max;
                        }
                    }
                    else
                    {
                        min = Random.NextDouble();
                    }
                    double max;
                    if (rangeID > 0 && Random.Next(10) == 7)
                    {
                        // Use an existing boundary:
                        DoubleRange prevRange = ranges[Random.Next(rangeID)];
                        if (Random.NextBoolean())
                        {
                            max = prevRange.Min;
                        }
                        else
                        {
                            max = prevRange.Max;
                        }
                    }
                    else
                    {
                        max = Random.NextDouble();
                    }

                    if (min > max)
                    {
                        double x = min;
                        min = max;
                        max = x;
                    }

                    bool minIncl;
                    bool maxIncl;
                    if (min == max)
                    {
                        minIncl = true;
                        maxIncl = true;
                    }
                    else
                    {
                        minIncl = Random.NextBoolean();
                        maxIncl = Random.NextBoolean();
                    }
                    ranges[rangeID] = new DoubleRange("r" + rangeID, min, minIncl, max, maxIncl);

                    // Do "slow but hopefully correct" computation of
                    // expected count:
                    for (int i = 0; i < numDocs; i++)
                    {
                        bool accept = true;
                        if (minIncl)
                        {
                            accept &= values[i] >= min;
                        }
                        else
                        {
                            accept &= values[i] > min;
                        }
                        if (maxIncl)
                        {
                            accept &= values[i] <= max;
                        }
                        else
                        {
                            accept &= values[i] < max;
                        }
                        if (accept)
                        {
                            expectedCounts[rangeID]++;
                            minAcceptedValue = Math.Min(minAcceptedValue, values[i]);
                            maxAcceptedValue = Math.Max(maxAcceptedValue, values[i]);
                        }
                    }
                }

                FacetsCollector sfc = new FacetsCollector();
                s.Search(new MatchAllDocsQuery(), sfc);
                Filter fastMatchFilter;
                if (Random.NextBoolean())
                {
                    if (Random.NextBoolean())
                    {
                        fastMatchFilter = NumericRangeFilter.NewDoubleRange("field", minValue, maxValue, true, true);
                    }
                    else
                    {
                        fastMatchFilter = NumericRangeFilter.NewDoubleRange("field", minAcceptedValue, maxAcceptedValue, true, true);
                    }
                }
                else
                {
                    fastMatchFilter = null;
                }
                ValueSource vs = new DoubleFieldSource("field");
                Facets facets = new DoubleRangeFacetCounts("field", vs, sfc, fastMatchFilter, ranges);
                FacetResult result = facets.GetTopChildren(10, "field");
                Assert.AreEqual(numRange, result.LabelValues.Length);
                for (int rangeID = 0; rangeID < numRange; rangeID++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  range " + rangeID + " expectedCount=" + expectedCounts[rangeID]);
                    }
                    LabelAndValue subNode = result.LabelValues[rangeID];
                    Assert.AreEqual("r" + rangeID, subNode.Label);
                    Assert.AreEqual(expectedCounts[rangeID], (int)subNode.Value);

                    DoubleRange range = ranges[rangeID];

                    // Test drill-down:
                    DrillDownQuery ddq = new DrillDownQuery(config);
                    if (Random.NextBoolean())
                    {
                        if (Random.NextBoolean())
                        {
                            ddq.Add("field", NumericRangeFilter.NewDoubleRange("field", range.Min, range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                        else
                        {
                            ddq.Add("field", NumericRangeQuery.NewDoubleRange("field", range.Min, range.Max, range.MinInclusive, range.MaxInclusive));
                        }
                    }
                    else
                    {
                        ddq.Add("field", range.GetFilter(fastMatchFilter, vs));
                    }

                    Assert.AreEqual(expectedCounts[rangeID], s.Search(ddq, 10).TotalHits);
                }
            }

            IOUtils.Dispose(w, r, dir);
        }

        // LUCENE-5178
        [Test]
        public virtual void TestMissingValues()
        {
            AssumeTrue("codec does not support docsWithField", DefaultCodecSupportsDocsWithField);
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            NumericDocValuesField field = new NumericDocValuesField("field", 0L);
            doc.Add(field);
            for (long l = 0; l < 100; l++)
            {
                if (l % 5 == 0)
                {
                    // Every 5th doc is missing the value:
                    w.AddDocument(new Document());
                    continue;
                }
                field.SetInt64Value(l);
                w.AddDocument(doc);
            }

            IndexReader r = w.GetReader();

            FacetsCollector fc = new FacetsCollector();

            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);
            Facets facets = new Int64RangeFacetCounts("field", fc,
                new Int64Range("less than 10", 0L, true, 10L, false),
                new Int64Range("less than or equal to 10", 0L, true, 10L, true),
                new Int64Range("over 90", 90L, false, 100L, false),
                new Int64Range("90 or above", 90L, true, 100L, false),
                new Int64Range("over 1000", 1000L, false, long.MaxValue, false));

            Assert.AreEqual("dim=field path=[] value=16 childCount=5\n  less than 10 (8)\n  less than or equal to 10 (8)\n  over 90 (8)\n  90 or above (8)\n  over 1000 (0)\n",
                facets.GetTopChildren(10, "field").ToString());

            IOUtils.Dispose(w, r, d);
        }

        [Test]
        public virtual void TestCustomDoublesValueSource()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            writer.AddDocument(doc);
            writer.AddDocument(doc);
            writer.AddDocument(doc);

            // Test wants 3 docs in one segment:
            writer.ForceMerge(1);

            var vs = new ValueSourceAnonymousClass(this, doc);

            FacetsConfig config = new FacetsConfig();

            FacetsCollector fc = new FacetsCollector();

            IndexReader r = writer.GetReader();
            IndexSearcher s = NewSearcher(r);
            s.Search(new MatchAllDocsQuery(), fc);

            DoubleRange[] ranges = new DoubleRange[] {
                new DoubleRange("< 1", 0.0, true, 1.0, false),
                new DoubleRange("< 2", 0.0, true, 2.0, false),
                new DoubleRange("< 5", 0.0, true, 5.0, false),
                new DoubleRange("< 10", 0.0, true, 10.0, false),
                new DoubleRange("< 20", 0.0, true, 20.0, false),
                new DoubleRange("< 50", 0.0, true, 50.0, false)
            };

            Filter fastMatchFilter;
            AtomicBoolean filterWasUsed = new AtomicBoolean();
            if (Random.NextBoolean())
            {
                // Sort of silly:
                fastMatchFilter = new CachingWrapperFilterAnonymousClass(this, new QueryWrapperFilter(new MatchAllDocsQuery()), filterWasUsed);
            }
            else
            {
                fastMatchFilter = null;
            }

            if (Verbose)
            {
                Console.WriteLine("TEST: fastMatchFilter=" + fastMatchFilter);
            }

            Facets facets = new DoubleRangeFacetCounts("field", vs, fc, fastMatchFilter, ranges);

            Assert.AreEqual("dim=field path=[] value=3 childCount=6\n  < 1 (0)\n  < 2 (1)\n  < 5 (3)\n  < 10 (3)\n  < 20 (3)\n  < 50 (3)\n",
                facets.GetTopChildren(10, "field").ToString());
            Assert.IsTrue(fastMatchFilter is null || filterWasUsed);

            DrillDownQuery ddq = new DrillDownQuery(config);
            ddq.Add("field", ranges[1].GetFilter(fastMatchFilter, vs));

            // Test simple drill-down:
            Assert.AreEqual(1, s.Search(ddq, 10).TotalHits);

            // Test drill-sideways after drill-down
            DrillSideways ds = new DrillSidewaysAnonymousClass2(this, s, config, (TaxonomyReader)null, vs, ranges, fastMatchFilter);


            DrillSidewaysResult dsr = ds.Search(ddq, 10);
            Assert.AreEqual(1, dsr.Hits.TotalHits);
            Assert.AreEqual("dim=field path=[] value=3 childCount=6\n  < 1 (0)\n  < 2 (1)\n  < 5 (3)\n  < 10 (3)\n  < 20 (3)\n  < 50 (3)\n",
                dsr.Facets.GetTopChildren(10, "field").ToString());

            IOUtils.Dispose(r, writer, dir);
        }

        private sealed class ValueSourceAnonymousClass : ValueSource
        {
            private readonly TestRangeFacetCounts outerInstance;

            private readonly Document doc;

            public ValueSourceAnonymousClass(TestRangeFacetCounts outerInstance, Document doc)
            {
                this.outerInstance = outerInstance;
                this.doc = doc;
            }

            public override FunctionValues GetValues(IDictionary ignored, AtomicReaderContext ignored2)
            {
                return new DoubleDocValuesAnonymousClass(this);
            }

            private sealed class DoubleDocValuesAnonymousClass : DoubleDocValues
            {
                private readonly ValueSourceAnonymousClass outerInstance;

                public DoubleDocValuesAnonymousClass(ValueSourceAnonymousClass outerInstance)
                    : base(null)
                {
                    this.outerInstance = outerInstance;
                }

                public override double DoubleVal(int doc)
                {
                    return doc + 1;
                }
            }

            public override bool Equals(object o)
            {
                throw UnsupportedOperationException.Create();
            }

            public override int GetHashCode()
            {
                throw UnsupportedOperationException.Create();
            }

            public override string GetDescription()
            {
                throw UnsupportedOperationException.Create();
            }

        }

        private sealed class CachingWrapperFilterAnonymousClass : CachingWrapperFilter
        {
            private readonly TestRangeFacetCounts outerInstance;

            private readonly AtomicBoolean filterWasUsed;

            public CachingWrapperFilterAnonymousClass(TestRangeFacetCounts outerInstance, QueryWrapperFilter org, AtomicBoolean filterWasUsed)
                : base(org)
            {
                this.outerInstance = outerInstance;
                this.filterWasUsed = filterWasUsed;
            }

            protected override DocIdSet CacheImpl(DocIdSetIterator iterator, AtomicReader reader)
            {
                var cached = new FixedBitSet(reader.MaxDoc);
                filterWasUsed.Value = true;
                cached.Or(iterator);
                return cached;
            }
        }

        private sealed class DrillSidewaysAnonymousClass2 : DrillSideways
        {
            private readonly TestRangeFacetCounts outerInstance;

            private readonly ValueSource vs;
            private readonly DoubleRange[] ranges;
            private readonly Filter fastMatchFilter;


            public DrillSidewaysAnonymousClass2(TestRangeFacetCounts outerInstance, IndexSearcher indexSearcher, FacetsConfig facetsConfig, TaxonomyReader org, ValueSource valueSource, DoubleRange[] doubleRanges, Filter filter)
                : base(indexSearcher, facetsConfig, org)
            {
                this.outerInstance = outerInstance;
                this.vs = valueSource;
                this.ranges = doubleRanges;
                this.fastMatchFilter = filter;
            }


            protected override Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(drillSideways.Length == 1);
                return new DoubleRangeFacetCounts("field", vs, drillSideways[0], fastMatchFilter, ranges);
            }

            protected override bool ScoreSubDocsAtOnce => Random.NextBoolean();
        }
    }
}