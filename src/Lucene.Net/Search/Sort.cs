using Lucene.Net.Support;
using System.IO;
using System.Text;

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
    /// Encapsulates sort criteria for returned hits.
    ///
    /// <para/>The fields used to determine sort order must be carefully chosen.
    /// <see cref="Documents.Document"/>s must contain a single term in such a field,
    /// and the value of the term should indicate the document's relative position in
    /// a given sort order.  The field must be indexed, but should not be tokenized,
    /// and does not need to be stored (unless you happen to want it back with the
    /// rest of your document data).  In other words:
    ///
    /// <para/><code>document.Add(new Field("byNumber", x.ToString(CultureInfo.InvariantCulture), Field.Store.NO, Field.Index.NOT_ANALYZED));</code>
    ///
    ///
    /// <para/><h3>Valid Types of Values</h3>
    ///
    /// <para/>There are four possible kinds of term values which may be put into
    /// sorting fields: <see cref="int"/>s, <see cref="long"/>s, <see cref="float"/>s, or <see cref="string"/>s.  Unless
    /// <see cref="SortField"/> objects are specified, the type of value
    /// in the field is determined by parsing the first term in the field.
    ///
    /// <para/><see cref="int"/> term values should contain only digits and an optional
    /// preceding negative sign.  Values must be base 10 and in the range
    /// <see cref="int.MinValue"/> and <see cref="int.MaxValue"/> inclusive.
    /// Documents which should appear first in the sort
    /// should have low value integers, later documents high values
    /// (i.e. the documents should be numbered <c>1..n</c> where
    /// <c>1</c> is the first and <c>n</c> the last).
    ///
    /// <para/><see cref="long"/> term values should contain only digits and an optional
    /// preceding negative sign.  Values must be base 10 and in the range
    /// <see cref="long.MinValue"/> and <see cref="long.MaxValue"/> inclusive.
    /// Documents which should appear first in the sort
    /// should have low value integers, later documents high values.
    ///
    /// <para/><see cref="float"/> term values should conform to values accepted by
    /// <see cref="float"/> (except that <c>NaN</c>
    /// and <c>Infinity</c> are not supported).
    /// <see cref="Documents.Document"/>s which should appear first in the sort
    /// should have low values, later documents high values.
    ///
    /// <para/><see cref="string"/> term values can contain any valid <see cref="string"/>, but should
    /// not be tokenized.  The values are sorted according to their
    /// comparable natural order (<see cref="System.StringComparer.Ordinal"/>).  Note that using this type
    /// of term value has higher memory requirements than the other
    /// two types.
    ///
    /// <para/><h3>Object Reuse</h3>
    ///
    /// <para/>One of these objects can be
    /// used multiple times and the sort order changed between usages.
    ///
    /// <para/>This class is thread safe.
    ///
    /// <para/><h3>Memory Usage</h3>
    ///
    /// <para/>Sorting uses of caches of term values maintained by the
    /// internal HitQueue(s).  The cache is static and contains an <see cref="int"/>
    /// or <see cref="float"/> array of length <c>IndexReader.MaxDoc</c> for each field
    /// name for which a sort is performed.  In other words, the size of the
    /// cache in bytes is:
    ///
    /// <para/><code>4 * IndexReader.MaxDoc * (# of different fields actually used to sort)</code>
    ///
    /// <para/>For <see cref="string"/> fields, the cache is larger: in addition to the
    /// above array, the value of every term in the field is kept in memory.
    /// If there are many unique terms in the field, this could
    /// be quite large.
    ///
    /// <para/>Note that the size of the cache is not affected by how many
    /// fields are in the index and <i>might</i> be used to sort - only by
    /// the ones actually used to sort a result set.
    ///
    /// <para/>Created: Feb 12, 2004 10:53:57 AM
    /// <para/>
    /// @since   lucene 1.4
    /// </summary>
    public class Sort
    {
        /// <summary>
        /// Represents sorting by computed relevance. Using this sort criteria returns
        /// the same results as calling
        /// <see cref="IndexSearcher.Search(Query, int)"/>without a sort criteria,
        /// only with slightly more overhead.
        /// </summary>
        public static readonly Sort RELEVANCE = new Sort();

        /// <summary>
        /// Represents sorting by index order. </summary>
        public static readonly Sort INDEXORDER = new Sort(SortField.FIELD_DOC);

        // internal representation of the sort criteria
        internal SortField[] fields;

        /// <summary>
        /// Sorts by computed relevance. This is the same sort criteria as calling
        /// <see cref="IndexSearcher.Search(Query, int)"/> without a sort criteria,
        /// only with slightly more overhead.
        /// </summary>
        public Sort()
            : this(SortField.FIELD_SCORE)
        {
        }

        /// <summary>
        /// Sorts by the criteria in the given <see cref="SortField"/>. </summary>
        public Sort(SortField field)
        {
            SetSortInternal(field); // LUCENENET specific - calling private instead of virtual method
        }

        /// <summary>
        /// Sorts in succession by the criteria in each <see cref="SortField"/>.
        /// </summary>
        public Sort(params SortField[] fields)
        {
            SetSortInternal(fields); // LUCENENET specific - calling private instead of virtual method
        }

        /// <summary>
        /// Sets the sort to the given criteria.
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void SetSort(SortField field) => SetSortInternal(field);

        /// <summary>
        /// Sets the sort to the given criteria in succession.
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void SetSort(params SortField[] fields) => SetSortInternal(fields);

        private void SetSortInternal(params SortField[] fields) => this.fields = fields;

        /// <summary> Representation of the sort criteria.</summary>
        /// <returns> Array of <see cref="SortField"/> objects used in this sort criteria
        /// </returns>
        [WritableArray]
        public virtual SortField[] GetSort()
        {
            return fields;
        }

        /// <summary>
        /// Rewrites the <see cref="SortField"/>s in this <see cref="Sort"/>, returning a new <see cref="Sort"/> if any of the fields
        /// changes during their rewriting.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="searcher"> <see cref="IndexSearcher"/> to use in the rewriting </param>
        /// <returns> <c>this</c> if the Sort/Fields have not changed, or a new <see cref="Sort"/> if there
        ///        is a change </returns>
        /// <exception cref="IOException"> Can be thrown by the rewriting</exception>
        public virtual Sort Rewrite(IndexSearcher searcher)
        {
            bool changed = false;

            SortField[] rewrittenSortFields = new SortField[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                rewrittenSortFields[i] = fields[i].Rewrite(searcher);
                if (fields[i] != rewrittenSortFields[i])
                {
                    changed = true;
                }
            }

            return (changed) ? new Sort(rewrittenSortFields) : this;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < fields.Length; i++)
            {
                buffer.Append(fields[i].ToString());
                if ((i + 1) < fields.Length)
                {
                    buffer.Append(',');
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is Sort))
            {
                return false;
            }
            Sort other = (Sort)o;
            return Arrays.Equals(this.fields, other.fields);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return 0x45aaf665 + Arrays.GetHashCode(fields);
        }

        /// <summary>
        /// Returns <c>true</c> if the relevance score is needed to sort documents. </summary>
        public virtual bool NeedsScores
        {
            get
            {
                foreach (SortField sortField in fields)
                {
                    if (sortField.NeedsScores)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}