using Lucene.Net.Index;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search.Grouping
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
    /// A collector that collects all groups that match the
    /// query. Only the group value is collected, and the order
    /// is undefined.  This collector does not determine
    /// the most relevant document of a group.
    /// 
    /// <para>
    /// This is an abstract version. Concrete implementations define
    /// what a group actually is and how it is internally collected.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public abstract class AbstractAllGroupsCollector<TGroupValue> : IAbstractAllGroupsCollector<TGroupValue>
    {
        /// <summary>
        /// Returns the total number of groups for the executed search.
        /// This is a convenience method. The following code snippet has the same effect: <code>GetGroups().Count</code>
        /// </summary>
        /// <returns>The total number of groups for the executed search</returns>
        public virtual int GroupCount => Groups.Count();

        /// <summary>
        /// Returns the group values
        /// <para>
        /// This is an unordered collections of group values. For each group that matched the query there is a <see cref="Util.BytesRef"/>
        /// representing a group value.
        /// </para>
        /// </summary>
        /// <returns>the group values</returns>
        public abstract IEnumerable<TGroupValue> Groups { get; }


        // Empty not necessary
        public virtual void SetScorer(Scorer scorer)
        {
        }

        // LUCENENET specific - we need to implement these here, since our abstract base class
        // is now an interface.

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
        ///
        /// Add <see cref="AtomicReaderContext.DocBase"/> to the current <see cref="Index.IndexReaderContext.Reader"/>'s
        /// internal document id to re-base ids in <see cref="Collect(int)"/>.
        /// </summary>
        /// <param name="context">next atomic reader context </param>
        public abstract void SetNextReader(AtomicReaderContext context);

        public virtual bool AcceptsDocsOutOfOrder => true;
    }

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to TGroupValue
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface IAbstractAllGroupsCollector<out TGroupValue> : ICollector
    {
        /// <summary>
        /// Returns the total number of groups for the executed search.
        /// This is a convenience method. The following code snippet has the same effect: <code>GetGroups().Count</code>
        /// </summary>
        /// <returns>The total number of groups for the executed search</returns>
        int GroupCount { get; }

        /// <summary>
        /// Returns the group values
        /// <para>
        /// This is an unordered collections of group values. For each group that matched the query there is a <see cref="Util.BytesRef"/>
        /// representing a group value.
        /// </para>
        /// </summary>
        /// <returns>the group values</returns>
        IEnumerable<TGroupValue> Groups { get; }
    }
}
