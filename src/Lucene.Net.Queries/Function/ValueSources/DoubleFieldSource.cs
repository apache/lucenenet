// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
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
    /// Obtains <see cref="double"/> field values from <see cref="IFieldCache.GetDoubles(AtomicReader, string, FieldCache.IDoubleParser, bool)"/> and makes
    /// those values available as other numeric types, casting as needed.
    /// </summary>
    public class DoubleFieldSource : FieldCacheSource
    {
        protected readonly FieldCache.IDoubleParser m_parser;

        public DoubleFieldSource(string field)
            : this(field, null)
        {
        }

        public DoubleFieldSource(string field, FieldCache.IDoubleParser parser)
            : base(field)
        {
            this.m_parser = parser;
        }

        public override string GetDescription()
        {
            return "double(" + m_field + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = m_cache.GetDoubles(readerContext.AtomicReader, m_field, m_parser, true);
            var valid = m_cache.GetDocsWithField(readerContext.AtomicReader, m_field);
            return new DoubleDocValuesAnonymousClass(this, arr, valid);
        }

        private sealed class DoubleDocValuesAnonymousClass : DoubleDocValues
        {
            private readonly FieldCache.Doubles arr;
            private readonly IBits valid;

            public DoubleDocValuesAnonymousClass(DoubleFieldSource @this, FieldCache.Doubles arr, IBits valid)
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

            public override ValueFiller GetValueFiller()
            {
                return new ValueFiller.AnonymousValueFiller<MutableValueDouble>(new MutableValueDouble(), fillValue: (doc, mutableValue) =>
                {
                    mutableValue.Value = arr.Get(doc);
                    mutableValue.Exists = mutableValue.Value != 0 || valid.Get(doc);
                });
            }
        }

        public override bool Equals(object o)
        {
            if (!(o is DoubleFieldSource other))
            {
                return false;
            }
            return base.Equals(other) &&
                   (this.m_parser is null ? other.m_parser is null : this.m_parser.GetType() == other.m_parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = m_parser is null ? typeof(double).GetHashCode() : m_parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}