using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

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
    /// A base class for all collectors that return a <see cref="TopDocs"/> output. This
    /// collector allows easy extension by providing a single constructor which
    /// accepts a <see cref="Util.PriorityQueue{T}"/> as well as protected members for that
    /// priority queue and a counter of the number of total hits.
    /// <para/>
    /// Extending classes can override any of the methods to provide their own
    /// implementation, as well as avoid the use of the priority queue entirely by
    /// passing null to <see cref="TopDocsCollector(Util.PriorityQueue{T})"/>. In that case
    /// however, you might want to consider overriding all methods, in order to avoid
    /// a <see cref="NullReferenceException"/>.
    /// </summary>
    public abstract class TopDocsCollector<T> : ICollector, ITopDocsCollector where T : ScoreDoc
    {
        /// <summary>
        /// This is used in case <see cref="GetTopDocs()"/> is called with illegal parameters, or there
        /// simply aren't (enough) results.
        /// </summary>
        protected static readonly TopDocs EMPTY_TOPDOCS = new TopDocs(0, Arrays.Empty<ScoreDoc>(), float.NaN);

        /// <summary>
        /// The priority queue which holds the top documents. Note that different
        /// implementations of <see cref="PriorityQueue{T}"/> give different meaning to 'top documents'.
        /// <see cref="HitQueue"/> for example aggregates the top scoring documents, while other priority queue
        /// implementations may hold documents sorted by other criteria.
        /// </summary>
        protected PriorityQueue<T> m_pq;

        /// <summary>
        /// The total number of documents that the collector encountered. </summary>
        protected int m_totalHits;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        protected TopDocsCollector(PriorityQueue<T> pq)
        {
            this.m_pq = pq;
        }

        /// <summary>
        /// Populates the results array with the <see cref="ScoreDoc"/> instances. This can be
        /// overridden in case a different <see cref="ScoreDoc"/> type should be returned.
        /// </summary>
        protected virtual void PopulateResults(ScoreDoc[] results, int howMany)
        {
            for (int i = howMany - 1; i >= 0; i--)
            {
                results[i] = m_pq.Pop();
            }
        }

        /// <summary>
        /// Returns a <see cref="TopDocs"/> instance containing the given results. If
        /// <paramref name="results"/> is <c>null</c> it means there are no results to return,
        /// either because there were 0 calls to <see cref="Collect(int)"/> or because the arguments to
        /// <see cref="TopDocs"/> were invalid.
        /// </summary>
        protected virtual TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            return results is null ? EMPTY_TOPDOCS : new TopDocs(m_totalHits, results);
        }

        /// <summary>
        /// The total number of documents that matched this query. </summary>
        public virtual int TotalHits
        {
            get => m_totalHits;
            internal set => m_totalHits = value;
        }

        /// <summary>
        /// The number of valid priority queue entries 
        /// </summary>
        protected virtual int TopDocsCount =>
            // In case pq was populated with sentinel values, there might be less
            // results than pq.size(). Therefore return all results until either
            // pq.size() or totalHits.
            m_totalHits < m_pq.Count ? m_totalHits : m_pq.Count;

        /// <summary>
        /// Returns the top docs that were collected by this collector. </summary>
        public virtual TopDocs GetTopDocs()
        {
            // In case pq was populated with sentinel values, there might be less
            // results than pq.size(). Therefore return all results until either
            // pq.size() or totalHits.
            return GetTopDocs(0, TopDocsCount);
        }

        /// <summary>
        /// Returns the documents in the rage [<paramref name="start"/> .. pq.Count) that were collected
        /// by this collector. Note that if <paramref name="start"/> &gt;= pq.Count, an empty <see cref="TopDocs"/> is
        /// returned.
        /// <para/>
        /// This method is convenient to call if the application always asks for the
        /// last results, starting from the last 'page'.
        /// <para/>
        /// <b>NOTE:</b> you cannot call this method more than once for each search
        /// execution. If you need to call it more than once, passing each time a
        /// different <paramref name="start"/>, you should call <see cref="GetTopDocs()"/> and work
        /// with the returned <see cref="TopDocs"/> object, which will contain all the
        /// results this search execution collected.
        /// </summary>
        public virtual TopDocs GetTopDocs(int start)
        {
            // In case pq was populated with sentinel values, there might be less
            // results than pq.Count. Therefore return all results until either
            // pq.Count or totalHits.
            return GetTopDocs(start, TopDocsCount);
        }

        /// <summary>
        /// Returns the documents in the rage [<paramref name="start"/> .. <paramref name="start"/>+<paramref name="howMany"/>) that were
        /// collected by this collector. Note that if <paramref name="start"/> >= pq.Count, an empty
        /// <see cref="TopDocs"/> is returned, and if pq.Count - <paramref name="start"/> &lt; <paramref name="howMany"/>, then only the
        /// available documents in [<paramref name="start"/> .. pq.Count) are returned.
        /// <para/>
        /// This method is useful to call in case pagination of search results is
        /// allowed by the search application, as well as it attempts to optimize the
        /// memory used by allocating only as much as requested by <paramref name="howMany"/>.
        /// <para/>
        /// <b>NOTE:</b> you cannot call this method more than once for each search
        /// execution. If you need to call it more than once, passing each time a
        /// different range, you should call <see cref="GetTopDocs()"/> and work with the
        /// returned <see cref="TopDocs"/> object, which will contain all the results this
        /// search execution collected.
        /// </summary>
        public virtual TopDocs GetTopDocs(int start, int howMany)
        {
            // In case pq was populated with sentinel values, there might be less
            // results than pq.Count. Therefore return all results until either
            // pq.Count or totalHits.
            int size = TopDocsCount;

            // Don't bother to throw an exception, just return an empty TopDocs in case
            // the parameters are invalid or out of range.
            // TODO: shouldn't we throw IAE if apps give bad params here so they dont
            // have sneaky silent bugs?
            if (start < 0 || start >= size || howMany <= 0)
            {
                return NewTopDocs(null, start);
            }

            // We know that start < pqsize, so just fix howMany.
            howMany = Math.Min(size - start, howMany);
            ScoreDoc[] results = new ScoreDoc[howMany];

            // pq's pop() returns the 'least' element in the queue, therefore need
            // to discard the first ones, until we reach the requested range.
            // Note that this loop will usually not be executed, since the common usage
            // should be that the caller asks for the last howMany results. However it's
            // needed here for completeness.
            for (int i = m_pq.Count - start - howMany; i > 0; i--)
            {
                m_pq.Pop();
            }

            // Get the requested results from pq.
            PopulateResults(results, howMany);

            return NewTopDocs(results, start);
        }

        // LUCENENET specific - we need to implement these here, since our abstract base class
        // is now an interface.
        /// <summary>
        /// Called before successive calls to <see cref="Collect(int)"/>. Implementations
        /// that need the score of the current document (passed-in to
        /// <see cref="Collect(int)"/>), should save the passed-in <see cref="Scorer"/> and call
        /// <see cref="Scorer.GetScore()"/> when needed.
        /// </summary>
        public abstract void SetScorer(Scorer scorer);

        /// <summary>
        /// Called once for every document matching a query, with the unbased document
        /// number.
        /// <para/>Note: The collection of the current segment can be terminated by throwing
        /// a <see cref="CollectionTerminatedException"/>. In this case, the last docs of the
        /// current <see cref="AtomicReaderContext"/> will be skipped and <see cref="IndexSearcher"/>
        /// will swallow the exception and continue collection with the next leaf.
        /// <para/>
        /// Note: this is called in an inner search loop. For good search performance,
        /// implementations of this method should not call <see cref="IndexSearcher.Doc(int)"/> or
        /// <see cref="Lucene.Net.Index.IndexReader.Document(int)"/> on every hit.
        /// Doing so can slow searches by an order of magnitude or more.
        /// </summary>
        public abstract void Collect(int doc);

        /// <summary>
        /// Called before collecting from each <see cref="AtomicReaderContext"/>. All doc ids in
        /// <see cref="Collect(int)"/> will correspond to <see cref="Index.IndexReaderContext.Reader"/>.
        /// <para/>
        /// Add <see cref="AtomicReaderContext.DocBase"/> to the current <see cref="Index.IndexReaderContext.Reader"/>'s
        /// internal document id to re-base ids in <see cref="Collect(int)"/>.
        /// </summary>
        /// <param name="context">Next atomic reader context </param>
        public abstract void SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Return <c>true</c> if this collector does not
        /// require the matching docIDs to be delivered in int sort
        /// order (smallest to largest) to <see cref="Collect"/>.
        ///
        /// <para> Most Lucene Query implementations will visit
        /// matching docIDs in order.  However, some queries
        /// (currently limited to certain cases of <see cref="BooleanQuery"/>) 
        /// can achieve faster searching if the
        /// <see cref="ICollector"/> allows them to deliver the
        /// docIDs out of order.</para>
        ///
        /// <para> Many collectors don't mind getting docIDs out of
        /// order, so it's important to return <c>true</c>
        /// here.</para>
        /// </summary>
        public abstract bool AcceptsDocsOutOfOrder { get; }
    }

    /// <summary>
    /// LUCENENET specific interface used to reference <see cref="TopDocsCollector{T}"/>
    /// without referencing its generic type.
    /// </summary>
    public interface ITopDocsCollector : ICollector
    {
        // From TopDocsCollector<T>
        /// <summary>
        /// The total number of documents that matched this query. </summary>
        int TotalHits { get; }

        /// <summary>
        /// Returns the top docs that were collected by this collector. </summary>
        TopDocs GetTopDocs();

        /// <summary>
        /// Returns the documents in the rage [<paramref name="start"/> .. pq.Count) that were collected
        /// by this collector. Note that if <paramref name="start"/> &gt;= pq.Count, an empty <see cref="TopDocs"/> is
        /// returned.
        /// <para/>
        /// This method is convenient to call if the application always asks for the
        /// last results, starting from the last 'page'.
        /// <para/>
        /// <b>NOTE:</b> you cannot call this method more than once for each search
        /// execution. If you need to call it more than once, passing each time a
        /// different <paramref name="start"/>, you should call <see cref="GetTopDocs()"/> and work
        /// with the returned <see cref="TopDocs"/> object, which will contain all the
        /// results this search execution collected.
        /// </summary>
        TopDocs GetTopDocs(int start);

        /// <summary>
        /// Returns the documents in the rage [<paramref name="start"/> .. <paramref name="start"/>+<paramref name="howMany"/>) that were
        /// collected by this collector. Note that if <paramref name="start"/> >= pq.Count, an empty
        /// <see cref="TopDocs"/> is returned, and if pq.Count - <paramref name="start"/> &lt; <paramref name="howMany"/>, then only the
        /// available documents in [<paramref name="start"/> .. pq.Count) are returned.
        /// <para/>
        /// This method is useful to call in case pagination of search results is
        /// allowed by the search application, as well as it attempts to optimize the
        /// memory used by allocating only as much as requested by <paramref name="howMany"/>.
        /// <para/>
        /// <b>NOTE:</b> you cannot call this method more than once for each search
        /// execution. If you need to call it more than once, passing each time a
        /// different range, you should call <see cref="GetTopDocs()"/> and work with the
        /// returned <see cref="TopDocs"/> object, which will contain all the results this
        /// search execution collected.
        /// </summary>
        TopDocs GetTopDocs(int start, int howMany);
    }
}