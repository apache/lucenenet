using Lucene.Net.Support;
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
    /// <p>The fields used to determine sort order must be carefully chosen.
    /// Documents must contain a single term in such a field,
    /// and the value of the term should indicate the document's relative position in
    /// a given sort order.  The field must be indexed, but should not be tokenized,
    /// and does not need to be stored (unless you happen to want it back with the
    /// rest of your document data).  In other words:
    ///
    /// <p><code>document.add (new Field ("byNumber", Integer.toString(x), Field.Store.NO, Field.Index.NOT_ANALYZED));</code></p>
    ///
    ///
    /// <p><h3>Valid Types of Values</h3>
    ///
    /// <p>There are four possible kinds of term values which may be put into
    /// sorting fields: Integers, Longs, Floats, or Strings.  Unless
    /// <seealso cref="SortField SortField"/> objects are specified, the type of value
    /// in the field is determined by parsing the first term in the field.
    ///
    /// <p>Integer term values should contain only digits and an optional
    /// preceding negative sign.  Values must be base 10 and in the range
    /// <code>Integer.MIN_VALUE</code> and <code>Integer.MAX_VALUE</code> inclusive.
    /// Documents which should appear first in the sort
    /// should have low value integers, later documents high values
    /// (i.e. the documents should be numbered <code>1..n</code> where
    /// <code>1</code> is the first and <code>n</code> the last).
    ///
    /// <p>Long term values should contain only digits and an optional
    /// preceding negative sign.  Values must be base 10 and in the range
    /// <code>Long.MIN_VALUE</code> and <code>Long.MAX_VALUE</code> inclusive.
    /// Documents which should appear first in the sort
    /// should have low value integers, later documents high values.
    ///
    /// <p>Float term values should conform to values accepted by
    /// <seealso cref="Float Float.valueOf(String)"/> (except that <code>NaN</code>
    /// and <code>Infinity</code> are not supported).
    /// Documents which should appear first in the sort
    /// should have low values, later documents high values.
    ///
    /// <p>String term values can contain any valid String, but should
    /// not be tokenized.  The values are sorted according to their
    /// <seealso cref="Comparable natural order"/>.  Note that using this type
    /// of term value has higher memory requirements than the other
    /// two types.
    ///
    /// <p><h3>Object Reuse</h3>
    ///
    /// <p>One of these objects can be
    /// used multiple times and the sort order changed between usages.
    ///
    /// <p>this class is thread safe.
    ///
    /// <p><h3>Memory Usage</h3>
    ///
    /// <p>Sorting uses of caches of term values maintained by the
    /// internal HitQueue(s).  The cache is static and contains an integer
    /// or float array of length <code>IndexReader.maxDoc()</code> for each field
    /// name for which a sort is performed.  In other words, the size of the
    /// cache in bytes is:
    ///
    /// <p><code>4 * IndexReader.maxDoc() * (# of different fields actually used to sort)</code>
    ///
    /// <p>For String fields, the cache is larger: in addition to the
    /// above array, the value of every term in the field is kept in memory.
    /// If there are many unique terms in the field, this could
    /// be quite large.
    ///
    /// <p>Note that the size of the cache is not affected by how many
    /// fields are in the index and <i>might</i> be used to sort - only by
    /// the ones actually used to sort a result set.
    ///
    /// <p>Created: Feb 12, 2004 10:53:57 AM
    ///
    /// @since   lucene 1.4
    /// </summary>
    public class Sort
    {
        /// <summary>
        /// Represents sorting by computed relevance. Using this sort criteria returns
        /// the same results as calling
        /// <seealso cref="IndexSearcher#search(Query,int) IndexSearcher#search()"/>without a sort criteria,
        /// only with slightly more overhead.
        /// </summary>
        public static readonly Sort RELEVANCE = new Sort();

        /// <summary>
        /// Represents sorting by index order. </summary>
        public static readonly Sort INDEXORDER = new Sort(SortField.FIELD_DOC);

        // internal representation of the sort criteria
        internal SortField[] fields;

        /// <summary>
        /// Sorts by computed relevance. this is the same sort criteria as calling
        /// <seealso cref="IndexSearcher#search(Query,int) IndexSearcher#search()"/>without a sort criteria,
        /// only with slightly more overhead.
        /// </summary>
        public Sort()
            : this(SortField.FIELD_SCORE)
        {
        }

        /// <summary>
        /// Sorts by the criteria in the given SortField. </summary>
        public Sort(SortField field)
        {
            SetSort(field);
        }

        /// <summary>
        /// Sorts in succession by the criteria in each SortField. </summary>
        public Sort(params SortField[] fields)
        {
            SetSort(fields);
        }

        /// <summary>Sets the sort to the given criteria. </summary>
        public virtual void SetSort(SortField field)
        {
            this.fields = new SortField[] { field };
        }

        /// <summary>Sets the sort to the given criteria in succession. </summary>
        public virtual void SetSort(params SortField[] fields)
        {
            this.fields = fields;
        }

        /// <summary> Representation of the sort criteria.</summary>
        /// <returns> Array of SortField objects used in this sort criteria
        /// </returns>
        public virtual SortField[] GetSort()
        {
            return fields;
        }

        /// <summary>
        /// Rewrites the SortFields in this Sort, returning a new Sort if any of the fields
        /// changes during their rewriting.
        /// </summary>
        /// <param name="searcher"> IndexSearcher to use in the rewriting </param>
        /// <returns> {@code this} if the Sort/Fields have not changed, or a new Sort if there
        ///        is a change </returns>
        /// <exception cref="IOException"> Can be thrown by the rewriting
        /// @lucene.experimental </exception>
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
        /// Returns true if <code>o</code> is equal to this. </summary>
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
        /// Returns true if the relevance score is needed to sort documents. </summary>
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