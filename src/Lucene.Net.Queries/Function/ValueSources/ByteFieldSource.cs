// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
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
    /// Obtains <see cref="int"/> field values from the <see cref="Search.FieldCache"/>
    /// using <see cref="IFieldCache.GetInt32s(AtomicReader, string, FieldCache.IInt32Parser, bool)"/>
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

            return new FunctionValuesAnonymousClass(this, arr);
        }

        private sealed class FunctionValuesAnonymousClass : FunctionValues
        {
            private readonly ByteFieldSource outerInstance;
            private readonly FieldCache.Bytes arr;

            public FunctionValuesAnonymousClass(ByteFieldSource outerInstance, FieldCache.Bytes arr)
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
                return (short)(sbyte)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return (float)(sbyte)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return (int)(sbyte)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return (long)(sbyte)arr.Get(doc);
            }

            public override double DoubleVal(int doc)
            {
                return (double)(sbyte)arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                return J2N.Numerics.SByte.ToString((sbyte)arr.Get(doc), CultureInfo.InvariantCulture);
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + '=' + ByteVal(doc);
            }

            public override object ObjectVal(int doc)
            {
                // LUCENENET: In Java, the conversion to instance of java.util.Byte is implicit, but we need to do an explicit conversion
                return J2N.Numerics.SByte.GetInstance((sbyte)arr.Get(doc)); // TODO: valid?
            }

        }

        public override bool Equals(object o)
        {
            if (o is null) return false;
            if (!(o is ByteFieldSource other)) return false;
            return base.Equals(other) && (this.parser is null ? other.parser is null : this.parser.GetType() == other.parser.GetType());
        }

        public override int GetHashCode()
        {
            int h = parser is null ? typeof(sbyte?).GetHashCode() : parser.GetType().GetHashCode();
            h += base.GetHashCode();
            return h;
        }
    }
}