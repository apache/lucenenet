// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Globalization;

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
    /// Obtains <see cref="long"/> field values from <see cref="IFieldCache.GetInt64s(AtomicReader, string, FieldCache.IInt64Parser, bool)"/> and makes those
    /// values available as other numeric types, casting as needed.
    /// <para/>
    /// NOTE: This was LongFieldSource in Lucene
    /// </summary>
    public class Int64FieldSource : FieldCacheSource
    {
        protected readonly FieldCache.IInt64Parser m_parser;

        public Int64FieldSource(string field)
            : this(field, null)
        {
        }

        public Int64FieldSource(string field, FieldCache.IInt64Parser parser)
            : base(field)
        {
            this.m_parser = parser;
        }

        public override string GetDescription()
        {
            return "long(" + m_field + ')';
        }

        /// <summary>
        /// NOTE: This was externalToLong() in Lucene
        /// </summary>
        public virtual long ExternalToInt64(string extVal)
        {
            return Convert.ToInt64(extVal, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// NOTE: This was longToObject() in Lucene
        /// </summary>
        public virtual object Int64ToObject(long val)
        {
            return J2N.Numerics.Int64.GetInstance(val); // LUCENENET: In Java, the conversion to instance of java.util.Long is implicit, but we need to do an explicit conversion
        }

        /// <summary>
        /// NOTE: This was longToString() in Lucene
        /// </summary>
        public virtual string Int64ToString(long val)
        {
            object obj = Int64ToObject(val);
            // LUCENENET: Optimized path for Number. We fall back to string.Format.
            if (obj is Number number)
                return number.ToString(NumberFormatInfo.InvariantInfo);
            return string.Format(StringFormatter.InvariantCulture, "{0}", obj);
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = m_cache.GetInt64s(readerContext.AtomicReader, m_field, m_parser, true);
            var valid = m_cache.GetDocsWithField(readerContext.AtomicReader, m_field);
            return new Int64DocValuesAnonymousClass(this, this, arr, valid);
        }

        private sealed class Int64DocValuesAnonymousClass : Int64DocValues
        {
            private readonly Int64FieldSource outerInstance;

            private readonly FieldCache.Int64s arr;
            private readonly IBits valid;

            public Int64DocValuesAnonymousClass(Int64FieldSource outerInstance, Int64FieldSource @this, FieldCache.Int64s arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return arr.Get(doc);
            }

            public override bool Exists(int doc)
            {
                return arr.Get(doc) != 0 || valid.Get(doc);
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? outerInstance.Int64ToObject(arr.Get(doc)) : null;
            }

            public override string StrVal(int doc)
            {
                return valid.Get(doc) ? outerInstance.Int64ToString(arr.Get(doc)) : null;
            }

            /// <summary>
            /// NOTE: This was externalToLong() in Lucene
            /// </summary>
            protected override long ExternalToInt64(string extVal)
            {
                return outerInstance.ExternalToInt64(extVal);
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFiller.AnonymousValueFiller<MutableValueInt64>(outerInstance.NewMutableValueInt64(), fillValue: (doc, mutableValue) =>
                {
                    mutableValue.Value = arr.Get(doc);
                    mutableValue.Exists = mutableValue.Value != 0 || valid.Get(doc);
                });
            }
        }

        /// <summary>
        /// NOTE: This was longToString() in Lucene
        /// </summary>
        protected virtual MutableValueInt64 NewMutableValueInt64()
        {
            return new MutableValueInt64();
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != this.GetType())
            {
                return false;
            }
            if (!(o is Int64FieldSource other))
                return false;
            return base.Equals(other) && (this.m_parser is null ? other.m_parser is null : this.m_parser.GetType() == other.m_parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = m_parser is null ? this.GetType().GetHashCode() : m_parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}