using Lucene.Net.Index;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// A second pass grouping collector that keeps track of distinct values for a specified field for the top N group.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="GC"></typeparam>
    public abstract class AbstractDistinctValuesCollector<GC> : IAbstractDistinctValuesCollector<GC>
        where GC : AbstractDistinctValuesCollector.IGroupCount<object>
    {
        /// <summary>
        /// Returns all unique values for each top N group.
        /// </summary>
        /// <returns>all unique values for each top N group</returns>
        public abstract IEnumerable<GC> Groups { get; }

        public virtual bool AcceptsDocsOutOfOrder => true;

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
    }

    /// <summary>
    /// LUCENENET specific class used to nest the <see cref="GroupCount{TGroupValue}"/>
    /// class so it has similar syntax to that in Java Lucene
    /// (AbstractDistinctValuesCollector.GroupCount{TGroupValue} rather than 
    /// AbstractDistinctValuesCollector{GC}.GroupCount{TGroupValue}).
    /// </summary>
    public static class AbstractDistinctValuesCollector // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Returned by <see cref="AbstractDistinctValuesCollector{GC}.Groups"/>,
        /// representing the value and set of distinct values for the group.
        /// </summary>
        /// <typeparam name="TGroupValue"></typeparam>
        /// <remarks>
        /// LUCENENET - removed this class from being a nested class of 
        /// <see cref="AbstractDistinctValuesCollector{GC}"/> and renamed
        /// from GroupCount to AbstractGroupCount
        /// </remarks>
        public abstract class GroupCount<TGroupValue> : IGroupCount<TGroupValue>
        {
            public TGroupValue GroupValue { get; protected set; }
            public IEnumerable<TGroupValue> UniqueValues { get; protected set; }

            protected GroupCount(TGroupValue groupValue) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.GroupValue = groupValue;
                this.UniqueValues = new JCG.HashSet<TGroupValue>();
            }
        }

        /// <summary>
        /// LUCENENET specific interface used to apply covariance to TGroupValue
        /// </summary>
        /// <typeparam name="TGroupValue"></typeparam>
        public interface IGroupCount<out TGroupValue>
        {
            TGroupValue GroupValue { get; }
            IEnumerable<TGroupValue> UniqueValues { get; }
        }
    }

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to GC
    /// </summary>
    /// <typeparam name="GC"></typeparam>
    public interface IAbstractDistinctValuesCollector<out GC> : ICollector
    {
        /// <summary>
        /// Returns all unique values for each top N group.
        /// </summary>
        /// <returns>all unique values for each top N group</returns>
        IEnumerable<GC> Groups { get; }
    }
}
