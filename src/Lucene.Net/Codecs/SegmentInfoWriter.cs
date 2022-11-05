using System.IO;

namespace Lucene.Net.Codecs
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

    using Directory = Lucene.Net.Store.Directory;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Specifies an API for classes that can write out <see cref="SegmentInfo"/> data.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class SegmentInfoWriter
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected SegmentInfoWriter()
        {
        }

        /// <summary>
        /// Write <see cref="SegmentInfo"/> data. </summary>
        /// <exception cref="IOException"> If an I/O error occurs. </exception>
        public abstract void Write(Directory dir, SegmentInfo info, FieldInfos fis, IOContext ioContext);
    }
}