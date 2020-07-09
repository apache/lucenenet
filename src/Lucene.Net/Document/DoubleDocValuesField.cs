using System;

namespace Lucene.Net.Documents
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
    /// Syntactic sugar for encoding doubles as <see cref="Index.NumericDocValues"/>
    /// via <see cref="J2N.BitConversion.DoubleToRawInt64Bits(double)"/>.
    /// <para/>
    /// Per-document double values can be retrieved via
    /// <see cref="Search.IFieldCache.GetDoubles(Lucene.Net.Index.AtomicReader, string, bool)"/>.
    /// <para/>
    /// <b>NOTE</b>: In most all cases this will be rather inefficient,
    /// requiring eight bytes per document. Consider encoding double
    /// values yourself with only as much precision as you require.
    /// </summary>
    public class DoubleDocValuesField : NumericDocValuesField
    {
        /// <summary>
        /// Creates a new <see cref="Index.DocValues"/> field with the specified 64-bit double value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit double value </param>
        /// <exception cref="ArgumentNullException"> if the field name is <c>null</c> </exception>
        public DoubleDocValuesField(string name, double value)
            : base(name, J2N.BitConversion.DoubleToRawInt64Bits(value))
        {
        }

        public override void SetDoubleValue(double value)
        {
            base.SetInt64Value(J2N.BitConversion.DoubleToRawInt64Bits(value));
        }

        public override void SetInt64Value(long value)
        {
            throw new ArgumentException("cannot change value type from System.Double to System.Int64");
        }
    }
}