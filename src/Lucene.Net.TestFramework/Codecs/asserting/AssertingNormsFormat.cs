using System.Diagnostics;

namespace Lucene.Net.Codecs.asserting
{
    using AssertingDocValuesProducer = Lucene.Net.Codecs.asserting.AssertingDocValuesFormat.AssertingDocValuesProducer;

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

    using AssertingNormsConsumer = Lucene.Net.Codecs.asserting.AssertingDocValuesFormat.AssertingNormsConsumer;
    using Lucene42NormsFormat = Lucene.Net.Codecs.Lucene42.Lucene42NormsFormat;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Just like <seealso cref="Lucene42NormsFormat"/> but with additional asserts.
    /// </summary>
    public class AssertingNormsFormat : NormsFormat
    {
        private readonly NormsFormat @in = new Lucene42NormsFormat();

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            DocValuesConsumer consumer = @in.NormsConsumer(state);
            Debug.Assert(consumer != null);
            return new AssertingNormsConsumer(consumer, state.SegmentInfo.DocCount);
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            Debug.Assert(state.FieldInfos.HasNorms);
            DocValuesProducer producer = @in.NormsProducer(state);
            Debug.Assert(producer != null);
            return new AssertingDocValuesProducer(producer, state.SegmentInfo.DocCount);
        }
    }
}