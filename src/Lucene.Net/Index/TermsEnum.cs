using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Enumerator to seek (<see cref="SeekCeil(BytesRef)"/>, 
    /// <see cref="SeekExact(BytesRef)"/>) or step through 
    /// (<see cref="MoveNext()"/> terms to obtain <see cref="Term"/>, frequency information 
    /// (<see cref="DocFreq"/>), <see cref="DocsEnum"/> or 
    /// <see cref="DocsAndPositionsEnum"/> for the current term 
    /// (<see cref="Docs(IBits, DocsEnum)"/>).
    ///
    /// <para/>Term enumerations are always ordered by
    /// <see cref="Comparer"/>.  Each term in the enumeration is
    /// greater than the one before it.
    ///
    /// <para/>The <see cref="TermsEnum"/> is unpositioned when you first obtain it
    /// and you must first successfully call <see cref="MoveNext()"/> or one
    /// of the <c>Seek</c> methods.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public abstract class TermsEnum : IBytesRefEnumerator
#pragma warning disable CS0618 // Type or member is obsolete
        , IBytesRefIterator
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public abstract IComparer<BytesRef> Comparer { get; } // LUCENENET specific - must supply implementation for the interface

        /// <inheritdoc/>
        [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract BytesRef Next(); // LUCENENET specific - must supply implementation for the interface

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TermsEnum Current => this; // LUCENENET specific - made into enumerator for foreach

        BytesRef IBytesRefEnumerator.Current => Term;

        /// <summary>
        /// Moves to the next item in the <see cref="TermsEnum"/>.
        /// <para/>
        /// The default implementation can and should be overridden with a more optimized version.
        /// </summary>
        /// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
        /// <c>false</c> if the enumerator has passed the end of the collection.</returns>
        public abstract bool MoveNext(); // LUCENENET specific - must supply implementation for the interface

        private AttributeSource atts = null;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
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
                if (atts is null)
                {
                    atts = new AttributeSource();
                }
                return atts;
            }
        }

        /// <summary>
        /// Represents returned result from <see cref="SeekCeil(BytesRef)"/>. </summary>
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
        /// <c>true</c> if the term is found.  If this returns <c>false</c>, the
        /// enum is unpositioned.  For some codecs, <see cref="SeekExact(BytesRef)"/> may
        /// be substantially faster than <see cref="SeekCeil(BytesRef)"/>.
        /// </summary>
        public virtual bool SeekExact(BytesRef text)
        {
            return SeekCeil(text) == SeekStatus.FOUND;
        }

        /// <summary>
        /// Seeks to the specified term, if it exists, or to the
        /// next (ceiling) term.  Returns <see cref="SeekStatus"/> to
        /// indicate whether exact term was found, a different
        /// term was found, or EOF was hit.  The target term may
        /// be before or after the current term.  If this returns
        /// <see cref="SeekStatus.END"/>, the enum is unpositioned.
        /// </summary>
        public abstract SeekStatus SeekCeil(BytesRef text);

        /// <summary>
        /// Seeks to the specified term by ordinal (position) as
        /// previously returned by <see cref="Ord"/>. The target <paramref name="ord"/>
        /// may be before or after the current ord, and must be
        /// within bounds.
        /// </summary>
        public abstract void SeekExact(long ord);

        /// <summary>
        /// Expert: Seeks a specific position by <see cref="TermState"/> previously obtained
        /// from <see cref="GetTermState()"/>. Callers should maintain the <see cref="TermState"/> to
        /// use this method. Low-level implementations may position the <see cref="TermsEnum"/>
        /// without re-seeking the term dictionary.
        /// <para/>
        /// Seeking by <see cref="TermState"/> should only be used iff the state was obtained
        /// from the same <see cref="TermsEnum"/> instance.
        /// <para/>
        /// NOTE: Using this method with an incompatible <see cref="TermState"/> might leave
        /// this <see cref="TermsEnum"/> in undefined state. On a segment level
        /// <see cref="TermState"/> instances are compatible only iff the source and the
        /// target <see cref="TermsEnum"/> operate on the same field. If operating on segment
        /// level, TermState instances must not be used across segments.
        /// <para/>
        /// NOTE: A seek by <see cref="TermState"/> might not restore the
        /// <see cref="AttributeSource"/>'s state. <see cref="AttributeSource"/> states must be
        /// maintained separately if this method is used. </summary>
        /// <param name="term"> the term the <see cref="TermState"/> corresponds to </param>
        /// <param name="state"> the <see cref="TermState"/> </param>
        public virtual void SeekExact(BytesRef term, TermState state)
        {
            if (!SeekExact(term))
            {
                throw new ArgumentException("term=" + term + " does not exist");
            }
        }

        /// <summary>
        /// Returns current term. Do not call this when the enum
        /// is unpositioned.
        /// </summary>
        public abstract BytesRef Term { get; }

        /// <summary>
        /// Returns ordinal position for current term.  This is an
        /// optional property (the codec may throw <see cref="NotSupportedException"/>.
        /// Do not call this when the enum is unpositioned.
        /// </summary>
        public abstract long Ord { get; } // LUCENENET NOTE: Although this isn't a great candidate for a property, did so to make API consistent

        /// <summary>
        /// Returns the number of documents containing the current
        /// term.  Do not call this when the enum is unpositioned.
        /// </summary>
        /// <seealso cref="SeekStatus.END"/>
        public abstract int DocFreq { get; } // LUCENENET NOTE: Although this isn't a great candidate for a property, did so to make API consistent

        /// <summary>
        /// Returns the total number of occurrences of this term
        /// across all documents (the sum of the Freq for each
        /// doc that has this term).  This will be -1 if the
        /// codec doesn't support this measure.  Note that, like
        /// other term measures, this measure does not take
        /// deleted documents into account.
        /// </summary>
        public abstract long TotalTermFreq { get; } // LUCENENET NOTE: Although this isn't a great candidate for a property, did so to make API consistent

        /// <summary>
        /// Get <see cref="DocsEnum"/> for the current term. Do not
        /// call this when the enum is unpositioned. This method
        /// will not return <c>null</c>.
        /// </summary>
        /// <param name="liveDocs"> Unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> Pass a prior <see cref="DocsEnum"/> for possible reuse </param>
        public DocsEnum Docs(IBits liveDocs, DocsEnum reuse)
        {
            return Docs(liveDocs, reuse, DocsFlags.FREQS);
        }

        /// <summary>
        /// Get <see cref="DocsEnum"/> for the current term, with
        /// control over whether freqs are required. Do not
        /// call this when the enum is unpositioned. This method
        /// will not return <c>null</c>.
        /// </summary>
        /// <param name="liveDocs"> Unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> Pass a prior DocsEnum for possible reuse </param>
        /// <param name="flags"> Specifies which optional per-document values
        ///        you require; <see cref="DocsFlags"/></param>
        /// <seealso cref="Docs(IBits, DocsEnum)"/>
        public abstract DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags);

        /// <summary>
        /// Get <see cref="DocsAndPositionsEnum"/> for the current term.
        /// Do not call this when the enum is unpositioned.  This
        /// method will return <c>null</c> if positions were not
        /// indexed.
        /// </summary>
        /// <param name="liveDocs"> Unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> Pass a prior DocsAndPositionsEnum for possible reuse </param>
        /// <seealso cref="DocsAndPositions(IBits, DocsAndPositionsEnum, DocsAndPositionsFlags)"/>
        public DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse)
        {
            return DocsAndPositions(liveDocs, reuse, DocsAndPositionsFlags.OFFSETS | DocsAndPositionsFlags.PAYLOADS);
        }

        /// <summary>
        /// Get <see cref="DocsAndPositionsEnum"/> for the current term,
        /// with control over whether offsets and payloads are
        /// required.  Some codecs may be able to optimize their
        /// implementation when offsets and/or payloads are not required.
        /// Do not call this when the enum is unpositioned. This
        /// will return <c>null</c> if positions were not indexed.
        /// </summary>
        /// <param name="liveDocs"> Unset bits are documents that should not
        /// be returned </param>
        /// <param name="reuse"> Pass a prior DocsAndPositionsEnum for possible reuse </param>
        /// <param name="flags"> Specifies which optional per-position values you
        ///         require; see <see cref="DocsAndPositionsFlags"/>. </param>
        public abstract DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags);

        /// <summary>
        /// Expert: Returns the <see cref="TermsEnum"/>s internal state to position the <see cref="TermsEnum"/>
        /// without re-seeking the term dictionary.
        /// <para/>
        /// NOTE: A seek by <see cref="GetTermState()"/> might not capture the
        /// <see cref="AttributeSource"/>'s state. Callers must maintain the
        /// <see cref="AttributeSource"/> states separately
        /// </summary>
        /// <seealso cref="TermState"/>
        /// <seealso cref="SeekExact(BytesRef, TermState)"/>
        public virtual TermState GetTermState() // LUCENENET NOTE: Renamed from TermState()
        {
            return new TermStateAnonymousClass();
        }

        private sealed class TermStateAnonymousClass : TermState
        {
            public override void CopyFrom(TermState other)
            {
                throw UnsupportedOperationException.Create();
            }
        }

        /// <summary>
        /// An empty <see cref="TermsEnum"/> for quickly returning an empty instance e.g.
        /// in <see cref="Lucene.Net.Search.MultiTermQuery"/>
        /// <para/><em>Please note:</em> this enum should be unmodifiable,
        /// but it is currently possible to add Attributes to it.
        /// This should not be a problem, as the enum is always empty and
        /// the existence of unused Attributes does not matter.
        /// </summary>
        public static readonly TermsEnum EMPTY = new TermsEnumAnonymousClass();

        private sealed class TermsEnumAnonymousClass : TermsEnum
        {
            public override SeekStatus SeekCeil(BytesRef term)
            {
                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
            }

            public override BytesRef Term => throw IllegalStateException.Create("this method should never be called");

            public override IComparer<BytesRef> Comparer => null;

            public override int DocFreq => throw IllegalStateException.Create("this method should never be called");

            public override long TotalTermFreq => throw IllegalStateException.Create("this method should never be called");

            public override long Ord => throw IllegalStateException.Create("this method should never be called");

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                throw IllegalStateException.Create("this method should never be called");
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                throw IllegalStateException.Create("this method should never be called");
            }

            // LUCENENET specific
            public override bool MoveNext()
            {
                return false;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                return null;
            }

            public override AttributeSource Attributes // make it synchronized here, to prevent double lazy init
            {
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        return base.Attributes;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            public override TermState GetTermState()
            {
                throw IllegalStateException.Create("this method should never be called");
            }

            public override void SeekExact(BytesRef term, TermState state)
            {
                throw IllegalStateException.Create("this method should never be called");
            }
        }
    }
}