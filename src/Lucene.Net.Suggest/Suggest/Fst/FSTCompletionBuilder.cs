using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;

namespace Lucene.Net.Search.Suggest.Fst
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

    /// <summary>
    /// Finite state automata based implementation of "autocomplete" functionality.
    /// 
    /// <h2>Implementation details</h2>
    /// 
    /// <para>
    /// The construction step in the object finalizer works as follows:
    /// <list type="bullet">
    /// <item><description>A set of input terms and their buckets is given.</description></item>
    /// <item><description>All terms in the input are prefixed with a synthetic pseudo-character
    /// (code) of the weight bucket the term fell into. For example a term
    /// <c>abc</c> with a discretized weight equal '1' would become
    /// <c>1abc</c>.</description></item>
    /// <item><description>The terms are then sorted by their raw value of UTF-8 character values
    /// (including the synthetic bucket code in front).</description></item>
    /// <item><description>A finite state automaton (<see cref="FST"/>) is constructed from the input. The
    /// root node has arcs labeled with all possible weights. We cache all these
    /// arcs, highest-weight first.</description></item>
    /// </list>
    /// 
    /// </para>
    /// <para>
    /// At runtime, in <see cref="FSTCompletion.DoLookup(string, int)"/>, 
    /// the automaton is utilized as follows:
    /// <list type="bullet">
    /// <item><description>For each possible term weight encoded in the automaton (cached arcs from
    /// the root above), starting with the highest one, we descend along the path of
    /// the input key. If the key is not a prefix of a sequence in the automaton
    /// (path ends prematurely), we exit immediately -- no completions.</description></item>
    /// <item><description>Otherwise, we have found an internal automaton node that ends the key.
    /// <b>The entire subautomaton (all paths) starting from this node form the key's
    /// completions.</b> We start the traversal of this subautomaton. Every time we
    /// reach a final state (arc), we add a single suggestion to the list of results
    /// (the weight of this suggestion is constant and equal to the root path we
    /// started from). The tricky part is that because automaton edges are sorted and
    /// we scan depth-first, we can terminate the entire procedure as soon as we
    /// collect enough suggestions the user requested.</description></item>
    /// <item><description>In case the number of suggestions collected in the step above is still
    /// insufficient, we proceed to the next (smaller) weight leaving the root node
    /// and repeat the same algorithm again.</description></item>
    /// </list>
    /// 
    /// <h2>Runtime behavior and performance characteristic</h2>
    /// 
    /// The algorithm described above is optimized for finding suggestions to short
    /// prefixes in a top-weights-first order. This is probably the most common use
    /// case: it allows presenting suggestions early and sorts them by the global
    /// frequency (and then alphabetically).
    /// 
    /// </para>
    /// <para>
    /// If there is an exact match in the automaton, it is returned first on the
    /// results list (even with by-weight sorting).
    /// 
    /// </para>
    /// <para>
    /// Note that the maximum lookup time for <b>any prefix</b> is the time of
    /// descending to the subtree, plus traversal of the subtree up to the number of
    /// requested suggestions (because they are already presorted by weight on the
    /// root level and alphabetically at any node level).
    /// 
    /// </para>
    /// <para>
    /// To order alphabetically only (no ordering by priorities), use identical term
    /// weights for all terms. Alphabetical suggestions are returned even if
    /// non-constant weights are used, but the algorithm for doing this is
    /// suboptimal.
    /// 
    /// </para>
    /// <para>
    /// "alphabetically" in any of the documentation above indicates UTF-8
    /// representation order, nothing else.
    /// 
    /// </para>
    /// <para>
    /// <b>NOTE</b>: the FST file format is experimental and subject to suddenly
    /// change, requiring you to rebuild the FST suggest index.
    /// 
    /// </para>
    /// </summary>
    /// <seealso cref="FSTCompletion"/>
    /// @lucene.experimental
    public class FSTCompletionBuilder
    {
        /// <summary>
        /// Default number of buckets.
        /// </summary>
        public const int DEFAULT_BUCKETS = 10;

        /// <summary>
        /// The number of separate buckets for weights (discretization). The more
        /// buckets, the more fine-grained term weights (priorities) can be assigned.
        /// The speed of lookup will not decrease for prefixes which have
        /// highly-weighted completions (because these are filled-in first), but will
        /// decrease significantly for low-weighted terms (but these should be
        /// infrequent, so it is all right).
        /// 
        /// <para>
        /// The number of buckets must be within [1, 255] range.
        /// </para>
        /// </summary>
        private readonly int buckets;

        /// <summary>
        /// Finite state automaton encoding all the lookup terms. See class notes for
        /// details.
        /// </summary>
        internal FST<object> automaton;

        /// <summary>
        /// FST construction require re-sorting the input. This is the class that
        /// collects all the input entries, their weights and then provides sorted
        /// order.
        /// </summary>
        private readonly IBytesRefSorter sorter;

        /// <summary>
        /// Scratch buffer for <see cref="Add(BytesRef, int)"/>.
        /// </summary>
        private readonly BytesRef scratch = new BytesRef();

        /// <summary>
        /// Max tail sharing length.
        /// </summary>
        private readonly int shareMaxTailLength;

        /// <summary>
        /// Creates an <see cref="FSTCompletion"/> with default options: 10 buckets, exact match
        /// promoted to first position and <see cref="InMemorySorter"/> with a comparer obtained from
        /// <see cref="BytesRef.UTF8SortedAsUnicodeComparer"/>.
        /// </summary>
        public FSTCompletionBuilder()
            : this(DEFAULT_BUCKETS, new InMemorySorter(BytesRef.UTF8SortedAsUnicodeComparer), int.MaxValue)
        {
        }

        /// <summary>
        /// Creates an FSTCompletion with the specified options. </summary>
        /// <param name="buckets">
        ///          The number of buckets for weight discretization. Buckets are used
        ///          in <see cref="Add(BytesRef, int)"/> and must be smaller than the number
        ///          given here.
        /// </param>
        /// <param name="sorter">
        ///          <see cref="IBytesRefSorter"/> used for re-sorting input for the automaton.
        ///          For large inputs, use on-disk sorting implementations. The sorter
        ///          is closed automatically in <see cref="Build()"/> if it implements
        ///          <see cref="IDisposable"/>.
        /// </param>
        /// <param name="shareMaxTailLength">
        ///          Max shared suffix sharing length.
        ///          
        ///          See the description of this parameter in <see cref="Builder"/>'s constructor.
        ///          In general, for very large inputs you'll want to construct a non-minimal
        ///          automaton which will be larger, but the construction will take far less ram.
        ///          For minimal automata, set it to <see cref="int.MaxValue"/>. </param>
        public FSTCompletionBuilder(int buckets, IBytesRefSorter sorter, int shareMaxTailLength)
        {
            if (buckets < 1 || buckets > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(buckets), buckets, "Buckets must be >= 1 and <= 255"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.sorter = sorter ?? throw new ArgumentNullException(nameof(sorter), "BytesRefSorter must not be null."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.buckets = buckets;
            this.shareMaxTailLength = shareMaxTailLength;
        }

        /// <summary>
        /// Appends a single suggestion and its weight to the internal buffers.
        /// </summary>
        /// <param name="utf8">
        ///          The suggestion (utf8 representation) to be added. The content is
        ///          copied and the object can be reused. </param>
        /// <param name="bucket">
        ///          The bucket to place this suggestion in. Must be non-negative and
        ///          smaller than the number of buckets passed in the constructor.
        ///          Higher numbers indicate suggestions that should be presented
        ///          before suggestions placed in smaller buckets. </param>
        public virtual void Add(BytesRef utf8, int bucket)
        {
            // LUCENENET: Added guard clause for null
            if (utf8 is null)
                throw new ArgumentNullException(nameof(utf8));

            if (bucket < 0 || bucket >= buckets)
            {
                throw new ArgumentOutOfRangeException(nameof(buckets), "Bucket outside of the allowed range [0, " + buckets + "): " + bucket); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            if (scratch.Bytes.Length < utf8.Length + 1)
            {
                scratch.Grow(utf8.Length + 10);
            }

            scratch.Length = 1;
            scratch.Bytes[0] = (byte)bucket;
            scratch.Append(utf8);
            sorter.Add(scratch);
        }

        /// <summary>
        /// Builds the final automaton from a list of added entries. This method may
        /// take a longer while as it needs to build the automaton.
        /// </summary>
        public virtual FSTCompletion Build()
        {
            this.automaton = BuildAutomaton(sorter);

            // Dispose of it if it is a disposable
            using (sorter as IDisposable)
            {

            }

            return new FSTCompletion(automaton);
        }

        /// <summary>
        /// Builds the final automaton from a list of entries.
        /// </summary>
        private FST<object> BuildAutomaton(IBytesRefSorter sorter)
        {
            // Build the automaton.
            Outputs<object> outputs = NoOutputs.Singleton;
            object empty = outputs.NoOutput;
            Builder<object> builder = new Builder<object>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, shareMaxTailLength, outputs, null, false, PackedInt32s.DEFAULT, true, 15);

            BytesRef scratch = new BytesRef();
            BytesRef entry;
            Int32sRef scratchIntsRef = new Int32sRef();
            int count = 0;
            IBytesRefEnumerator iter = sorter.GetEnumerator();
            while (iter.MoveNext())
            {
                entry = iter.Current;
                count++;
                if (scratch.CompareTo(entry) != 0)
                {
                    builder.Add(Util.Fst.Util.ToInt32sRef(entry, scratchIntsRef), empty);
                    scratch.CopyBytes(entry);
                }
            }

            return count == 0 ? null : builder.Finish();
        }
    }
}