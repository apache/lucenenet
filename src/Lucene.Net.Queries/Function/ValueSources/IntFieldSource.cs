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
using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Obtains int field values from <seealso cref="FieldCache#getInts"/> and makes those
    /// values available as other numeric types, casting as needed.
    /// </summary>
    public class IntFieldSource : FieldCacheSource
    {
        internal readonly FieldCache.IIntParser parser;

        public IntFieldSource(string field)
            : this(field, null)
        {
        }

        public IntFieldSource(string field, FieldCache.IIntParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string Description
        {
            get { return "int(" + field + ')'; }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FieldCache.Ints arr = cache.GetInts(readerContext.AtomicReader, field, parser, true);
            IBits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);

            return new IntDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
        }

        private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
        {
            private readonly IntFieldSource outerInstance;

            private readonly FieldCache.Ints arr;
            private readonly IBits valid;

            public IntDocValuesAnonymousInnerClassHelper(IntFieldSource outerInstance, IntFieldSource @this, FieldCache.Ints arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
                val = new MutableValueInt();
            }

            private readonly MutableValueInt val;

            public override float FloatVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            public override int IntVal(int doc)
            {
                return arr.Get(doc);
            }

            public override long LongVal(int doc)
            {
                return (long)arr.Get(doc);
            }

            public override double DoubleVal(int doc)
            {
                return (double)arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                return Convert.ToString(arr.Get(doc));
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? arr.Get(doc) : (int?)null;
            }

            public override bool Exists(int doc)
            {
                return arr.Get(doc) != 0 || valid.Get(doc);
            }

            public override string ToString(int doc)
            {
                return outerInstance.Description + '=' + IntVal(doc);
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
                    mval.Value = outerInstance.arr.Get(doc);
                    mval.Exists = mval.Value != 0 || outerInstance.valid.Get(doc);
                }
            }
        }

        public override bool Equals(object o)
        {
            var other = o as IntFieldSource;
            if (other == null)
                return false;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(int?).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}