/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Contrib.Spatial.Test
{
    /// <summary>
    /// Utility class for asserting expected hits in tests.
    /// 
    /// Taken from apache.lucene.search
    /// </summary>
    public class CheckHits : LuceneTestCase
    {
        /*
* Asserts that the explanation value for every document matching a
* query corresponds with the true score.  Optionally does "deep" 
* testing of the explanation details.
*
* @see ExplanationAsserter
* @param query the query to test
* @param searcher the searcher to test the query against
* @param defaultFieldName used for displaing the query in assertion messages
* @param deep indicates whether a deep comparison of sub-Explanation details should be executed
*/
        public static void checkExplanations(Query query,
                                             String defaultFieldName,
                                             IndexSearcher searcher,
                                             bool deep = false)
        {

            searcher.Search(query,
                            new ExplanationAsserter
                                (query, defaultFieldName, searcher, deep));

        }

        public class ExplanationAsserter : Collector
        {
            /*
 * Some explains methods calculate their values though a slightly
 * different  order of operations from the actual scoring method ...
 * this allows for a small amount of relative variation
 */
            public static float EXPLAIN_SCORE_TOLERANCE_DELTA = 0.001f;

            /*
             * In general we use a relative epsilon, but some tests do crazy things
             * like boost documents with 0, creating tiny tiny scores where the
             * relative difference is large but the absolute difference is tiny.
             * we ensure the the epsilon is always at least this big.
             */
            public static float EXPLAIN_SCORE_TOLERANCE_MINIMUM = 1e-6f;

            private Query q;
            private IndexSearcher s;
            private String d;
            private bool deep;

            private Scorer scorer;
            private int @base = 0;

            /* Constructs an instance which does shallow tests on the Explanation */

            public ExplanationAsserter(Query q, String defaultFieldName, IndexSearcher s)
                : this(q, defaultFieldName, s, false)
            {
            }

            public ExplanationAsserter(Query q, String defaultFieldName, IndexSearcher s, bool deep)
            {
                this.q = q;
                this.s = s;
                this.d = q.ToString(defaultFieldName);
                this.deep = deep;
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public override void Collect(int doc)
            {
                Explanation exp = null;
                doc = doc + @base;
                try
                {
                    exp = s.Explain(q, doc);
                }
                catch (IOException e)
                {
                    throw new Exception
                        ("exception in hitcollector of [[" + d + "]] for #" + doc, e);
                }

                assertNotNull("Explanation of [[" + d + "]] for #" + doc + " is null", exp);
                verifyExplanation(d, doc, scorer.Score(), deep, exp);
                assertTrue("Explanation of [[" + d + "]] for #" + doc +
                                           " does not indicate match: " + exp.ToString(), exp.IsMatch);
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                @base = docBase;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }

            /* 
 * Assert that an explanation has the expected score, and optionally that its
 * sub-details max/sum/factor match to that score.
 *
 * @param q String representation of the query for assertion messages
 * @param doc Document ID for assertion messages
 * @param score Real score value of doc with query q
 * @param deep indicates whether a deep comparison of sub-Explanation details should be executed
 * @param expl The Explanation to match against score
 */
            public static void verifyExplanation(String q,
                                                 int doc,
                                                 float score,
                                                 bool deep,
                                                 Explanation expl)
            {
                float value = expl.Value;
                assertEquals(q + ": score(doc=" + doc + ")=" + score +
                    " != explanationScore=" + value + " Explanation: " + expl,
                    score, value, explainToleranceDelta(score, value));

                if (!deep) return;

                var detail = expl.GetDetails();
                // TODO: can we improve this entire method? its really geared to work only with TF/IDF
                if (expl.Description.EndsWith("computed from:"))
                {
                    return; // something more complicated.
                }
                if (detail != null)
                {
                    if (detail.Length == 1)
                    {
                        // simple containment, unless its a freq of: (which lets a query explain how the freq is calculated), 
                        // just verify contained expl has same score
                        if (!expl.Description.EndsWith("with freq of:"))
                            verifyExplanation(q, doc, score, deep, detail[0]);
                    }
                    else
                    {
                        // explanation must either:
                        // - end with one of: "product of:", "sum of:", "max of:", or
                        // - have "max plus <x> times others" (where <x> is float).
                        float x = 0;
                        String descr = expl.Description.ToLowerInvariant();
                        bool productOf = descr.EndsWith("product of:");
                        bool sumOf = descr.EndsWith("sum of:");
                        bool maxOf = descr.EndsWith("max of:");
                        bool maxTimesOthers = false;
                        if (!(productOf || sumOf || maxOf))
                        {
                            // maybe 'max plus x times others'
                            int k1 = descr.IndexOf("max plus ");
                            if (k1 >= 0)
                            {
                                k1 += "max plus ".Length;
                                int k2 = descr.IndexOf(" ", k1);
                                try
                                {
                                    x = float.Parse(descr.Substring(k1, k2).Trim());
                                    if (descr.Substring(k2).Trim().Equals("times others of:"))
                                    {
                                        maxTimesOthers = true;
                                    }
                                }
                                catch (FormatException e)
                                {
                                }
                            }
                        }
                        // TODO: this is a TERRIBLE assertion!!!!
                        assertTrue(
                            q + ": multi valued explanation description=\"" + descr
                            + "\" must be 'max of plus x times others' or end with 'product of'"
                            + " or 'sum of:' or 'max of:' - " + expl,
                            productOf || sumOf || maxOf || maxTimesOthers);
                        float sum = 0;
                        float product = 1;
                        float max = 0;
                        for (int i = 0; i < detail.Length; i++)
                        {
                            float dval = detail[i].Value;
                            verifyExplanation(q, doc, dval, deep, detail[i]);
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
                            assertTrue("should never get here!", false);
                        }
                        assertEquals(q + ": actual subDetails combined==" + combined +
                            " != value=" + value + " Explanation: " + expl,
                            combined, value, explainToleranceDelta(combined, value));
                    }
                }
            }

            /* returns a reasonable epsilon for comparing two floats,
   *  where minor differences are acceptable such as score vs. explain */
            public static float explainToleranceDelta(float f1, float f2)
            {
                return Math.Max(EXPLAIN_SCORE_TOLERANCE_MINIMUM, Math.Max(Math.Abs(f1), Math.Abs(f2)) * EXPLAIN_SCORE_TOLERANCE_DELTA);
            }
        }
    }
}
