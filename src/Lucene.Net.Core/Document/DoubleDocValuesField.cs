using System;

using Lucene.Net.Search;

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

    // javadocs
    // javadocs
    
    /// <summary>
    /// Syntactic sugar for encoding doubles as NumericDocValues
    /// via <seealso cref="Double#doubleToRawLongBits(double)"/>.
    /// 
    /// Per-document double values can be retrieved via
    /// <seealso cref="IFieldCache#getDoubles(AtomicReader, String, boolean)"/>.
    /// 
    /// <b>NOTE</b>: In most all cases this will be rather inefficient,
    /// requiring eight bytes per document. Consider encoding double
    /// values yourself with only as much precision as you require.
    /// </summary>
    public class DoubleDocValuesField : NumericDocValuesField
    {
        /// <summary>
        /// Creates a new DocValues field with the specified 64-bit double value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit double value </param>
        /// <exception cref="ArgumentException"> if the field name is null </exception>
        public DoubleDocValuesField(string name, double value)
            : base(name, BitConverter.DoubleToInt64Bits(value))
        {
        }

        public override void SetDoubleValue(double value)
        {
            base.SetInt64Value(BitConverter.DoubleToInt64Bits(value));
        }

        public override void SetInt64Value(long value)
        {
            throw new System.ArgumentException("cannot change value type from Double to Long");
        }
    }
}