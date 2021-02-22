// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util.Mutable;
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
    /// Obtains the ordinal of the field value from the default Lucene <see cref="FieldCache"/> using StringIndex.
    /// <para/>
    /// The native lucene index order is used to assign an ordinal value for each field value.
    /// <para/>
    /// Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
    /// <para/>
    /// Example:
    /// <code>
    ///     If there were only three field values: "apple","banana","pear"
    ///     then ord("apple")=1, ord("banana")=2, ord("pear")=3
    /// </code>
    /// <para/>
    /// WARNING: Ord depends on the position in an index and can thus change when other documents are inserted or deleted,
    /// or if a MultiSearcher is used.
    /// <para/>
    /// WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
    /// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
    /// or using a different function query, in addition to ord()/rord() will double memory use.
    /// </summary>
    public class OrdFieldSource : ValueSource
    {
        protected readonly string m_field;

        public OrdFieldSource(string field)
        {
            this.m_field = field;
        }

        public override string GetDescription()
        {
            return "ord(" + m_field + ')';
        }


        // TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            int off = readerContext.DocBase;
            IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(topReader);
            SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, m_field);
            return new Int32DocValuesAnonymousClass(this, off, sindex);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly int off;
            private readonly SortedDocValues sindex;

            public Int32DocValuesAnonymousClass(OrdFieldSource @this, int off, SortedDocValues sindex)
                : base(@this)
            {
                this.off = off;
                this.sindex = sindex;
            }

            //private string ToTerm(string readableValue) // LUCENENET: IDE0051: Remove unused private member
            //{
            //    return readableValue;
            //}

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return sindex.GetOrd(doc + off);
            }
            public override int OrdVal(int doc)
            {
                return sindex.GetOrd(doc + off);
            }
            public override int NumOrd => sindex.ValueCount;

            public override bool Exists(int doc)
            {
                return sindex.GetOrd(doc + off) != 0;
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFiller.AnonymousValueFiller<MutableValueInt32>(new MutableValueInt32(), fillValue: (doc, mutableValue) =>
                {
                    mutableValue.Value = sindex.GetOrd(doc);
                    mutableValue.Exists = mutableValue.Value != 0;
                });
            }
        }

        public override bool Equals(object o)
        {
            return o != null && o.GetType() == typeof(OrdFieldSource) && this.m_field.Equals(((OrdFieldSource)o).m_field, StringComparison.Ordinal);
        }

        private static readonly int hcode = typeof(OrdFieldSource).GetHashCode();

        public override int GetHashCode()
        {
            return hcode + m_field.GetHashCode();
        }
    }
}