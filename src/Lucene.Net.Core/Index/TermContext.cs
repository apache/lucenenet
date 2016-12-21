using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;

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
    /// Maintains a <seealso cref="IndexReader"/> <seealso cref="TermState"/> view over
    /// <seealso cref="IndexReader"/> instances containing a single term. The
    /// <seealso cref="TermContext"/> doesn't track if the given <seealso cref="TermState"/>
    /// objects are valid, neither if the <seealso cref="TermState"/> instances refer to the
    /// same terms in the associated readers.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class TermContext
    {
        /// <summary>
        /// Holds the <seealso cref="IndexReaderContext"/> of the top-level
        ///  <seealso cref="IndexReader"/>, used internally only for
        ///  asserting.
        ///
        ///  @lucene.internal
        /// </summary>
        public IndexReaderContext TopReaderContext { get; private set; }

        private readonly TermState[] states;
        private int docFreq;
        private long totalTermFreq;

        //public static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        /// <summary>
        /// Creates an empty <seealso cref="TermContext"/> from a <seealso cref="IndexReaderContext"/>
        /// </summary>
        public TermContext(IndexReaderContext context)
        {
            Debug.Assert(context != null && context.IsTopLevel);
            TopReaderContext = context;
            docFreq = 0;
            int len;
            if (context.Leaves == null)
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
        /// Creates a <seealso cref="TermContext"/> with an initial <seealso cref="TermState"/>,
        /// <seealso cref="IndexReader"/> pair.
        /// </summary>
        public TermContext(IndexReaderContext context, TermState state, int ord, int docFreq, long totalTermFreq)
            : this(context)
        {
            Register(state, ord, docFreq, totalTermFreq);
        }

        /// <summary>
        /// Creates a <seealso cref="TermContext"/> from a top-level <seealso cref="IndexReaderContext"/> and the
        /// given <seealso cref="Term"/>. this method will lookup the given term in all context's leaf readers
        /// and register each of the readers containing the term in the returned <seealso cref="TermContext"/>
        /// using the leaf reader's ordinal.
        /// <p>
        /// Note: the given context must be a top-level context.
        /// </summary>
        public static TermContext Build(IndexReaderContext context, Term term)
        {
            Debug.Assert(context != null && context.IsTopLevel);
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
                    Terms terms = fields.Terms(field);
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.Iterator(null);
                        if (termsEnum.SeekExact(bytes))
                        {
                            TermState termState = termsEnum.TermState();
                            //if (DEBUG) System.out.println("    found");
                            perReaderTermState.Register(termState, ctx.Ord, termsEnum.DocFreq(), termsEnum.TotalTermFreq());
                        }
                    }
                }
            }
            return perReaderTermState;
        }

        /// <summary>
        /// Clears the <seealso cref="TermContext"/> internal state and removes all
        /// registered <seealso cref="TermState"/>s
        /// </summary>
        public void Clear()
        {
            docFreq = 0;
            Arrays.Fill(states, null);
        }

        /// <summary>
        /// Registers and associates a <seealso cref="TermState"/> with an leaf ordinal. The leaf ordinal
        /// should be derived from a <seealso cref="IndexReaderContext"/>'s leaf ord.
        /// </summary>
        public void Register(TermState state, int ord, int docFreq, long totalTermFreq)
        {
            Debug.Assert(state != null, "state must not be null");
            Debug.Assert(ord >= 0 && ord < states.Length);
            Debug.Assert(states[ord] == null, "state for ord: " + ord + " already registered");
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
        /// Returns the <seealso cref="TermState"/> for an leaf ordinal or <code>null</code> if no
        /// <seealso cref="TermState"/> for the ordinal was registered.
        /// </summary>
        /// <param name="ord">
        ///          the readers leaf ordinal to get the <seealso cref="TermState"/> for. </param>
        /// <returns> the <seealso cref="TermState"/> for the given readers ord or <code>null</code> if no
        ///         <seealso cref="TermState"/> for the reader was registered </returns>
        public TermState Get(int ord)
        {
            Debug.Assert(ord >= 0 && ord < states.Length);
            return states[ord];
        }

        /// <summary>
        ///  Returns the accumulated term frequency of all <seealso cref="TermState"/>
        ///         instances passed to <seealso cref="#register(TermState, int, int, long)"/>. </summary>
        /// <returns> the accumulated term frequency of all <seealso cref="TermState"/>
        ///         instances passed to <seealso cref="#register(TermState, int, int, long)"/>. </returns>
        public long TotalTermFreq() // LUCENENET TODO: Make property
        {
            return totalTermFreq;
        }

        /// <summary>
        /// expert: only available for queries that want to lie about docfreq
        /// @lucene.internal
        /// </summary>
        public int DocFreq
        {
            set
            {
                this.docFreq = value;
            }
            get
            {
                return docFreq;
            }
        }
    }
}