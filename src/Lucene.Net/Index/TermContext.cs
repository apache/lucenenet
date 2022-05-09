using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Maintains a <see cref="IndexReader"/> <see cref="TermState"/> view over
    /// <see cref="IndexReader"/> instances containing a single term. The
    /// <see cref="TermContext"/> doesn't track if the given <see cref="TermState"/>
    /// objects are valid, neither if the <see cref="TermState"/> instances refer to the
    /// same terms in the associated readers.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class TermContext
    {
        /// <summary>
        /// Holds the <see cref="IndexReaderContext"/> of the top-level
        /// <see cref="IndexReader"/>, used internally only for
        /// asserting.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public IndexReaderContext TopReaderContext { get; private set; }

        private readonly TermState[] states;
        private int docFreq;
        private long totalTermFreq;

        //public static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        /// <summary>
        /// Creates an empty <see cref="TermContext"/> from a <see cref="IndexReaderContext"/>
        /// </summary>
        public TermContext(IndexReaderContext context)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(context != null && context.IsTopLevel);
            TopReaderContext = context;
            docFreq = 0;
            int len;
            if (context.Leaves is null)
            {
                len = 1;
            }
            else
            {
                len = context.Leaves.Count;
            }
            states = new TermState[len];
        }

        /// <summary>
        /// Creates a <see cref="TermContext"/> with an initial <see cref="TermState"/>,
        /// <see cref="IndexReader"/> pair.
        /// </summary>
        public TermContext(IndexReaderContext context, TermState state, int ord, int docFreq, long totalTermFreq)
            : this(context)
        {
            Register(state, ord, docFreq, totalTermFreq);
        }

        /// <summary>
        /// Creates a <see cref="TermContext"/> from a top-level <see cref="IndexReaderContext"/> and the
        /// given <see cref="Term"/>. this method will lookup the given term in all context's leaf readers
        /// and register each of the readers containing the term in the returned <see cref="TermContext"/>
        /// using the leaf reader's ordinal.
        /// <para/>
        /// Note: the given context must be a top-level context.
        /// </summary>
        public static TermContext Build(IndexReaderContext context, Term term)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(context != null && context.IsTopLevel);
            string field = term.Field;
            BytesRef bytes = term.Bytes;
            TermContext perReaderTermState = new TermContext(context);
            //if (DEBUG) System.out.println("prts.build term=" + term);
            foreach (AtomicReaderContext ctx in context.Leaves)
            {
                //if (DEBUG) System.out.println("  r=" + leaves[i].reader);
                Fields fields = ctx.AtomicReader.Fields;
                if (fields != null)
                {
                    Terms terms = fields.GetTerms(field);
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.GetEnumerator();
                        if (termsEnum.SeekExact(bytes))
                        {
                            TermState termState = termsEnum.GetTermState();
                            //if (DEBUG) System.out.println("    found");
                            perReaderTermState.Register(termState, ctx.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                        }
                    }
                }
            }
            return perReaderTermState;
        }

        /// <summary>
        /// Clears the <see cref="TermContext"/> internal state and removes all
        /// registered <see cref="TermState"/>s
        /// </summary>
        public void Clear()
        {
            docFreq = 0;
            Arrays.Fill(states, null);
        }

        /// <summary>
        /// Registers and associates a <see cref="TermState"/> with an leaf ordinal. The leaf ordinal
        /// should be derived from a <see cref="IndexReaderContext"/>'s leaf ord.
        /// </summary>
        public void Register(TermState state, int ord, int docFreq, long totalTermFreq)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(state != null, "state must not be null");
                Debugging.Assert(ord >= 0 && ord < states.Length);
                Debugging.Assert(states[ord] is null, "state for ord: {0} already registered", ord);
            }
            this.docFreq += docFreq;
            if (this.totalTermFreq >= 0 && totalTermFreq >= 0)
            {
                this.totalTermFreq += totalTermFreq;
            }
            else
            {
                this.totalTermFreq = -1;
            }
            states[ord] = state;
        }

        /// <summary>
        /// Returns the <see cref="TermState"/> for an leaf ordinal or <c>null</c> if no
        /// <see cref="TermState"/> for the ordinal was registered.
        /// </summary>
        /// <param name="ord">
        ///          The readers leaf ordinal to get the <see cref="TermState"/> for. </param>
        /// <returns> The <see cref="TermState"/> for the given readers ord or <c>null</c> if no
        ///         <see cref="TermState"/> for the reader was registered </returns>
        public TermState Get(int ord)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(ord >= 0 && ord < states.Length);
            return states[ord];
        }

        /// <summary>
        ///  Returns the accumulated term frequency of all <see cref="TermState"/>
        ///         instances passed to <see cref="Register(TermState, int, int, long)"/>. </summary>
        /// <returns> the accumulated term frequency of all <see cref="TermState"/>
        ///         instances passed to <see cref="Register(TermState, int, int, long)"/>. </returns>
        public long TotalTermFreq => totalTermFreq;

        /// <summary>
        /// expert: only available for queries that want to lie about docfreq
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public int DocFreq
        {
            get => docFreq;
            internal set => this.docFreq = value;
        }
    }
}