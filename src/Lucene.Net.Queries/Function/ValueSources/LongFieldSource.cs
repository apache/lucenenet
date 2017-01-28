using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
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
    /// Obtains <see cref="long"/> field values from <see cref="IFieldCache.GetLongs"/> and makes those
    /// values available as other numeric types, casting as needed.
    /// </summary>
    public class LongFieldSource : FieldCacheSource
    {
        protected readonly FieldCache.ILongParser m_parser;

        public LongFieldSource(string field)
            : this(field, null)
        {
        }

        public LongFieldSource(string field, FieldCache.ILongParser parser)
            : base(field)
        {
            this.m_parser = parser;
        }

        public override string GetDescription()
        {
            return "long(" + m_field + ')';
        }

        public virtual long ExternalToLong(string extVal)
        {
            return Convert.ToInt64(extVal);
        }

        public virtual object LongToObject(long val)
        {
            return val;
        }

        public virtual string LongToString(long val)
        {
            return LongToObject(val).ToString();
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = m_cache.GetLongs(readerContext.AtomicReader, m_field, m_parser, true);
            var valid = m_cache.GetDocsWithField(readerContext.AtomicReader, m_field);
            return new LongDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
        }

        private class LongDocValuesAnonymousInnerClassHelper : LongDocValues
        {
            private readonly LongFieldSource outerInstance;

            private readonly FieldCache.Longs arr;
            private readonly IBits valid;

            public LongDocValuesAnonymousInnerClassHelper(LongFieldSource outerInstance, LongFieldSource @this, FieldCache.Longs arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
            }

            public override long LongVal(int doc)
            {
                return arr.Get(doc);
            }

            public override bool Exists(int doc)
            {
                return arr.Get(doc) != 0 || valid.Get(doc);
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? outerInstance.LongToObject(arr.Get(doc)) : null;
            }

            public override string StrVal(int doc)
            {
                return valid.Get(doc) ? outerInstance.LongToString(arr.Get(doc)) : null;
            }

            protected override long ExternalToLong(string extVal)
            {
                return outerInstance.ExternalToLong(extVal);
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFillerAnonymousInnerClassHelper(this);
            }

            private class ValueFillerAnonymousInnerClassHelper : ValueFiller
            {
                private readonly LongDocValuesAnonymousInnerClassHelper outerInstance;

                public ValueFillerAnonymousInnerClassHelper(LongDocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                    mval = outerInstance.outerInstance.NewMutableValueLong();
                }

                private readonly MutableValueLong mval;

                public override MutableValue Value
                {
                    get
                    {
                        return mval;
                    }
                }

                public override void FillValue(int doc)
                {
                    mval.Value = outerInstance.arr.Get(doc);
                    mval.Exists = mval.Value != 0 || outerInstance.valid.Get(doc);
                }
            }
        }

        protected virtual MutableValueLong NewMutableValueLong()
        {
            return new MutableValueLong();
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != this.GetType())
            {
                return false;
            }
            var other = o as LongFieldSource;
            if (other == null)
                return false;
            return base.Equals(other) && (this.m_parser == null ? other.m_parser == null : this.m_parser.GetType() == other.m_parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = m_parser == null ? this.GetType().GetHashCode() : m_parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}