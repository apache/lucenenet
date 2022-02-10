using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
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

    /// <summary>
    /// Utility class for asserting expected hits in tests.
    /// </summary>
    public static class CheckHits // LUCENENET specific - made static because all of its members are static
    {
        /// <summary>
        /// Some explains methods calculate their values though a slightly
        /// different  order of operations from the actual scoring method ...
        /// this allows for a small amount of relative variation
        /// </summary>
        public static float EXPLAIN_SCORE_TOLERANCE_DELTA = 0.001f;

        /// <summary>
        /// In general we use a relative epsilon, but some tests do crazy things
        /// like boost documents with 0, creating tiny tiny scores where the
        /// relative difference is large but the absolute difference is tiny.
        /// we ensure the the epsilon is always at least this big.
        /// </summary>
        public static float EXPLAIN_SCORE_TOLERANCE_MINIMUM = 1e-6f;

        /// <summary>
        /// Tests that all documents up to maxDoc which are *not* in the
        /// expected result set, have an explanation which indicates that
        /// the document does not match
        /// </summary>
        public static void CheckNoMatchExplanations(Query q, string defaultFieldName, IndexSearcher searcher, int[] results)
        {
            string d = q.ToString(defaultFieldName);
            JCG.SortedSet<int> ignore = new JCG.SortedSet<int>();
            for (int i = 0; i < results.Length; i++)
            {
                ignore.Add(Convert.ToInt32(results[i], CultureInfo.InvariantCulture));
            }

            int maxDoc = searcher.IndexReader.MaxDoc;
            for (int doc = 0; doc < maxDoc; doc++)
            {
                if (ignore.Contains(Convert.ToInt32(doc, CultureInfo.InvariantCulture)))
                {
                    continue;
                }

                Explanation exp = searcher.Explain(q, doc);
                Assert.IsNotNull(exp, "Explanation of [[" + d + "]] for #" + doc + " is null");
                Assert.IsFalse(exp.IsMatch, "Explanation of [[" + d + "]] for #" + doc + " doesn't indicate non-match: " + exp.ToString());
            }
        }

        /// <summary>
        /// Tests that a query matches the an expected set of documents using a
        /// HitCollector.
        /// <para>
        /// Note that when using the HitCollector API, documents will be collected
        /// if they "match" regardless of what their score is.
        /// </para>
        /// </summary>
        /// <param name="query"> The query to test. </param>
        /// <param name="searcher"> The searcher to test the query against. </param>
        /// <param name="defaultFieldName"> Used for displaying the query in assertion messages. </param>
        /// <param name="results"> A list of documentIds that must match the query. </param>
        /// <seealso cref="DoCheckHits(Random, Query, string, IndexSearcher, int[])"/>
        public static void CheckHitCollector(Random random, Query query, string defaultFieldName, IndexSearcher searcher, int[] results)
        {
            QueryUtils.Check(random, query, searcher);

            Trace.TraceInformation("Checked");

            JCG.SortedSet<int> correct = new JCG.SortedSet<int>(results);
            JCG.SortedSet<int> actual = new JCG.SortedSet<int>();
            ICollector c = new SetCollector(actual);

            searcher.Search(query, c);

            Assert.AreEqual(correct, actual, aggressive: false, () => "Simple: " + query.ToString(defaultFieldName));

            for (int i = -1; i < 2; i++)
            {
                actual.Clear();
                IndexSearcher s = QueryUtils.WrapUnderlyingReader(random, searcher, i);
                s.Search(query, c);
                Assert.AreEqual(correct, actual, aggressive: false, () => "Wrap Reader " + i + ": " + query.ToString(defaultFieldName));
            }
        }

        // LUCENENET specific - de-nested SetCollector

        /// <summary>
        /// Tests that a query matches the an expected set of documents using Hits.
        ///
        /// <para>Note that when using the Hits API, documents will only be returned
        /// if they have a positive normalized score.
        /// </para>
        /// </summary>
        /// <param name="query"> the query to test </param>
        /// <param name="searcher"> the searcher to test the query against </param>
        /// <param name="defaultFieldName"> used for displaing the query in assertion messages </param>
        /// <param name="results"> a list of documentIds that must match the query </param>
        /// <seealso cref="CheckHitCollector(Random, Query, string, IndexSearcher, int[])"/>
        public static void DoCheckHits(Random random, Query query, string defaultFieldName, IndexSearcher searcher, int[] results)
        {
            ScoreDoc[] hits = searcher.Search(query, 1000).ScoreDocs;

            JCG.SortedSet<int> correct = new JCG.SortedSet<int>();
            for (int i = 0; i < results.Length; i++)
            {
                correct.Add(Convert.ToInt32(results[i], CultureInfo.InvariantCulture));
            }

            JCG.SortedSet<int> actual = new JCG.SortedSet<int>();
            for (int i = 0; i < hits.Length; i++)
            {
                actual.Add(Convert.ToInt32(hits[i].Doc, CultureInfo.InvariantCulture));
            }

            Assert.AreEqual(correct, actual, aggressive: false, () => query.ToString(defaultFieldName));

            QueryUtils.Check(random, query, searcher, LuceneTestCase.Rarely(random));
        }

        /// <summary>
        /// Tests that a Hits has an expected order of documents. </summary>
        public static void CheckDocIds(string mes, int[] results, ScoreDoc[] hits)
        {
            Assert.AreEqual(hits.Length, results.Length, mes + " nr of hits");
            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(results[i], hits[i].Doc, mes + " doc nrs for hit " + i);
            }
        }

        /// <summary>
        /// Tests that two queries have an expected order of documents,
        /// and that the two queries have the same score values.
        /// </summary>
        public static void CheckHitsQuery(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2, int[] results)
        {
            CheckDocIds("hits1", results, hits1);
            CheckDocIds("hits2", results, hits2);
            CheckEqual(query, hits1, hits2);
        }

        public static void CheckEqual(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2)
        {
            const float scoreTolerance = 1.0e-6f;
            if (hits1.Length != hits2.Length)
            {
                Assert.Fail("Unequal lengths: hits1=" + hits1.Length + ",hits2=" + hits2.Length);
            }
            for (int i = 0; i < hits1.Length; i++)
            {
                if (hits1[i].Doc != hits2[i].Doc)
                {
                    Assert.Fail("Hit " + i + " docnumbers don't match\n" + Hits2str(hits1, hits2, 0, 0) + "for query:" + query.ToString());
                }

                if ((hits1[i].Doc != hits2[i].Doc) || Math.Abs(hits1[i].Score - hits2[i].Score) > scoreTolerance)
                {
                    Assert.Fail("Hit " + i + ", doc nrs " + hits1[i].Doc + " and " + hits2[i].Doc + "\nunequal       : " + hits1[i].Score + "\n           and: " + hits2[i].Score + "\nfor query:" + query.ToString());
                }
            }
        }

        public static string Hits2str(ScoreDoc[] hits1, ScoreDoc[] hits2, int start, int end)
        {
            StringBuilder sb = new StringBuilder();
            int len1 = hits1 is null ? 0 : hits1.Length;
            int len2 = hits2 is null ? 0 : hits2.Length;
            if (end <= 0)
            {
                end = Math.Max(len1, len2);
            }

            sb.Append("Hits length1=").Append(len1).Append("\tlength2=").Append(len2);

            sb.Append('\n');
            for (int i = start; i < end; i++)
            {
                sb.Append("hit=").Append(i).Append(':');
                if (i < len1)
                {
                    sb.Append(" doc").Append(hits1[i].Doc).Append('=').Append(hits1[i].Score);
                }
                else
                {
                    sb.Append("               ");
                }
                sb.Append(",\t");
                if (i < len2)
                {
                    sb.Append(" doc").Append(hits2[i].Doc).Append('=').Append(hits2[i].Score);
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static string TopDocsString(TopDocs docs, int start, int end) // LUCENENET specific - renamed from TopdocsString
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TopDocs totalHits=").Append(docs.TotalHits).Append(" top=").Append(docs.ScoreDocs.Length).Append('\n');
            if (end <= 0)
            {
                end = docs.ScoreDocs.Length;
            }
            else
            {
                end = Math.Min(end, docs.ScoreDocs.Length);
            }
            for (int i = start; i < end; i++)
            {
                sb.Append('\t');
                sb.Append(i);
                sb.Append(") doc=");
                sb.Append(docs.ScoreDocs[i].Doc);
                sb.Append("\tscore=");
                sb.Append(docs.ScoreDocs[i].Score);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Asserts that the explanation value for every document matching a
        /// query corresponds with the true score.
        /// <para/>
        /// See <see cref="CheckExplanations(Query, String, IndexSearcher, bool)"/> for a
        /// "deep" testing of the explanation details.
        /// </summary>
        /// <seealso cref="ExplanationAsserter"/>
        /// <seealso cref="CheckExplanations(Query, String, IndexSearcher, bool)"/>
        /// <param name="query"> The query to test. </param>
        /// <param name="searcher"> The searcher to test the query against. </param>
        /// <param name="defaultFieldName"> Used for displaing the query in assertion messages. </param>
        public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher searcher)
        {
            CheckExplanations(query, defaultFieldName, searcher, false);
        }

        /// <summary>
        /// Asserts that the explanation value for every document matching a
        /// query corresponds with the true score.  Optionally does "deep"
        /// testing of the explanation details.
        /// </summary>
        /// <seealso cref="ExplanationAsserter"/>
        /// <param name="query"> The query to test. </param>
        /// <param name="searcher"> The searcher to test the query against. </param>
        /// <param name="defaultFieldName"> Used for displaing the query in assertion messages. </param>
        /// <param name="deep"> Indicates whether a deep comparison of sub-Explanation details should be executed. </param>
        public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher searcher, bool deep)
        {
            searcher.Search(query, new ExplanationAsserter(query, defaultFieldName, searcher, deep));
        }

        /// <summary>
        /// Returns a reasonable epsilon for comparing two floats,
        /// where minor differences are acceptable such as score vs. explain.
        /// </summary>
        public static float ExplainToleranceDelta(float f1, float f2)
        {
            return Math.Max(EXPLAIN_SCORE_TOLERANCE_MINIMUM, Math.Max(Math.Abs(f1), Math.Abs(f2)) * EXPLAIN_SCORE_TOLERANCE_DELTA);
        }

        /// <summary>
        /// Assert that an explanation has the expected score, and optionally that its
        /// sub-details max/sum/factor match to that score.
        /// </summary>
        /// <param name="q"> String representation of the query for assertion messages. </param>
        /// <param name="doc"> Document ID for assertion messages. </param>
        /// <param name="score"> Real score value of doc with query <paramref name="q"/>. </param>
        /// <param name="deep"> Indicates whether a deep comparison of sub-Explanation details should be executed. </param>
        /// <param name="expl"> The <see cref="Explanation"/> to match against score. </param>
        public static void VerifyExplanation(string q, int doc, float score, bool deep, Explanation expl)
        {
            float value = expl.Value;
            Assert.AreEqual(score, value, ExplainToleranceDelta(score, value), q + ": score(doc=" + doc + ")=" + score + " != explanationScore=" + value + " Explanation: " + expl);

            if (!deep)
            {
                return;
            }

            Explanation[] detail = expl.GetDetails();
            // TODO: can we improve this entire method? its really geared to work only with TF/IDF
            if (expl.Description.EndsWith("computed from:", StringComparison.Ordinal))
            {
                return; // something more complicated.
            }
            if (detail != null)
            {
                if (detail.Length == 1)
                {
                    // simple containment, unless its a freq of: (which lets a query explain how the freq is calculated),
                    // just verify contained expl has same score
                    if (!expl.Description.EndsWith("with freq of:", StringComparison.Ordinal))
                    {
                        VerifyExplanation(q, doc, score, deep, detail[0]);
                    }
                }
                else
                {
                    // explanation must either:
                    // - end with one of: "product of:", "sum of:", "max of:", or
                    // - have "max plus <x> times others" (where <x> is float).
                    float x = 0;
                    string descr = CultureInfo.InvariantCulture.TextInfo.ToLower(expl.Description);
                    bool productOf = descr.EndsWith("product of:", StringComparison.Ordinal);
                    bool sumOf = descr.EndsWith("sum of:", StringComparison.Ordinal);
                    bool maxOf = descr.EndsWith("max of:", StringComparison.Ordinal);
                    bool maxTimesOthers = false;
                    if (!(productOf || sumOf || maxOf))
                    {
                        // maybe 'max plus x times others'
                        int k1 = descr.IndexOf("max plus ", StringComparison.Ordinal);
                        if (k1 >= 0)
                        {
                            k1 += "max plus ".Length;
                            int k2 = descr.IndexOf(" ", k1, StringComparison.Ordinal);

                            // LUCENENET NOTE: Using current culture here is intentional because
                            // we are parsing from text that was made using the current culture.
                            if (float.TryParse(descr.Substring(k1, k2 - k1).Trim(), out x) &&
                                descr.Substring(k2).Trim().Equals("times others of:", StringComparison.Ordinal))
                            {
                                maxTimesOthers = true;
                            }
                        }
                    }
                    // TODO: this is a TERRIBLE assertion!!!!
                    Assert.IsTrue(productOf || sumOf || maxOf || maxTimesOthers,
                        q + ": multi valued explanation description=\"" + descr
                        + "\" must be 'max of plus x times others' or end with 'product of'"
                        + " or 'sum of:' or 'max of:' - " + expl);
                    float sum = 0;
                    float product = 1;
                    float max = 0;
                    for (int i = 0; i < detail.Length; i++)
                    {
                        float dval = detail[i].Value;
                        VerifyExplanation(q, doc, dval, deep, detail[i]);
                        product *= dval;
                        sum += dval;
                        max = Math.Max(max, dval);
                    }
                    float combined = 0;
                    if (productOf)
                    {
                        combined = product;
                    }
                    else if (sumOf)
                    {
                        combined = sum;
                    }
                    else if (maxOf)
                    {
                        combined = max;
                    }
                    else if (maxTimesOthers)
                    {
                        combined = max + x * (sum - max);
                    }
                    else
                    {
                        Assert.IsTrue(false, "should never get here!");
                    }
                    Assert.AreEqual(combined, value, ExplainToleranceDelta(combined, value),
                        q + ": actual subDetails combined==" + combined + " != value=" + value + " Explanation: " + expl);
                }
            }
        }

        // LUCENENET specific - de-nested ExplanationAssertingSearcher

        // LUCENENET specific - de-nested ExplanationAsserter
    }

    /// <summary>
    /// Just collects document ids into a set.
    /// </summary>
    public class SetCollector : ICollector
    {
        internal readonly ISet<int> bag;

        public SetCollector(ISet<int> bag)
        {
            this.bag = bag;
        }

        internal int @base = 0;

        public virtual void SetScorer(Scorer scorer)
        {
        }

        public virtual void Collect(int doc)
        {
            bag.Add(Convert.ToInt32(doc + @base, CultureInfo.InvariantCulture));
        }

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            @base = context.DocBase;
        }

        public virtual bool AcceptsDocsOutOfOrder => true;
    }

    /// <summary>
    /// An <see cref="IndexSearcher"/> that implicitly checks hte explanation of every match
    /// whenever it executes a search.
    /// </summary>
    /// <seealso cref="ExplanationAsserter"/>
    public class ExplanationAssertingSearcher : IndexSearcher
    {
        public ExplanationAssertingSearcher(IndexReader r)
            : base(r)
        {
        }

        protected virtual void CheckExplanations(Query q)
        {
            base.Search(q, null, new ExplanationAsserter(q, null, this));
        }

        public override TopFieldDocs Search(Query query, Filter filter, int n, Sort sort)
        {
            CheckExplanations(query);
            return base.Search(query, filter, n, sort);
        }

        public override void Search(Query query, ICollector results)
        {
            CheckExplanations(query);
            base.Search(query, results);
        }

        public override void Search(Query query, Filter filter, ICollector results)
        {
            CheckExplanations(query);
            base.Search(query, filter, results);
        }

        public override TopDocs Search(Query query, Filter filter, int n)
        {
            CheckExplanations(query);
            return base.Search(query, filter, n);
        }
    }

    /// <summary>
    /// Asserts that the score explanation for every document matching a
    /// query corresponds with the true score.
    /// <para/>
    /// NOTE: this HitCollector should only be used with the <see cref="Query"/> and <see cref="IndexSearcher"/>
    /// specified at when it is constructed.
    /// </summary>
    /// <seealso cref="CheckHits.VerifyExplanation(string, int, float, bool, Explanation)"/>
    public class ExplanationAsserter : ICollector
    {
        internal Query q;
        internal IndexSearcher s;
        internal string d;
        internal bool deep;

        internal Scorer scorer;
        internal int @base = 0;

        /// <summary>
        /// Constructs an instance which does shallow tests on the Explanation </summary>
        public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s)
            : this(q, defaultFieldName, s, false)
        {
        }

        public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s, bool deep)
        {
            this.q = q;
            this.s = s;
            this.d = q.ToString(defaultFieldName);
            this.deep = deep;
        }

        public virtual void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }

        public virtual void Collect(int doc)
        {
            Explanation exp; // LUCENENET: IDE0059: Remove unnecessary value assignment
            doc = doc + @base;
            try
            {
                exp = s.Explain(q, doc);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create("exception in hitcollector of [[" + d + "]] for #" + doc, e);
            }

            Assert.IsNotNull(exp, "Explanation of [[" + d + "]] for #" + doc + " is null");
            CheckHits.VerifyExplanation(d, doc, scorer.GetScore(), deep, exp);
            Assert.IsTrue(exp.IsMatch, "Explanation of [[" + d + "]] for #" + doc + " does not indicate match: " + exp.ToString());
        }

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            @base = context.DocBase;
        }

        public virtual bool AcceptsDocsOutOfOrder => true;
    }
}