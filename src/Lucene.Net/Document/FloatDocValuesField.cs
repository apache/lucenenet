using J2N;
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
    /// Syntactic sugar for encoding floats as <see cref="Index.NumericDocValues"/>
    /// via <see cref="J2N.BitConversion.SingleToRawInt32Bits(float)"/>.
    /// <para>
    /// Per-document floating point values can be retrieved via
    /// <seealso cref="Search.IFieldCache.GetSingles(Lucene.Net.Index.AtomicReader, string, bool)"/>.</para>
    /// <para>
    /// <b>NOTE</b>: In most all cases this will be rather inefficient,
    /// requiring four bytes per document. Consider encoding floating
    /// point values yourself with only as much precision as you require.
    /// </para>
    /// <para>
    /// NOTE: This was FloatDocValuesField in Lucene
    /// </para>
    /// </summary>
    public class SingleDocValuesField : NumericDocValuesField
    {
        /// <summary>
        /// Creates a new DocValues field with the specified 32-bit <see cref="float"/> value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="float"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field name is <c>null</c> </exception>
        public SingleDocValuesField(string name, float value)
            : base(name, BitConversion.SingleToRawInt32Bits(value))
        {
        }

        public override void SetSingleValue(float value)
        {
            base.SetInt64Value(BitConversion.SingleToRawInt32Bits(value));
        }

        public override void SetInt64Value(long value)
        {
            throw new ArgumentException("cannot change value type from System.Single to System.Int64");
        }
    }
}