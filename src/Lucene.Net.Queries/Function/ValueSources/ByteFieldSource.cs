using Lucene.Net.Index;
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
    /// Obtains <see cref="int"/> field values from the <see cref="Search.FieldCache"/>
    /// using <see cref="IFieldCache.GetInt32s"/>
    /// and makes those values available as other numeric types, casting as needed. *
    /// </summary>
    [Obsolete]
    public class ByteFieldSource : FieldCacheSource
    {
        private readonly FieldCache.IByteParser parser;

        public ByteFieldSource(string field)
            : this(field, null)
        {
        }

        public ByteFieldSource(string field, FieldCache.IByteParser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string GetDescription()
        {
            return "byte(" + m_field + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FieldCache.Bytes arr = m_cache.GetBytes(readerContext.AtomicReader, m_field, parser, false);

            return new FunctionValuesAnonymousInnerClassHelper(this, arr);
        }

        private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
        {
            private readonly ByteFieldSource outerInstance;
            private readonly FieldCache.Bytes arr;

            public FunctionValuesAnonymousInnerClassHelper(ByteFieldSource outerInstance, FieldCache.Bytes arr)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
            }

            public override byte ByteVal(int doc)
            {
                return (byte)arr.Get(doc);
            }

            public override short ShortVal(int doc)
            {
                return (short)arr.Get(doc);
            }

            public override float FloatVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            public override int IntVal(int doc)
            {
                return (int)arr.Get(doc);
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

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + '=' + ByteVal(doc);
            }

            public override object ObjectVal(int doc)
            {
                return arr.Get(doc); // TODO: valid?
            }

        }

        public override bool Equals(object o)
        {
            var other = o as ByteFieldSource;
            if (other == null)
                return false;
            return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser == null ? typeof(sbyte?).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}