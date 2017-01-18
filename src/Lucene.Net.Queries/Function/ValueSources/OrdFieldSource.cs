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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Obtains the ordinal of the field value from the default Lucene <seealso cref="FieldCache"/> using getStringIndex().
    /// <br>
    /// The native lucene index order is used to assign an ordinal value for each field value.
    /// <br>Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
    /// <br>
    /// Example:<br>
    ///  If there were only three field values: "apple","banana","pear"
    /// <br>then ord("apple")=1, ord("banana")=2, ord("pear")=3
    /// <para>
    /// WARNING: ord() depends on the position in an index and can thus change when other documents are inserted or deleted,
    ///  or if a MultiSearcher is used.
    /// <br>WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
    /// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
    /// or using a different function query, in addition to ord()/rord() will double memory use.
    /// 
    /// </para>
    /// </summary>

    public class OrdFieldSource : ValueSource
    {
        protected readonly string field;

        public OrdFieldSource(string field)
        {
            this.field = field;
        }

        public override string GetDescription()
        {
            return "ord(" + field + ')';
        }


        // TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            int off = readerContext.DocBase;
            IndexReader topReader = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            AtomicReader r = SlowCompositeReaderWrapper.Wrap(topReader);
            SortedDocValues sindex = FieldCache.DEFAULT.GetTermsIndex(r, field);
            return new IntDocValuesAnonymousInnerClassHelper(this, this, off, sindex);
        }

        private sealed class IntDocValuesAnonymousInnerClassHelper : IntDocValues
        {
            private readonly OrdFieldSource outerInstance;

            private readonly int off;
            private readonly SortedDocValues sindex;

            public IntDocValuesAnonymousInnerClassHelper(OrdFieldSource outerInstance, OrdFieldSource @this, int off, SortedDocValues sindex)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.off = off;
                this.sindex = sindex;
            }

            private string ToTerm(string readableValue)
            {
                return readableValue;
            }
            public override int IntVal(int doc)
            {
                return sindex.GetOrd(doc + off);
            }
            public override int OrdVal(int doc)
            {
                return sindex.GetOrd(doc + off);
            }
            public override int NumOrd()
            {
                return sindex.ValueCount;
            }

            public override bool Exists(int doc)
            {
                return sindex.GetOrd(doc + off) != 0;
            }

            public override AbstractValueFiller ValueFiller
            {
                get
                {
                    return new ValueFillerAnonymousInnerClassHelper(this);
                }
            }

            private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
            {
                private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

                public ValueFillerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                    mval = new MutableValueInt();
                }

                private readonly MutableValueInt mval;

                public override MutableValue Value
                {
                    get
                    {
                        return mval;
                    }
                }

                public override void FillValue(int doc)
                {
                    mval.Value = outerInstance.sindex.GetOrd(doc);
                    mval.Exists = mval.Value != 0;
                }
            }
        }

        public override bool Equals(object o)
        {
            return o != null && o.GetType() == typeof(OrdFieldSource) && this.field.Equals(((OrdFieldSource)o).field);
        }

        private static readonly int hcode = typeof(OrdFieldSource).GetHashCode();

        public override int GetHashCode()
        {
            return hcode + field.GetHashCode();
        }
    }
}