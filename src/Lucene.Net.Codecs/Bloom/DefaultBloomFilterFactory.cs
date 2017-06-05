using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Bloom
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
    /// Default policy is to allocate a bitset with 10% saturation given a unique term per document.
    /// Bits are set via <see cref="MurmurHash2"/> hashing function.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class DefaultBloomFilterFactory : BloomFilterFactory
    {
        public override FuzzySet GetSetForField(SegmentWriteState state, FieldInfo info)
        {
            //Assume all of the docs have a unique term (e.g. a primary key) and we hope to maintain a set with 10% of bits set
            return FuzzySet.CreateSetBasedOnQuality(state.SegmentInfo.DocCount, 0.10f);
        }

        public override bool IsSaturated(FuzzySet bloomFilter, FieldInfo fieldInfo)
        {
            // Don't bother saving bitsets if >90% of bits are set - we don't want to
            // throw any more memory at this problem.
            return bloomFilter.GetSaturation() > 0.9f;
        }
    }
}