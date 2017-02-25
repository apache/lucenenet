namespace Lucene.Net.Codecs.Lucene3x
{
    using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;

    /// <summary>
    /// PreFlex implementation of <seealso cref="SegmentInfoWriter"/>.
    /// @lucene.experimental
    /// </summary>
#pragma warning disable 612, 618
    internal class PreFlexRWSegmentInfoWriter : SegmentInfoWriter
    {
        // NOTE: this is not "really" 3.x format, because we are
        // writing each SI to its own file, vs 3.x where the list
        // of segments and SI for each segment is written into a
        // single segments_N file

        /// <summary>
        /// Save a single segment's info. </summary>
        public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext ioContext)
        {
            SegmentInfos.Write3xInfo(dir, si, ioContext);
        }
    }
#pragma warning restore 612, 618
}