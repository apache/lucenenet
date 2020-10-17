using Lucene.Net.Search;

namespace Lucene.Net.Index
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
    /// A per-document numeric value.
    /// </summary>
    public abstract class NumericDocValues : IFieldCacheGetter<byte>,  // LUCENENET specific - Add interfaces per type to reduce previous Func<int,T> allocations on reading from cache
                                             IFieldCacheGetter<short>, 
                                             IFieldCacheGetter<int>,
                                             IFieldCacheGetter<long>, 
                                             IFieldCacheGetter<float>, 
                                             IFieldCacheGetter<double>
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected NumericDocValues()
        {
        }

        /// <summary>
        /// Returns the numeric value for the specified document ID. </summary>
        /// <param name="docID"> document ID to lookup </param>
        /// <returns> numeric value </returns>
        public abstract long Get(int docID);

        byte IFieldCacheGetter<byte>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (byte)Get(docID);
        }

        short IFieldCacheGetter<short>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (short)Get(docID);
        }

        int IFieldCacheGetter<int>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return (int)Get(docID);
        }

        long IFieldCacheGetter<long>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return Get(docID);
        }

        float IFieldCacheGetter<float>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return J2N.BitConversion.Int32BitsToSingle((int)Get(docID));
        }

        double IFieldCacheGetter<double>.GetCached(int docID) // LUCENENET specific - moved read logic from FieldCacheImpl to here
        {
            return J2N.BitConversion.Int64BitsToDouble(Get(docID));
        }
    }
}