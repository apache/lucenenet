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

    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Provides a <see cref="Codecs.PostingsReaderBase"/> and 
    /// <see cref="Codecs.PostingsWriterBase"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    // TODO: find a better name; this defines the API that the
    // terms dict impls use to talk to a postings impl.
    // TermsDict + PostingsReader/WriterBase == PostingsConsumer/Producer

    // can we clean this up and do this some other way?
    // refactor some of these classes and use covariant return?
    public abstract class PostingsBaseFormat
    {
        /// <summary>
        /// Unique name that's used to retrieve this codec when
        /// reading the index.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Sole constructor. </summary>
        protected PostingsBaseFormat(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Creates the <see cref="Codecs.PostingsReaderBase"/> for this
        /// format.
        /// </summary>
        public abstract PostingsReaderBase PostingsReaderBase(SegmentReadState state);

        /// <summary>
        /// Creates the <see cref="Codecs.PostingsWriterBase"/> for this
        /// format.
        /// </summary>
        public abstract PostingsWriterBase PostingsWriterBase(SegmentWriteState state);
    }
}