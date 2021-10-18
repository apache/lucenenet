using J2N.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Create sloppy phrase queries for performance test, in an index created using simple doc maker.
    /// </summary>
    public class SimpleSloppyPhraseQueryMaker : SimpleQueryMaker
    {
        /// <seealso cref="SimpleQueryMaker.PrepareQueries()"/>
        protected override Query[] PrepareQueries()
        {
            // extract some 100 words from doc text to an array
            string[] words;
            JCG.List<string> w = new JCG.List<string>();
            StringTokenizer st = new StringTokenizer(SingleDocSource.DOC_TEXT);
            while (st.MoveNext() && w.Count < 100)
            {
                w.Add(st.Current);
            }
            words = w.ToArray();

            // create queries (that would find stuff) with varying slops
            JCG.List<Query> queries = new JCG.List<Query>();
            for (int slop = 0; slop < 8; slop++)
            {
                for (int qlen = 2; qlen < 6; qlen++)
                {
                    for (int wd = 0; wd < words.Length - qlen - slop; wd++)
                    {
                        // ordered
                        int remainedSlop = slop;
                        PhraseQuery q = new PhraseQuery();
                        q.Slop = slop;
                        int wind = wd;
                        for (int i = 0; i < qlen; i++)
                        {
                            q.Add(new Term(DocMaker.BODY_FIELD, words[wind++]));
                            if (remainedSlop > 0)
                            {
                                remainedSlop--;
                                wind++;
                            }
                        }
                        queries.Add(q);
                        // reversed
                        remainedSlop = slop;
                        q = new PhraseQuery();
                        q.Slop = slop + 2 * qlen;
                        wind = wd + qlen + remainedSlop - 1;
                        for (int i = 0; i < qlen; i++)
                        {
                            q.Add(new Term(DocMaker.BODY_FIELD, words[wind--]));
                            if (remainedSlop > 0)
                            {
                                remainedSlop--;
                                wind--;
                            }
                        }
                        queries.Add(q);
                    }
                }
            }
            return queries.ToArray();
        }
    }
}
