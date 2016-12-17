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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

    /// <summary>
    /// Access to the terms in a specific field.  See <seealso cref="Fields"/>.
    /// @lucene.experimental
    /// </summary>

    public abstract class Terms
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal Terms()
        {
        }

        /// <summary>
        /// Returns an iterator that will step through all
        ///  terms. this method will not return null.  If you have
        ///  a previous TermsEnum, for example from a different
        ///  field, you can pass it for possible reuse if the
        ///  implementation can do so.
        /// </summary>
        public abstract TermsEnum Iterator(TermsEnum reuse);

        /// <summary>
        /// Returns a TermsEnum that iterates over all terms that
        ///  are accepted by the provided {@link
        ///  CompiledAutomaton}.  If the <code>startTerm</code> is
        ///  provided then the returned enum will only accept terms
        ///  > <code>startTerm</code>, but you still must call
        ///  next() first to get to the first term.  Note that the
        ///  provided <code>startTerm</code> must be accepted by
        ///  the automaton.
        ///
        /// <p><b>NOTE</b>: the returned TermsEnum cannot
        /// seek</p>.
        /// </summary>
        public virtual TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
        {
            // TODO: eventually we could support seekCeil/Exact on
            // the returned enum, instead of only being able to seek
            // at the start
            if (compiled.Type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
            {
                throw new System.ArgumentException("please use CompiledAutomaton.getTermsEnum instead");
            }
            if (startTerm == null)
            {
                return new AutomatonTermsEnum(Iterator(null), compiled);
            }
            else
            {
                return new AutomatonTermsEnumAnonymousInnerClassHelper(this, Iterator(null), compiled, startTerm);
            }
        }

        private class AutomatonTermsEnumAnonymousInnerClassHelper : AutomatonTermsEnum
        {
            private readonly Terms OuterInstance;

            private BytesRef StartTerm;

            public AutomatonTermsEnumAnonymousInnerClassHelper(Terms outerInstance, Lucene.Net.Index.TermsEnum iterator, CompiledAutomaton compiled, BytesRef startTerm)
                : base(iterator, compiled)
            {
                this.OuterInstance = outerInstance;
                this.StartTerm = startTerm;
            }

            protected internal override BytesRef NextSeekTerm(BytesRef term)
            {
                if (term == null)
                {
                    term = StartTerm;
                }
                return base.NextSeekTerm(term);
            }
        }

        /// <summary>
        /// Return the BytesRef Comparator used to sort terms
        ///  provided by the iterator.  this method may return null
        ///  if there are no terms.  this method may be invoked
        ///  many times; it's best to cache a single instance &
        ///  reuse it.
        /// </summary>
        public abstract IComparer<BytesRef> Comparator { get; } // LUCENENET TODO: Rename to Comparer

        /// <summary>
        /// Returns the number of terms for this field, or -1 if this
        ///  measure isn't stored by the codec. Note that, just like
        ///  other term measures, this measure does not take deleted
        ///  documents into account.
        /// </summary>
        public abstract long Size(); // LUCENENET TODO: Rename to Count property

        /// <summary>
        /// Returns the sum of <seealso cref="TermsEnum#totalTermFreq"/> for
        ///  all terms in this field, or -1 if this measure isn't
        ///  stored by the codec (or if this fields omits term freq
        ///  and positions).  Note that, just like other term
        ///  measures, this measure does not take deleted documents
        ///  into account.
        /// </summary>
        public abstract long SumTotalTermFreq { get; }

        /// <summary>
        /// Returns the sum of <seealso cref="TermsEnum#docFreq()"/> for
        ///  all terms in this field, or -1 if this measure isn't
        ///  stored by the codec.  Note that, just like other term
        ///  measures, this measure does not take deleted documents
        ///  into account.
        /// </summary>
        public abstract long SumDocFreq { get; }

        /// <summary>
        /// Returns the number of documents that have at least one
        ///  term for this field, or -1 if this measure isn't
        ///  stored by the codec.  Note that, just like other term
        ///  measures, this measure does not take deleted documents
        ///  into account.
        /// </summary>
        public abstract int DocCount { get; }

        /// <summary>
        /// Returns true if documents in this field store
        ///  per-document term frequency (<seealso cref="DocsEnum#freq"/>).
        /// </summary>
        public abstract bool HasFreqs(); // LUCENENET TODO: make property

        /// <summary>
        /// Returns true if documents in this field store offsets. </summary>
        public abstract bool HasOffsets(); // LUCENENET TODO: make property

        /// <summary>
        /// Returns true if documents in this field store positions. </summary>
        public abstract bool HasPositions(); // LUCENENET TODO: make property

        /// <summary>
        /// Returns true if documents in this field store payloads. </summary>
        public abstract bool HasPayloads(); // LUCENENET TODO: make property

        /// <summary>
        /// Zero-length array of <seealso cref="Terms"/>. </summary>
        public static readonly Terms[] EMPTY_ARRAY = new Terms[0];
    }
}