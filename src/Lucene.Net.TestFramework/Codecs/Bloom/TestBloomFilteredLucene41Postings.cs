using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Bloom
{
    /**
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
    /// A class used for testing <see cref="BloomFilteringPostingsFormat"/> with a concrete
    /// delegate (Lucene41). Creates a Bloom filter on ALL fields and with tiny
    /// amounts of memory reserved for the filter. DO NOT USE IN A PRODUCTION
    /// APPLICATION! This is not a realistic application of Bloom Filters as they
    /// ordinarily are larger and operate on only primary key type fields.
    /// </summary>
    [PostingsFormatName("TestBloomFilteredLucene41Postings")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class TestBloomFilteredLucene41Postings : PostingsFormat
    {
        private readonly BloomFilteringPostingsFormat @delegate;

        // Special class used to avoid OOM exceptions where Junit tests create many
        // fields.
        internal class LowMemoryBloomFactory : BloomFilterFactory
        {
            public override FuzzySet GetSetForField(SegmentWriteState state, FieldInfo info)
            {
                return FuzzySet.CreateSetBasedOnMaxMemory(1024);
            }

            public override bool IsSaturated(FuzzySet bloomFilter, FieldInfo fieldInfo)
            {
                // For test purposes always maintain the BloomFilter - even past the point
                // of usefulness when all bits are set
                return false;
            }
        }

        public TestBloomFilteredLucene41Postings()
            : base()
        {
            @delegate = new BloomFilteringPostingsFormat(new Lucene41PostingsFormat(),
                new LowMemoryBloomFactory());
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return @delegate.FieldsConsumer(state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return @delegate.FieldsProducer(state);
        }
    }
}
