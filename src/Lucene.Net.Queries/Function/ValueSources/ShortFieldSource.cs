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
    /// Obtains <see cref="short"/> field values from the <see cref="FieldCache"/>
    /// using <see cref="IFieldCache.GetInt16s(AtomicReader, string, FieldCache.IInt16Parser, bool)"/>
    /// and makes those values available as other numeric types, casting as needed.
    /// <para/>
    /// NOTE: This was ShortFieldSource in Lucene
    /// </summary>
    [Obsolete]
    public class Int16FieldSource : FieldCacheSource
    {
        private readonly FieldCache.IInt16Parser parser;

        public Int16FieldSource(string field)
            : this(field, null)
        {
        }

        public Int16FieldSource(string field, FieldCache.IInt16Parser parser)
            : base(field)
        {
            this.parser = parser;
        }

        public override string GetDescription()
        {
            return "short(" + m_field + ')';
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = m_cache.GetInt16s(readerContext.AtomicReader, m_field, parser, false);
            return new FunctionValuesAnonymousInnerClassHelper(this, arr);
        }

        private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
        {
            private readonly Int16FieldSource outerInstance;
            private readonly FieldCache.Int16s arr;

            public FunctionValuesAnonymousInnerClassHelper(Int16FieldSource outerInstance, FieldCache.Int16s arr)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
            }

            public override byte ByteVal(int doc)
            {
                return (byte)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was shortVal() in Lucene
            /// </summary>
            public override short Int16Val(int doc)
            {
                return arr.Get(doc);
            }

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
                return (int)arr.Get(doc);
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
                return arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                return Convert.ToString(arr.Get(doc));
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + '=' + Int16Val(doc);
            }
        }

        public override bool Equals(object o)
        {
            var other = o as Int16FieldSource;
            if (other == null)
                return false;
            return base.Equals(other) 
                && (parser == null ? other.parser == null : 
                parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            var h = parser == null ? typeof(short?).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}