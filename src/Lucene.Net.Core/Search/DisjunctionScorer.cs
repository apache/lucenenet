using System.Collections.Generic;
using System.Diagnostics;

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
    /// Base class for Scorers that score disjunctions.
    /// Currently this just provides helper methods to manage the heap.
    /// </summary>
    internal abstract class DisjunctionScorer : Scorer
    {
        protected readonly Scorer[] m_subScorers;

        /// <summary>
        /// The document number of the current match. </summary>
        protected int m_doc = -1;

        protected int m_numScorers;

        protected DisjunctionScorer(Weight weight, Scorer[] subScorers)
            : base(weight)
        {
            this.m_subScorers = subScorers;
            this.m_numScorers = subScorers.Length;
            Heapify();
        }

        /// <summary>
        /// Organize subScorers into a min heap with scorers generating the earliest document on top.
        /// </summary>
        protected void Heapify()
        {
            for (int i = (m_numScorers >> 1) - 1; i >= 0; i--)
            {
                HeapAdjust(i);
            }
        }

        /// <summary>
        /// The subtree of subScorers at root is a min heap except possibly for its root element.
        /// Bubble the root down as required to make the subtree a heap.
        /// </summary>
        protected void HeapAdjust(int root)
        {
            Scorer scorer = m_subScorers[root];
            int doc = scorer.DocID;
            int i = root;
            while (i <= (m_numScorers >> 1) - 1)
            {
                int lchild = (i << 1) + 1;
                Scorer lscorer = m_subScorers[lchild];
                int ldoc = lscorer.DocID;
                int rdoc = int.MaxValue, rchild = (i << 1) + 2;
                Scorer rscorer = null;
                if (rchild < m_numScorers)
                {
                    rscorer = m_subScorers[rchild];
                    rdoc = rscorer.DocID;
                }
                if (ldoc < doc)
                {
                    if (rdoc < ldoc)
                    {
                        m_subScorers[i] = rscorer;
                        m_subScorers[rchild] = scorer;
                        i = rchild;
                    }
                    else
                    {
                        m_subScorers[i] = lscorer;
                        m_subScorers[lchild] = scorer;
                        i = lchild;
                    }
                }
                else if (rdoc < doc)
                {
                    m_subScorers[i] = rscorer;
                    m_subScorers[rchild] = scorer;
                    i = rchild;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Remove the root Scorer from subScorers and re-establish it as a heap
        /// </summary>
        protected void HeapRemoveRoot()
        {
            if (m_numScorers == 1)
            {
                m_subScorers[0] = null;
                m_numScorers = 0;
            }
            else
            {
                m_subScorers[0] = m_subScorers[m_numScorers - 1];
                m_subScorers[m_numScorers - 1] = null;
                --m_numScorers;
                HeapAdjust(0);
            }
        }

        public override sealed ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(m_numScorers);
                for (int i = 0; i < m_numScorers; i++)
                {
                    children.Add(new ChildScorer(m_subScorers[i], "SHOULD"));
                }
                return children;
            }
        }

        public override long Cost()
        {
            long sum = 0;
            for (int i = 0; i < m_numScorers; i++)
            {
                sum += m_subScorers[i].Cost();
            }
            return sum;
        }

        public override int DocID
        {
            get { return m_doc; }
        }

        public override int NextDoc()
        {
            Debug.Assert(m_doc != NO_MORE_DOCS);
            while (true)
            {
                if (m_subScorers[0].NextDoc() != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (m_numScorers == 0)
                    {
                        return m_doc = NO_MORE_DOCS;
                    }
                }
                if (m_subScorers[0].DocID != m_doc)
                {
                    AfterNext();
                    return m_doc;
                }
            }
        }

        public override int Advance(int target)
        {
            Debug.Assert(m_doc != NO_MORE_DOCS);
            while (true)
            {
                if (m_subScorers[0].Advance(target) != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (m_numScorers == 0)
                    {
                        return m_doc = NO_MORE_DOCS;
                    }
                }
                if (m_subScorers[0].DocID >= target)
                {
                    AfterNext();
                    return m_doc;
                }
            }
        }

        /// <summary>
        /// Called after next() or advance() land on a new document.
        /// <p>
        /// {@code subScorers[0]} will be positioned to the new docid,
        /// which could be {@code NO_MORE_DOCS} (subclass must handle this).
        /// <p>
        /// implementations should assign {@code doc} appropriately, and do any
        /// other work necessary to implement {@code score()} and {@code freq()}
        /// </summary>
        // TODO: make this less horrible
        protected abstract void AfterNext();
    }
}