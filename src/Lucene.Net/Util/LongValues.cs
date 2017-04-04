namespace Lucene.Net.Util
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

    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;

    /// <summary>
    /// Abstraction over an array of longs.
    ///  this class extends NumericDocValues so that we don't need to add another
    ///  level of abstraction every time we want eg. to use the <seealso cref="PackedInt32s"/>
    ///  utility classes to represent a <seealso cref="NumericDocValues"/> instance.
    /// <para/>
    /// NOTE: This was LongValues in Lucene
    /// 
    ///  @lucene.internal
    /// </summary>
    public abstract class Int64Values : NumericDocValues
    {
        /// <summary>
        /// Get value at <code>index</code>. </summary>
        public abstract long Get(long index);

        public override long Get(int idx)
        {
            return Get((long)idx);
        }
    }
}