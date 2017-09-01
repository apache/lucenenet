using System;

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

    // LUCENENET specific - converted constants from DocsAndPositionsEnum
    // into a flags enum.
    [Flags]
    public enum DocsAndPositionsFlags
    {
        /// <summary>
        /// Flag to pass to <see cref="TermsEnum.DocsAndPositions(Util.IBits, DocsAndPositionsEnum, DocsAndPositionsFlags)"/> 
        /// if you require that no offsets and payloads will be returned.
        /// </summary>
        NONE = 0x0,

        /// <summary>
        /// Flag to pass to <see cref="TermsEnum.DocsAndPositions(Util.IBits, DocsAndPositionsEnum, DocsAndPositionsFlags)"/>
        /// if you require offsets in the returned enum.
        /// </summary>
        OFFSETS = 0x1, // LUCENENET specific - renamed from FLAG_OFFSETS since FLAG_ makes it redundant

        /// <summary>
        /// Flag to pass to  <see cref="TermsEnum.DocsAndPositions(Util.IBits, DocsAndPositionsEnum, DocsAndPositionsFlags)"/>
        /// if you require payloads in the returned enum.
        /// </summary>
        PAYLOADS = 0x2 // LUCENENET specific - renamed from FLAG_PAYLOADS since FLAG_ makes it redundant
    }


    /// <summary>
    /// Also iterates through positions. </summary>
    public abstract class DocsAndPositionsEnum : DocsEnum
    {
        // LUCENENET specific - made flags into their own [Flags] enum named DocsAndPositionsFlags and de-nested from this type

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected DocsAndPositionsEnum()
        {
        }

        /// <summary>
        /// Returns the next position.  You should only call this
        /// up to <see cref="DocsEnum.Freq"/> times else
        /// the behavior is not defined.  If positions were not
        /// indexed this will return -1; this only happens if
        /// offsets were indexed and you passed needsOffset=true
        /// when pulling the enum.
        /// </summary>
        public abstract int NextPosition();

        /// <summary>
        /// Returns start offset for the current position, or -1
        /// if offsets were not indexed.
        /// </summary>
        public abstract int StartOffset { get; }

        /// <summary>
        /// Returns end offset for the current position, or -1 if
        /// offsets were not indexed.
        /// </summary>
        public abstract int EndOffset { get; }

        /// <summary>
        /// Returns the payload at this position, or <c>null</c> if no
        /// payload was indexed. You should not modify anything
        /// (neither members of the returned <see cref="BytesRef"/> nor bytes
        /// in the <see cref="T:byte[]"/>).
        /// </summary>
        public abstract BytesRef GetPayload();
    }
}