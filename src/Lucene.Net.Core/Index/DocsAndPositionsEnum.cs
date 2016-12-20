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

    // javadocs
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Also iterates through positions. </summary>
    public abstract class DocsAndPositionsEnum : DocsEnum
    {
        /// <summary>
        /// Flag to pass to <seealso cref="TermsEnum#docsAndPositions(Bits,DocsAndPositionsEnum,int)"/>
        ///  if you require offsets in the returned enum.
        /// </summary>
        public static readonly int FLAG_OFFSETS = 0x1;

        /// <summary>
        /// Flag to pass to  <seealso cref="TermsEnum#docsAndPositions(Bits,DocsAndPositionsEnum,int)"/>
        ///  if you require payloads in the returned enum.
        /// </summary>
        public static readonly int FLAG_PAYLOADS = 0x2;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected DocsAndPositionsEnum()
        {
        }

        /// <summary>
        /// Returns the next position.  You should only call this
        ///  up to <seealso cref="DocsEnum#freq()"/> times else
        ///  the behavior is not defined.  If positions were not
        ///  indexed this will return -1; this only happens if
        ///  offsets were indexed and you passed needsOffset=true
        ///  when pulling the enum.
        /// </summary>
        public abstract int NextPosition();

        /// <summary>
        /// Returns start offset for the current position, or -1
        ///  if offsets were not indexed.
        /// </summary>
        public abstract int StartOffset(); // LUCENENET TODO: make property

        /// <summary>
        /// Returns end offset for the current position, or -1 if
        ///  offsets were not indexed.
        /// </summary>
        public abstract int EndOffset(); // LUCENENET TODO: make property

        /// <summary>
        /// Returns the payload at this position, or null if no
        ///  payload was indexed. You should not modify anything
        ///  (neither members of the returned BytesRef nor bytes
        ///  in the byte[]).
        /// </summary>
        public abstract BytesRef Payload { get; }
    }
}