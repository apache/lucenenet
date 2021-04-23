using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene40
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

    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.0 Norms Format.
    /// <para/>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><c>.nrm.cfs</c>: compound container (<see cref="Store.CompoundFileDirectory"/>) </description></item>
    ///   <item><description><c>.nrm.cfe</c>: compound entries (<see cref="Store.CompoundFileDirectory"/>) </description></item>
    /// </list>
    /// Norms are implemented as DocValues, so other than file extension, norms are
    /// written exactly the same way as <see cref="Lucene40DocValuesFormat"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene40DocValuesFormat"/>
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    public class Lucene40NormsFormat : NormsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40NormsFormat()
        {
        }

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            throw UnsupportedOperationException.Create("this codec can only be used for reading");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            string filename = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "nrm", IndexFileNames.COMPOUND_FILE_EXTENSION);
            return new Lucene40DocValuesReader(state, filename, Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY);
        }
    }
}