// Lucene version compatibility level 4.8.1
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
    /// Obtains <see cref="int"/> field values from <see cref="IFieldCache.GetInt32s(AtomicReader, string, FieldCache.IInt32Parser, bool)"/> and makes those
    /// values available as other numeric types, casting as needed.
    /// <para/>
    /// NOTE: This was IntFieldSource in Lucene
    /// </summary>
    public class Int32FieldSource : FieldCacheSource
    {
        private readonly FieldCache.IInt32Parser parser;

        public Int32FieldSource(string field)
            : this(field, null)
        {
        }

        public Int32FieldSource(string field, FieldCache.IInt32Parser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string GetDescription()
        {
            return "int(" + m_field + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FieldCache.Int32s arr = m_cache.GetInt32s(readerContext.AtomicReader, m_field, parser, true);
            IBits valid = m_cache.GetDocsWithField(readerContext.AtomicReader, m_field);

            return new Int32DocValuesAnonymousClass(this, this, arr, valid);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly Int32FieldSource outerInstance;

            private readonly FieldCache.Int32s arr;
            private readonly IBits valid;

            public Int32DocValuesAnonymousClass(Int32FieldSource outerInstance, Int32FieldSource @this, FieldCache.Int32s arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
                //val = new MutableValueInt32(); // LUCENENET: Never read
            }

            //private readonly MutableValueInt32 val; // LUCENENET: Never read

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return (long)arr.Get(doc);
            }

            public override double DoubleVal(int doc)
            {
                return (double)arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                return Convert.ToString(arr.Get(doc), CultureInfo.InvariantCulture);
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? J2N.Numerics.Int32.GetInstance(arr.Get(doc)) : null; // LUCENENET: In Java, the conversion to instance of java.util.Integer is implicit, but we need to do an explicit conversion
            }

            public override bool Exists(int doc)
            {
                return arr.Get(doc) != 0 || valid.Get(doc);
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + '=' + Int32Val(doc);
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFiller.AnonymousValueFiller<MutableValueInt32>(new MutableValueInt32(), fillValue: (doc, mutableValue) =>
                {
                    mutableValue.Value = arr.Get(doc);
                    mutableValue.Exists = mutableValue.Value != 0 || valid.Get(doc);
                });
            }
        }

        public override bool Equals(object o)
        {
            if (!(o is Int32FieldSource other))
                return false;
            return base.Equals(other) && (this.parser is null ? other.parser is null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser is null ? typeof(int).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}