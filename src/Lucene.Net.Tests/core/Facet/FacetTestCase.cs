using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Facet;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Facet
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


    using CachedOrdinalsReader = Lucene.Net.Facet.Taxonomy.CachedOrdinalsReader;
    using DocValuesOrdinalsReader = Lucene.Net.Facet.Taxonomy.DocValuesOrdinalsReader;
    using FastTaxonomyFacetCounts = Lucene.Net.Facet.Taxonomy.FastTaxonomyFacetCounts;
    using OrdinalsReader = Lucene.Net.Facet.Taxonomy.OrdinalsReader;
    using TaxonomyFacetCounts = Lucene.Net.Facet.Taxonomy.TaxonomyFacetCounts;
    using TaxonomyReader = Lucene.Net.Facet.Taxonomy.TaxonomyReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public abstract class FacetTestCase : LuceneTestCase
    {
        public virtual Facets GetTaxonomyFacetCounts(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector c)
        {
            return GetTaxonomyFacetCounts(taxoReader, config, c, FacetsConfig.DEFAULT_INDEX_FIELD_NAME);
        }
        public virtual Facets GetTaxonomyFacetCounts(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector c, string indexFieldName)
        {
            Facets facets;
            if (Random().NextBoolean())
            {
                facets = new FastTaxonomyFacetCounts(indexFieldName, taxoReader, config, c);
            }
            else
            {
                OrdinalsReader ordsReader = new DocValuesOrdinalsReader(indexFieldName);
                if (Random().NextBoolean())
                {
                    ordsReader = new CachedOrdinalsReader(ordsReader);
                }
                facets = new TaxonomyFacetCounts(ordsReader, taxoReader, config, c);
            }

            return facets;
        }

        protected internal virtual string[] GetRandomTokens(int count)
        {
            string[] tokens = new string[count];
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = TestUtil.RandomRealisticUnicodeString(Random(), 1, 10);
                //tokens[i] = TestUtil.RandomSimpleString(Random(), 1, 10);
            }
            return tokens;
        }

        protected internal virtual string PickToken(string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (Random().NextBoolean())
                {
                    return tokens[i];
                }
            }

            // Move long tail onto first token:
            return tokens[0];
        }

        protected internal class TestDoc
        {
            public string content;
            public string[] dims;
            public float value;
        }

        protected internal virtual IList<TestDoc> GetRandomDocs(string[] tokens, int count, int numDims)
        {
            IList<TestDoc> docs = new List<TestDoc>();
            for (int i = 0; i < count; i++)
            {
                TestDoc doc = new TestDoc();
                docs.Add(doc);
                doc.content = PickToken(tokens);
                doc.dims = new string[numDims];
                for (int j = 0; j < numDims; j++)
                {
                    doc.dims[j] = PickToken(tokens);
                    if (Random().Next(10) < 3)
                    {
                        break;
                    }
                }
                if (VERBOSE)
                {
                    Console.WriteLine("  doc " + i + ": content=" + doc.content);
                    for (int j = 0; j < numDims; j++)
                    {
                        if (doc.dims[j] != null)
                        {
                            Console.WriteLine("    dim[" + j + "]=" + doc.dims[j]);
                        }
                    }
                }
            }

            return docs;
        }

        protected internal virtual void SortTies(IList<FacetResult> results)
        {
            foreach (FacetResult result in results)
            {
                SortTies(result.labelValues);
            }
        }

        protected internal virtual void SortTies(LabelAndValue[] labelValues)
        {
            double lastValue = -1;
            int numInRow = 0;
            int i = 0;
            while (i <= labelValues.Length)
            {
                if (i < labelValues.Length && (double)labelValues[i].value == lastValue)
                {
                    numInRow++;
                }
                else
                {
                    if (numInRow > 1)
                    {
                        Array.Sort(labelValues, i - numInRow, i, new ComparatorAnonymousInnerClassHelper(this));
                    }
                    numInRow = 1;
                    if (i < labelValues.Length)
                    {
                        lastValue = (double)labelValues[i].value;
                    }
                }
                i++;
            }
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<LabelAndValue>
        {
            private readonly FacetTestCase outerInstance;

            public ComparatorAnonymousInnerClassHelper(FacetTestCase outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual int Compare(LabelAndValue a, LabelAndValue b)
            {
                Debug.Assert((double)a.value == (double)b.value);
                return (new BytesRef(a.label)).CompareTo(new BytesRef(b.label));
            }
        }

        protected internal virtual void SortLabelValues(IList<LabelAndValue> labelValues)
        {
            var resArray = labelValues.ToArray();
            Array.Sort(resArray,new ComparatorAnonymousInnerClassHelper2(this));
            labelValues = resArray.ToList();
        }

        private class ComparatorAnonymousInnerClassHelper2 : IComparer<LabelAndValue>
        {
            private readonly FacetTestCase outerInstance;

            public ComparatorAnonymousInnerClassHelper2(FacetTestCase outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual int Compare(LabelAndValue a, LabelAndValue b)
            {
                if ((double)a.value > (double)b.value)
                {
                    return -1;
                }
                else if ((double)a.value < (double)b.value)
                {
                    return 1;
                }
                else
                {
                    return (new BytesRef(a.label)).CompareTo(new BytesRef(b.label));
                }
            }
        }

        protected internal virtual void SortFacetResults(IList<FacetResult> results)
        {
            var resArray = results.ToArray();
            Array.Sort(resArray, new ComparatorAnonymousInnerClassHelper3(this));
            results = resArray.ToList();
        }

        private class ComparatorAnonymousInnerClassHelper3 : IComparer<FacetResult>
        {
            private readonly FacetTestCase outerInstance;

            public ComparatorAnonymousInnerClassHelper3(FacetTestCase outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual int Compare(FacetResult a, FacetResult b)
            {
                if ((double)a.value > (double)b.value)
                {
                    return -1;
                }
                else if ((double)b.value > (double)a.value)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        [Test]
        protected internal virtual void AssertFloatValuesEquals(IList<FacetResult> a, IList<FacetResult> b)
        {
            Assert.AreEqual(a.Count, b.Count);
            float lastValue = float.PositiveInfinity;
            IDictionary<string, FacetResult> aByDim = new Dictionary<string, FacetResult>();
            for (int i = 0; i < a.Count; i++)
            {
                Assert.True((float)a[i].value <= lastValue);
                lastValue = (float)a[i].value;
                aByDim[a[i].dim] = a[i];
            }
            lastValue = float.PositiveInfinity;
            IDictionary<string, FacetResult> bByDim = new Dictionary<string, FacetResult>();
            for (int i = 0; i < b.Count; i++)
            {
                bByDim[b[i].dim] = b[i];
                Assert.True((float)b[i].value <= lastValue);
                lastValue = (float)b[i].value;
            }
            foreach (string dim in aByDim.Keys)
            {
                AssertFloatValuesEquals(aByDim[dim], bByDim[dim]);
            }
        }

        [Test]
        protected internal virtual void AssertFloatValuesEquals(FacetResult a, FacetResult b)
        {
            Assert.AreEqual(a.dim, b.dim);
            Assert.True(Arrays.Equals(a.path, b.path));
            Assert.AreEqual(a.childCount, b.childCount);
            Assert.AreEqual((float)a.value, (float)b.value, (float)a.value / 1e5);
            Assert.AreEqual(a.labelValues.Length, b.labelValues.Length);
            for (int i = 0; i < a.labelValues.Length; i++)
            {
                Assert.AreEqual(a.labelValues[i].label, b.labelValues[i].label);
                Assert.AreEqual((float)a.labelValues[i].value, (float)b.labelValues[i].value, (float)a.labelValues[i].value / 1e5);
            }
        }
    }

}