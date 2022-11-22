using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

    /// <summary>
    /// Access to the terms in a specific field.  See <see cref="Fields"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public abstract class Terms
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected Terms()
        {
        }

        /// <summary>
        /// Returns an iterator that will step through all
        /// terms. This method will not return <c>null</c>.
        /// </summary>
        public abstract TermsEnum GetEnumerator(); // LUCENENET specific - Refactored to require both overloads, so we don't have a strange null parameter unless needed

        /// <summary>
        /// Returns an iterator that will step through all
        /// terms. This method will not return <c>null</c>.
        /// </summary>
        /// <param name="reuse">If you have a previous <see cref="TermsEnum"/>,
        /// for example from a different field, you can pass it for possible
        /// reuse if the implementation can do so.</param>
        public virtual TermsEnum GetEnumerator(TermsEnum reuse) => GetEnumerator(); // LUCENENET specific - Refactored to require both overloads, so we don't have a strange null parameter unless needed

        /// <summary>
        /// Returns an iterator that will step through all
        /// terms. This method will not return <c>null</c>.  If you have
        /// a previous <see cref="TermsEnum"/>, for example from a different
        /// field, you can pass it for possible reuse if the
        /// implementation can do so.
        /// </summary>
        [Obsolete("Use GetEnumerator() or GetEnumerator(TermsEnum). This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual TermsEnum GetIterator(TermsEnum reuse) => GetEnumerator(reuse);

        /// <summary>
        /// Returns a <see cref="TermsEnum"/> that iterates over all terms that
        /// are accepted by the provided 
        /// <see cref="CompiledAutomaton"/>.  If the <paramref name="startTerm"/> is
        /// provided then the returned enum will only accept terms
        /// &gt; <paramref name="startTerm"/>, but you still must call
        /// <see cref="TermsEnum.MoveNext()"/> first to get to the first term.  Note that the
        /// provided <paramref name="startTerm"/> must be accepted by
        /// the automaton.
        ///
        /// <para><b>NOTE</b>: the returned <see cref="TermsEnum"/> cannot
        /// seek</para>.
        /// </summary>
        public virtual TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
        {
            // TODO: eventually we could support seekCeil/Exact on
            // the returned enum, instead of only being able to seek
            // at the start
            if (compiled.Type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
            {
                throw new ArgumentException("please use CompiledAutomaton.TermsEnum instead");
            }
            if (startTerm is null)
            {
                return new AutomatonTermsEnum(GetEnumerator(), compiled);
            }
            else
            {
                return new AutomatonTermsEnumAnonymousClass(GetEnumerator(), compiled, startTerm);
            }
        }

        private sealed class AutomatonTermsEnumAnonymousClass : AutomatonTermsEnum
        {
            private readonly BytesRef startTerm;

            public AutomatonTermsEnumAnonymousClass(TermsEnum iterator, CompiledAutomaton compiled, BytesRef startTerm)
                : base(iterator, compiled)
            {
                this.startTerm = startTerm;
            }

            protected override BytesRef NextSeekTerm(BytesRef term)
            {
                if (term is null)
                {
                    term = startTerm;
                }
                return base.NextSeekTerm(term);
            }
        }

        /// <summary>
        /// Return the <see cref="T:IComparer{BytesRef}"/> used to sort terms
        /// provided by the iterator.  This method may return <c>null</c>
        /// if there are no terms.  This method may be invoked
        /// many times; it's best to cache a single instance &amp;
        /// reuse it.
        /// </summary>
        public abstract IComparer<BytesRef> Comparer { get; }

        /// <summary>
        /// Returns the number of terms for this field, or -1 if this
        /// measure isn't stored by the codec. Note that, just like
        /// other term measures, this measure does not take deleted
        /// documents into account.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public abstract long Count { get; }

        /// <summary>
        /// Returns the sum of <see cref="TermsEnum.TotalTermFreq"/> for
        /// all terms in this field, or -1 if this measure isn't
        /// stored by the codec (or if this fields omits term freq
        /// and positions).  Note that, just like other term
        /// measures, this measure does not take deleted documents
        /// into account.
        /// </summary>
        public abstract long SumTotalTermFreq { get; } 

        /// <summary>
        /// Returns the sum of <see cref="TermsEnum.DocFreq"/> for
        /// all terms in this field, or -1 if this measure isn't
        /// stored by the codec.  Note that, just like other term
        /// measures, this measure does not take deleted documents
        /// into account.
        /// </summary>
        public abstract long SumDocFreq { get; }

        /// <summary>
        /// Returns the number of documents that have at least one
        /// term for this field, or -1 if this measure isn't
        /// stored by the codec.  Note that, just like other term
        /// measures, this measure does not take deleted documents
        /// into account.
        /// </summary>
        public abstract int DocCount { get; }

        /// <summary>
        /// Returns true if documents in this field store
        /// per-document term frequency (<see cref="DocsEnum.Freq"/>).
        /// </summary>
        public abstract bool HasFreqs { get; }

        /// <summary>
        /// Returns <c>true</c> if documents in this field store offsets. </summary>
        public abstract bool HasOffsets { get; }

        /// <summary>
        /// Returns <c>true</c> if documents in this field store positions. </summary>
        public abstract bool HasPositions { get; }

        /// <summary>
        /// Returns <c>true</c> if documents in this field store payloads. </summary>
        public abstract bool HasPayloads { get; }

        /// <summary>
        /// Zero-length array of <see cref="Terms"/>. </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly Terms[] EMPTY_ARRAY = Arrays.Empty<Terms>();
    }
}