// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using System;
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Obtains the ordinal of the field value from the default Lucene <see cref="FieldCache"/> using <see cref="IFieldCache.GetTermsIndex(AtomicReader, string, float)"/>
    /// and reverses the order.
    /// <para/>
    /// The native lucene index order is used to assign an ordinal value for each field value.
    /// <para/>Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
    /// <para/>
    /// Example of reverse ordinal (rord):
    /// <code>
    ///     If there were only three field values: "apple","banana","pear"
    ///     then rord("apple")=3, rord("banana")=2, ord("pear")=1
    /// </code>
    /// <para/>
    ///  WARNING: Ord depends on the position in an index and can thus change when other documents are inserted or deleted,
    ///  or if a MultiSearcher is used.
    /// <para/>
    ///  WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
    /// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
    /// or using a different function query, in addition to ord()/rord() will double memory use.
    /// </summary>
    public class ReverseOrdFieldSource : ValueSource
    {
        // LUCENENET NOTE: Made private and added public property for reading
        private readonly string field;
        public string Field => field;

        public ReverseOrdFieldSource(string field)
        {
            this.field = field;
        }

        public override string GetDescription()
        {
            return "rord(" + field + ')';
        }

        // TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(topReader);
            int off = readerContext.DocBase;

            var sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
            var end = sindex.ValueCount;

            return new Int32DocValuesAnonymousClass(this, off, sindex, end);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly int off;
            private readonly SortedDocValues sindex;
            private readonly int end;

            public Int32DocValuesAnonymousClass(ReverseOrdFieldSource @this, int off, SortedDocValues sindex, int end)
                : base(@this)
            {
                this.off = off;
                this.sindex = sindex;
                this.end = end;
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return (end - sindex.GetOrd(doc + off) - 1);
            }
        }

        public override bool Equals(object o)
        {
            if (!(o is ReverseOrdFieldSource other))
                return false;
            return this.field.Equals(other.field, StringComparison.Ordinal);
        }

        private static readonly int hcode = typeof(ReverseOrdFieldSource).GetHashCode();
        public override int GetHashCode()
        {
            return hcode + field.GetHashCode();
        }
    }
}