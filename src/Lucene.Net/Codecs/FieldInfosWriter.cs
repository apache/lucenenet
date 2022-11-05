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

    /// <summary>
    /// Codec API for writing <see cref="FieldInfos"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldInfosWriter
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected FieldInfosWriter()
        {
        }

        /// <summary>
        /// Writes the provided <see cref="FieldInfos"/> to the
        /// directory.
        /// </summary>
        public abstract void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context);
    }
}