using Lucene.Net.Util;
using System;
using System.Collections.Generic;

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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Iterator to seek (<seealso cref="#seekCeil(BytesRef)"/>, {@link
    /// #seekExact(BytesRef)}) or step through ({@link
    /// #next} terms to obtain frequency information ({@link
    /// #docFreq}), <seealso cref="DocsEnum"/> or {@link
    /// DocsAndPositionsEnum} for the current term ({@link
    /// #docs}.
    ///
    /// <p>Term enumerations are always ordered by
    /// <seealso cref="#getComparator"/>.  Each term in the enumeration is
    /// greater than the one before it.</p>
    ///
    /// <p>The TermsEnum is unpositioned when you first obtain it
    /// and you must first successfully call <seealso cref="#next"/> or one
    /// of the <code>seek</code> methods.
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class TermsEnum : BytesRefIterator
    {
        public abstract IComparer<BytesRef> Comparator { get; } // LUCENENET specific - must supply implementation for the interface

        public abstract BytesRef Next(); // LUCENENET specific - must supply implementation for the interface

        private AttributeSource atts = null;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected TermsEnum()
        {
        }

        /// <summary>
        /// Returns the related attributes. </summary>
        public virtual AttributeSource Attributes
        {
            get
            {
                if (atts == null)
                {
                    atts = new AttributeSource();
                }
                return atts;
            }
        }

        /// <summary>
        /// Represents returned result from <seealso cref="#seekCeil"/>. </summary>
        public enum SeekStatus
        {
            /// <summary>
            /// The term was not found, and the end of iteration was hit. </summary>
            END,

            /// <summary>
            /// The precise term was found. </summary>
            FOUND,

            /// <summary>
            /// A different term was found after the requested term </summary>
            NOT_FOUND
        }

        /// <summary>
        /// Attempts to seek to the exact term, returning
        ///  true if the term is found.  If this returns false, the
        ///  enum is unpositioned.  For some codecs, seekExact may
        ///  be substantially faster than <seealso cref="#seekCeil"/>.
        /// </summary>
        public virtual bool SeekExact(BytesRef text)
        {
            return SeekCeil(text) == SeekStatus.FOUND;
        }

        /// <summary>
        /// Seeks to the specified term, if it exists, or to the
        ///  next (ceiling) term.  Returns SeekStatus to
        ///  indicate whether exact term was found, a different
        ///  term was found, or EOF was hit.  The target term may
        ///  be before or after the current term.  If this returns
        ///  SeekStatus.END, the enum is unpositioned.
        /// </summary>
        public abstract SeekStatus SeekCeil(BytesRef text);

        /// <summary>
        /// Seeks to the specified term by ordinal (position) as
        ///  previously returned by <seealso cref="#ord"/>.  The target ord
        ///  may be before or after the current ord, and must be
        ///  within bounds.
        /// </summary>
        public abstract void SeekExact(long ord);

        /// <summary>
        /// Expert: Seeks a specific position by <seealso cref="TermState"/> previously obtained
        /// from <seealso cref="#termState()"/>. Callers should maintain the <seealso cref="TermState"/> to
        /// use this method. Low-level implementations may position the TermsEnum
        /// without re-seeking the term dictionary.
        /// <p>
        /// Seeking by <seealso cref="TermState"/> should only be used iff the state was obtained
        /// from the same <seealso cref="TermsEnum"/> instance.
        /// <p>
        /// NOTE: Using this method with an incompatible <seealso cref="TermState"/> might leave
        /// this <seealso cref="TermsEnum"/> in undefined state. On a segment level
        /// <seealso cref="TermState"/> instances are compatible only iff the source and the
        /// target <seealso cref="TermsEnum"/> operate on the same field. If operating on segment
        /// level, TermState instances must not be used across segments.
        /// <p>
        /// NOTE: A seek by <seealso cref="TermState"/> might not restore the
        /// <seealso cref="AttributeSource"/>'s state. <seealso cref="AttributeSource"/> states must be
        /// maintained separately if this method is used. </summary>
        /// <param name="term"> the term the TermState corresponds to </param>
        /// <param name="state"> the <seealso cref="TermState"/>
        ///  </param>
        public virtual void SeekExact(BytesRef term, TermState state)
        {
            if (!SeekExact(term))
            {
                throw new System.ArgumentException("term=" + term + " does not exist");
            }
        }

        /// <summary>
        /// Returns current term. Do not call this when the enum
        ///  is unpositioned.
        /// </summary>
        public abstract BytesRef Term { get; }

        /// <summary>
        /// Returns ordinal position for current term.  this is an
        ///  optional method (the codec may throw {@link
        ///  UnsupportedOperationException}).  Do not call this
        ///  when the enum is unpositioned.
        /// </summary>
        public abstract long Ord(); // LUCENENET TODO: make property ?

        /// <summary>
        /// Returns the number of documents containing the current
        ///  term.  Do not call this when the enum is unpositioned.
        ///  <seealso cref="SeekStatus#END"/>.
        /// </summary>
        public abstract int DocFreq(); // LUCENENET TODO: make property ?

        /// <summary>
        /// Returns the total number of occurrences of this term
        ///  across all documents (the sum of the freq() for each
        ///  doc that has this term).  this will be -1 if the
        ///  codec doesn't support this measure.  Note that, like
        ///  other term measures, this measure does not take
        ///  deleted documents into account.
        /// </summary>
        public abstract long TotalTermFreq(); // LUCENENET TODO: make property ?

        /// <summary>
        /// Get <seealso cref="DocsEnum"/> for the current term.  Do not
        ///  call this when the enum is unpositioned.  this method
        ///  will not return null.
        /// </summary>
        /// <param name="liveDocs"> unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> pass a prior DocsEnum for possible reuse  </param>
        public DocsEnum Docs(Bits liveDocs, DocsEnum reuse)
        {
            return Docs(liveDocs, reuse, DocsEnum.FLAG_FREQS);
        }

        /// <summary>
        /// Get <seealso cref="DocsEnum"/> for the current term, with
        ///  control over whether freqs are required.  Do not
        ///  call this when the enum is unpositioned.  this method
        ///  will not return null.
        /// </summary>
        /// <param name="liveDocs"> unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> pass a prior DocsEnum for possible reuse </param>
        /// <param name="flags"> specifies which optional per-document values
        ///        you require; see <seealso cref="DocsEnum#FLAG_FREQS"/> </param>
        /// <seealso cref= #docs(Bits, DocsEnum, int)  </seealso>
        public abstract DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags);

        /// <summary>
        /// Get <seealso cref="DocsAndPositionsEnum"/> for the current term.
        ///  Do not call this when the enum is unpositioned.  this
        ///  method will return null if positions were not
        ///  indexed.
        /// </summary>
        ///  <param name="liveDocs"> unset bits are documents that should not
        ///  be returned </param>
        ///  <param name="reuse"> pass a prior DocsAndPositionsEnum for possible reuse </param>
        ///  <seealso cref= #docsAndPositions(Bits, DocsAndPositionsEnum, int)  </seealso>
        public DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse)
        {
            return DocsAndPositions(liveDocs, reuse, DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS);
        }

        /// <summary>
        /// Get <seealso cref="DocsAndPositionsEnum"/> for the current term,
        ///  with control over whether offsets and payloads are
        ///  required.  Some codecs may be able to optimize their
        ///  implementation when offsets and/or payloads are not required.
        ///  Do not call this when the enum is unpositioned.  this
        ///  will return null if positions were not indexed.
        /// </summary>
        ///  <param name="liveDocs"> unset bits are documents that should not
        ///  be returned </param>
        ///  <param name="reuse"> pass a prior DocsAndPositionsEnum for possible reuse </param>
        ///  <param name="flags"> specifies which optional per-position values you
        ///         require; see <seealso cref="DocsAndPositionsEnum#FLAG_OFFSETS"/> and
        ///         <seealso cref="DocsAndPositionsEnum#FLAG_PAYLOADS"/>.  </param>
        public abstract DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags);

        /// <summary>
        /// Expert: Returns the TermsEnums internal state to position the TermsEnum
        /// without re-seeking the term dictionary.
        /// <p>
        /// NOTE: A seek by <seealso cref="TermState"/> might not capture the
        /// <seealso cref="AttributeSource"/>'s state. Callers must maintain the
        /// <seealso cref="AttributeSource"/> states separately
        /// </summary>
        /// <seealso cref= TermState </seealso>
        /// <seealso cref= #seekExact(BytesRef, TermState) </seealso>
        public virtual TermState TermState() // LUCENENET TODO: Rename GetTermState() ?
        {
            return new TermStateAnonymousInnerClassHelper(this);
        }

        private class TermStateAnonymousInnerClassHelper : TermState
        {
            private readonly TermsEnum outerInstance;

            public TermStateAnonymousInnerClassHelper(TermsEnum outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override void CopyFrom(TermState other)
            {
                throw new System.NotSupportedException();
            }
        }

        /// <summary>
        /// An empty TermsEnum for quickly returning an empty instance e.g.
        /// in <seealso cref="Lucene.Net.Search.MultiTermQuery"/>
        /// <p><em>Please note:</em> this enum should be unmodifiable,
        /// but it is currently possible to add Attributes to it.
        /// this should not be a problem, as the enum is always empty and
        /// the existence of unused Attributes does not matter.
        /// </summary>
        public static readonly TermsEnum EMPTY = new TermsEnumAnonymousInnerClassHelper();

        private class TermsEnumAnonymousInnerClassHelper : TermsEnum
        {
            public TermsEnumAnonymousInnerClassHelper()
            {
            }

            public override SeekStatus SeekCeil(BytesRef term)
            {
                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
            }

            public override BytesRef Term
            {
                get { throw new InvalidOperationException("this method should never be called"); }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public override int DocFreq()
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override long TotalTermFreq()
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override long Ord()
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override BytesRef Next()
            {
                return null;
            }

            public override AttributeSource Attributes // make it synchronized here, to prevent double lazy init
            {
                get
                {
                    lock (this)
                    {
                        return base.Attributes;
                    }
                }
            }

            public override TermState TermState()
            {
                throw new InvalidOperationException("this method should never be called");
            }

            public override void SeekExact(BytesRef term, TermState state)
            {
                throw new InvalidOperationException("this method should never be called");
            }
        }
    }
}