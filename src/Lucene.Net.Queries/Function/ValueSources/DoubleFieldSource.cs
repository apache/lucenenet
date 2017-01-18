using System.Collections;
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
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Obtains double field values from <seealso cref="IFieldCache#getDoubles"/> and makes
    /// those values available as other numeric types, casting as needed.
    /// </summary>
    public class DoubleFieldSource : FieldCacheSource
    {

        protected internal readonly FieldCache.IDoubleParser parser;

        public DoubleFieldSource(string field)
            : this(field, null)
        {
        }

        public DoubleFieldSource(string field, FieldCache.IDoubleParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string GetDescription()
        {
            return "double(" + field + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = cache.GetDoubles(readerContext.AtomicReader, field, parser, true);
            var valid = cache.GetDocsWithField(readerContext.AtomicReader, field);
            return new DoubleDocValuesAnonymousInnerClassHelper(this, arr, valid);

        }

        private class DoubleDocValuesAnonymousInnerClassHelper : DoubleDocValues
        {
            private readonly FieldCache.Doubles arr;
            private readonly IBits valid;

            public DoubleDocValuesAnonymousInnerClassHelper(DoubleFieldSource @this, FieldCache.Doubles arr, IBits valid)
                : base(@this)
            {
                this.arr = arr;
                this.valid = valid;
            }

            public override double DoubleVal(int doc)
            {
                return arr.Get(doc);
            }

            public override bool Exists(int doc)
            {
                return arr.Get(doc) != 0 || valid.Get(doc);
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
                private readonly DoubleDocValuesAnonymousInnerClassHelper outerInstance;

                public ValueFillerAnonymousInnerClassHelper(DoubleDocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                    mval = new MutableValueDouble();
                }

                private readonly MutableValueDouble mval;

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
            var other = o as
                DoubleFieldSource;
            if (other == null)
            {
                return false;
            }
            return base.Equals(other) &&
                   (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(double?).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}