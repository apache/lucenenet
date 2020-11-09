using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Diagnostics.CodeAnalysis;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    public class BooleanQueryTst
    {
        private string queryText;
        private readonly int[] expectedDocNrs;
        private SingleFieldTestDb dBase;
        private string fieldName;
        private BasicQueryFactory qf;
        private bool verbose = true;

        public BooleanQueryTst(
            string queryText,
            int[] expectedDocNrs,
            SingleFieldTestDb dBase,
            string fieldName,
            BasicQueryFactory qf)
        {
            this.queryText = queryText;
            this.expectedDocNrs = expectedDocNrs;
            this.dBase = dBase;
            this.fieldName = fieldName;
            this.qf = qf;
        }

        public virtual bool Verbose { set => this.verbose = value; }

        public virtual string QueryText => this.queryText;

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        public virtual int[] ExpectedDocNrs => this.expectedDocNrs;

        internal class TestCollector : ICollector
        { // FIXME: use check hits from Lucene tests
            private int totalMatched;
            private bool[] encountered;
            private Scorer scorer = null;
            private int docBase = 0;
            private BooleanQueryTst parent;

            public TestCollector(BooleanQueryTst parent)
            {
                totalMatched = 0;
                encountered = new bool[parent.expectedDocNrs.Length];
                this.parent = parent;
            }

            public virtual void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public virtual bool AcceptsDocsOutOfOrder => true;

            public virtual void SetNextReader(AtomicReaderContext context)
            {
                docBase = context.DocBase;
            }

            public virtual void Collect(int docNr)
            {
                float score = scorer.GetScore();
                docNr += docBase;
                /* System.out.println(docNr + " '" + dBase.getDocs()[docNr] + "': " + score); */
                Assert.True(score > 0.0, parent.QueryText + ": positive score");
                Assert.True(totalMatched < parent.ExpectedDocNrs.Length, parent.QueryText + ": too many hits");
                int i;
                for (i = 0; i < parent.expectedDocNrs.Length; i++)
                {
                    if ((!encountered[i]) && (parent.ExpectedDocNrs[i] == docNr))
                    {
                        encountered[i] = true;
                        break;
                    }
                }
                if (i == parent.ExpectedDocNrs.Length)
                {
                    Assert.True(false, parent.QueryText + ": doc nr for hit not expected: " + docNr);
                }
                totalMatched++;
            }

            public void CheckNrHits()
            {
                Assert.AreEqual(parent.ExpectedDocNrs.Length, totalMatched, parent.QueryText + ": nr of hits");
            }
        }

        public void DoTest()
        {

            if (verbose)
            {
                Console.WriteLine("");
                Console.WriteLine("Query: " + queryText);
            }

            SrndQuery lq = Parser.QueryParser.Parse(queryText);

            /* if (verbose) System.out.println("Srnd: " + lq.toString()); */

            Search.Query query = lq.MakeLuceneQueryField(fieldName, qf);
            /* if (verbose) System.out.println("Lucene: " + query.toString()); */

            TestCollector tc = new TestCollector(this);
            using (IndexReader reader = DirectoryReader.Open(dBase.Db))
            {
                IndexSearcher searcher = new IndexSearcher(reader);

                searcher.Search(query, tc);
            }
            tc.CheckNrHits();
        }
    }
}
