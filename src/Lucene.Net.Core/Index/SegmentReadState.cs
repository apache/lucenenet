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

    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat; // javadocs
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat; // javadocs

    /// <summary>
    /// Holder class for common parameters used during read.
    /// @lucene.experimental
    /// </summary>
    public class SegmentReadState
    {
        /// <summary>
        /// <seealso cref="Directory"/> where this segment is read from. </summary>
        public Directory Directory { get; private set; }

        /// <summary>
        /// <seealso cref="SegmentInfo"/> describing this segment. </summary>
        public SegmentInfo SegmentInfo { get; private set; }

        /// <summary>
        /// <seealso cref="FieldInfos"/> describing all fields in this
        ///  segment.
        /// </summary>
        public FieldInfos FieldInfos { get; private set; }

        /// <summary>
        /// <seealso cref="IOContext"/> to pass to {@link
        ///  Directory#openInput(String,IOContext)}.
        /// </summary>
        public IOContext Context { get; private set; }

        /// <summary>
        /// The {@code termInfosIndexDivisor} to use, if
        ///  appropriate (not all <seealso cref="PostingsFormat"/>s support
        ///  it; in particular the current default does not).
        ///
        /// <p>  NOTE: if this is &lt; 0, that means "defer terms index
        ///  load until needed".  But if the codec must load the
        ///  terms index on init (preflex is the only once currently
        ///  that must do so), then it should negate this value to
        ///  get the app's terms divisor
        /// </summary>
        public int TermsIndexDivisor { get; set; } 

        /// <summary>
        /// Unique suffix for any postings files read for this
        ///  segment.  <seealso cref="PerFieldPostingsFormat"/> sets this for
        ///  each of the postings formats it wraps.  If you create
        ///  a new <seealso cref="PostingsFormat"/> then any files you
        ///  write/read must be derived using this suffix (use
        ///  <seealso cref="IndexFileNames#segmentFileName(String,String,String)"/>).
        /// </summary>
        public string SegmentSuffix { get; private set; }

        /// <summary>
        /// Create a {@code SegmentReadState}. </summary>
        public SegmentReadState(Directory dir, SegmentInfo info, FieldInfos fieldInfos, IOContext context, int termsIndexDivisor)
            : this(dir, info, fieldInfos, context, termsIndexDivisor, "")
        {
        }

        /// <summary>
        /// Create a {@code SegmentReadState}. </summary>
        public SegmentReadState(Directory dir, SegmentInfo info, FieldInfos fieldInfos, IOContext context, int termsIndexDivisor, string segmentSuffix)
        {
            this.Directory = dir;
            this.SegmentInfo = info;
            this.FieldInfos = fieldInfos;
            this.Context = context;
            this.TermsIndexDivisor = termsIndexDivisor;
            this.SegmentSuffix = segmentSuffix;
        }

        /// <summary>
        /// Create a {@code SegmentReadState}. </summary>
        public SegmentReadState(SegmentReadState other, string newSegmentSuffix)
        {
            this.Directory = other.Directory;
            this.SegmentInfo = other.SegmentInfo;
            this.FieldInfos = other.FieldInfos;
            this.Context = other.Context;
            this.TermsIndexDivisor = other.TermsIndexDivisor;
            this.SegmentSuffix = newSegmentSuffix;
        }
    }
}