using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    /// </summary>
    /// <seealso cref="FSTCompletionBuilder"/>
    /// @lucene.experimental

    // TODO: we could store exact weights as outputs from the FST (int4 encoded
    // floats). This would provide exact outputs from this method and to some
    // degree allowed post-sorting on a more fine-grained weight.

    // TODO: support for Analyzers (infix suggestions, synonyms?)

    public class FSTCompletion
    {
        /// <summary>
        /// A single completion for a given key.
        /// </summary>
        public sealed class Completion : IComparable<Completion>
        {
            /// <summary>
            /// UTF-8 bytes of the suggestion </summary>
            public BytesRef Utf8 { get; private set; }
            /// <summary>
            /// source bucket (weight) of the suggestion </summary>
            public int Bucket { get; private set; }

            internal Completion(BytesRef key, int bucket)
            {
                this.Utf8 = BytesRef.DeepCopyOf(key);
                this.Bucket = bucket;
            }

            public override string ToString()
            {
                return Utf8.Utf8ToString() + "/" + Bucket.ToString("0.0", CultureInfo.InvariantCulture);
            }

            /// <seealso cref="BytesRef.CompareTo(object)"></seealso>
            public int CompareTo(Completion o)
            {
                return this.Utf8.CompareTo(o.Utf8);
            }
        }

        /// <summary>
        /// Default number of buckets.
        /// </summary>
        public const int DEFAULT_BUCKETS = 10;

        /// <summary>
        /// An empty result. Keep this an <see cref="List{T}"/> to keep all the returned
        /// lists of single type (monomorphic calls).
        /// </summary>
        private static readonly IList<Completion> EMPTY_RESULT = new JCG.List<Completion>();

        /// <summary>
        /// Finite state automaton encoding all the lookup terms. See class notes for
        /// details.
        /// </summary>
        private readonly FST<object> automaton;

        /// <summary>
        /// An array of arcs leaving the root automaton state and encoding weights of
        /// all completions in their sub-trees.
        /// </summary>
        private readonly FST.Arc<object>[] rootArcs;

        /// <seealso cref="FSTCompletion(FST{object}, bool, bool)" />
        private readonly bool exactFirst;

        /// <seealso cref="FSTCompletion(FST{object}, bool, bool)" />
        private readonly bool higherWeightsFirst;

        /// <summary>
        /// Constructs an FSTCompletion, specifying higherWeightsFirst and exactFirst. </summary>
        /// <param name="automaton">
        ///          Automaton with completions. See <see cref="FSTCompletionBuilder"/>. </param>
        /// <param name="higherWeightsFirst">
        ///          Return most popular suggestions first. This is the default
        ///          behavior for this implementation. Setting it to <c>false</c>
        ///          has no effect (use constant term weights to sort alphabetically
        ///          only). </param>
        /// <param name="exactFirst">
        ///          Find and push an exact match to the first position of the result
        ///          list if found. </param>
        public FSTCompletion(FST<object> automaton, bool higherWeightsFirst, bool exactFirst)
        {
            this.automaton = automaton;
            if (automaton != null)
            {
                this.rootArcs = CacheRootArcs(automaton);
            }
            else
            {
                this.rootArcs = Arrays.Empty<FST.Arc<object>>();
            }
            this.higherWeightsFirst = higherWeightsFirst;
            this.exactFirst = exactFirst;
        }

        /// <summary>
        /// Defaults to higher weights first and exact first. </summary>
        /// <seealso cref="FSTCompletion(FST{object}, bool, bool)"/>
        public FSTCompletion(FST<object> automaton)
            : this(automaton, true, true)
        {
        }

        /// <summary>
        /// Cache the root node's output arcs starting with completions with the
        /// highest weights.
        /// </summary>
        private static FST.Arc<object>[] CacheRootArcs(FST<object> automaton)
        {
            try
            {
                // LUCENENET specific: Using a stack rather than List, as we want the results in reverse
                Stack<FST.Arc<object>> rootArcs = new Stack<FST.Arc<object>>();
                FST.Arc<object> arc = automaton.GetFirstArc(new FST.Arc<object>());
                FST.BytesReader fstReader = automaton.GetBytesReader();
                automaton.ReadFirstTargetArc(arc, arc, fstReader);
                while (true)
                {
                    rootArcs.Push(new FST.Arc<object>().CopyFrom(arc));
                    if (arc.IsLast)
                    {
                        break;
                    }
                    automaton.ReadNextArc(arc, fstReader);
                }

                // we want highest weights first.
                return rootArcs.ToArray();
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        /// <summary>
        /// Returns the first exact match by traversing root arcs, starting from the
        /// arc <paramref name="rootArcIndex"/>.
        /// </summary>
        /// <param name="rootArcIndex">
        ///          The first root arc index in <see cref="rootArcs"/> to consider when
        ///          matching.
        /// </param>
        /// <param name="utf8">
        ///          The sequence of utf8 bytes to follow.
        /// </param>
        /// <returns> Returns the bucket number of the match or <code>-1</code> if no
        ///         match was found. </returns>
        private int GetExactMatchStartingFromRootArc(int rootArcIndex, BytesRef utf8)
        {
            // Get the UTF-8 bytes representation of the input key.
            try
            {
                FST.Arc<object> scratch = new FST.Arc<object>();
                FST.BytesReader fstReader = automaton.GetBytesReader();
                for (; rootArcIndex < rootArcs.Length; rootArcIndex++)
                {
                    FST.Arc<object> rootArc = rootArcs[rootArcIndex];
                    FST.Arc<object> arc = scratch.CopyFrom(rootArc);

                    // Descend into the automaton using the key as prefix.
                    if (DescendWithPrefix(arc, utf8))
                    {
                        automaton.ReadFirstTargetArc(arc, arc, fstReader);
                        if (arc.Label == Lucene.Net.Util.Fst.FST.END_LABEL)
                        {
                            // Normalize prefix-encoded weight.
                            return rootArc.Label;
                        }
                    }
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                // Should never happen, but anyway.
                throw RuntimeException.Create(e);
            }

            // No match.
            return -1;
        }

        /// <summary>
        /// Lookup suggestions to <paramref name="key"/>.
        /// </summary>
        /// <param name="key">
        ///          The prefix to which suggestions should be sought. </param>
        /// <param name="num">
        ///          At most this number of suggestions will be returned. </param>
        /// <returns> Returns the suggestions, sorted by their approximated weight first
        ///         (decreasing) and then alphabetically (UTF-8 codepoint order). </returns>
        public virtual IList<Completion> DoLookup(string key, int num)
        {
            // LUCENENET: Added guard clause for null
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (key.Length == 0 || automaton is null)
            {
                return EMPTY_RESULT;
            }

            try
            {
                var keyUtf8 = new BytesRef(key);
                if (!higherWeightsFirst && rootArcs.Length > 1)
                {
                    // We could emit a warning here (?). An optimal strategy for
                    // alphabetically sorted
                    // suggestions would be to add them with a constant weight -- this saves
                    // unnecessary
                    // traversals and sorting.
                    return LookupSortedAlphabetically(keyUtf8, num);
                }
                else
                {
                    return LookupSortedByWeight(keyUtf8, num, false);
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                // Should never happen, but anyway.
                throw RuntimeException.Create(e);
            }
        }

        /// <summary>
        /// Lookup suggestions sorted alphabetically <c>if weights are not
        /// constant</c>. This is a workaround: in general, use constant weights for
        /// alphabetically sorted result.
        /// </summary>
        private JCG.List<Completion> LookupSortedAlphabetically(BytesRef key, int num)
        {
            // Greedily get num results from each weight branch.
            var res = LookupSortedByWeight(key, num, true);

            // Sort and trim.
            res.Sort();
            if (res.Count > num)
            {
                res = res.GetView(0, num - 0); // LUCENENET: Converted end index to length
            }
            return res;
        }

        /// <summary>
        /// Lookup suggestions sorted by weight (descending order).
        /// </summary>
        /// <param name="collectAll">
        ///          If <c>true</c>, the routine terminates immediately when
        ///          <paramref name="num"/> suggestions have been collected. If
        ///          <c>false</c>, it will collect suggestions from all weight
        ///          arcs (needed for <see cref="LookupSortedAlphabetically"/>. </param>
        private JCG.List<Completion> LookupSortedByWeight(BytesRef key, int num, bool collectAll)
        {
            // Don't overallocate the results buffers. This also serves the purpose of
            // allowing the user of this class to request all matches using Integer.MAX_VALUE as
            // the number of results.
            JCG.List<Completion> res = new JCG.List<Completion>(Math.Min(10, num));

            BytesRef output = BytesRef.DeepCopyOf(key);
            for (int i = 0; i < rootArcs.Length; i++)
            {
                FST.Arc<object> rootArc = rootArcs[i];
                FST.Arc<object> arc = (new FST.Arc<object>()).CopyFrom(rootArc);

                // Descend into the automaton using the key as prefix.
                if (DescendWithPrefix(arc, key))
                {
                    // A subgraph starting from the current node has the completions
                    // of the key prefix. The arc we're at is the last key's byte,
                    // so we will collect it too.
                    output.Length = key.Length - 1;
                    if (Collect(res, num, rootArc.Label, output, arc) && !collectAll)
                    {
                        // We have enough suggestions to return immediately. Keep on looking
                        // for an
                        // exact match, if requested.
                        if (exactFirst)
                        {
                            if (!CheckExistingAndReorder(res, key))
                            {
                                int exactMatchBucket = GetExactMatchStartingFromRootArc(i, key);
                                if (exactMatchBucket != -1)
                                {
                                    // Insert as the first result and truncate at num.
                                    while (res.Count >= num)
                                    {
                                        res.RemoveAt(res.Count - 1);
                                    }
                                    res.Insert(0, new Completion(key, exactMatchBucket));
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Checks if the list of
        /// <see cref="Lookup.LookupResult"/>s already has a
        /// <paramref name="key"/>. If so, reorders that
        /// <see cref="Lookup.LookupResult"/> to the first
        /// position.
        /// </summary>
        /// <returns> 
        /// Returns <c>true</c> if and only if <paramref name="list"/> contained
        /// <paramref name="key"/>.
        /// </returns>
        private static bool CheckExistingAndReorder(IList<Completion> list, BytesRef key) // LUCENENET: CA1822: Mark members as static
        {
            // We assume list does not have duplicates (because of how the FST is created).
            for (int i = list.Count; --i >= 0; )
            {
                if (key.Equals(list[i].Utf8))
                {
                    // Key found. Unless already at i==0, remove it and push up front so
                    // that the ordering
                    // remains identical with the exception of the exact match.
                    if (key.Equals(list[i].Utf8))
                    {
                        var element = list[i];
                        list.Remove(element);
                        list.Insert(0, element);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Descend along the path starting at <paramref name="arc"/> and going through bytes
        /// in the argument.
        /// </summary>
        /// <param name="arc">
        ///          The starting arc. This argument is modified in-place. </param>
        /// <param name="utf8">
        ///          The term to descend along. </param>
        /// <returns> If <c>true</c>, <paramref name="arc"/> will be set to the arc
        ///         matching last byte of <c>term</c>. <c>false</c> is
        ///         returned if no such prefix exists. </returns>
        private bool DescendWithPrefix(FST.Arc<object> arc, BytesRef utf8)
        {
            int max = utf8.Offset + utf8.Length;
            // Cannot save as instance var since multiple threads
            // can use FSTCompletion at once...
            FST.BytesReader fstReader = automaton.GetBytesReader();
            for (int i = utf8.Offset; i < max; i++)
            {
                if (automaton.FindTargetArc(utf8.Bytes[i] & 0xff, arc, arc, fstReader) is null)
                {
                    // No matching prefixes, return an empty result.
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Recursive collect lookup results from the automaton subgraph starting at
        /// <paramref name="arc"/>.
        /// </summary>
        /// <param name="num">
        ///          Maximum number of results needed (early termination). </param>
        private bool Collect(IList<Completion> res, int num, int bucket, BytesRef output, FST.Arc<object> arc)
        {
            if (output.Length == output.Bytes.Length)
            {
                output.Bytes = ArrayUtil.Grow(output.Bytes);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(output.Offset == 0);
            output.Bytes[output.Length++] = (byte) arc.Label;
            FST.BytesReader fstReader = automaton.GetBytesReader();
            automaton.ReadFirstTargetArc(arc, arc, fstReader);
            while (true)
            {
                if (arc.Label == Lucene.Net.Util.Fst.FST.END_LABEL)
                {
                    res.Add(new Completion(output, bucket));
                    if (res.Count >= num)
                    {
                        return true;
                    }
                }
                else
                {
                    int save = output.Length;
                    if (Collect(res, num, bucket, output, (new FST.Arc<object>()).CopyFrom(arc)))
                    {
                        return true;
                    }
                    output.Length = save;
                }

                if (arc.IsLast)
                {
                    break;
                }
                automaton.ReadNextArc(arc, fstReader);
            }
            return false;
        }

        /// <summary>
        /// Returns the bucket count (discretization thresholds).
        /// </summary>
        public virtual int BucketCount => rootArcs.Length;

        /// <summary>
        /// Returns the bucket assigned to a given key (if found) or <c>-1</c> if
        /// no exact match exists.
        /// </summary>
        public virtual int GetBucket(string key)
        {
            return GetExactMatchStartingFromRootArc(0, new BytesRef(key));
        }

        /// <summary>
        /// Returns the internal automaton.
        /// </summary>
        public virtual FST<object> FST => automaton;
    }
}