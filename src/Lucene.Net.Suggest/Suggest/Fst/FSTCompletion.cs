using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

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
    /// <seealso cref=FSTCompletionBuilder
    /// @lucene.experimental </seealso>

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
            public readonly BytesRef utf8;
            /// <summary>
            /// source bucket (weight) of the suggestion </summary>
            public readonly int bucket;

            internal Completion(BytesRef key, int bucket)
            {
                this.utf8 = BytesRef.DeepCopyOf(key);
                this.bucket = bucket;
            }

            public override string ToString()
            {
                return utf8.Utf8ToString() + "/" + bucket;
            }

            /// <seealso cref= BytesRef#compareTo(BytesRef) </seealso>
            public int CompareTo(Completion o)
            {
                return this.utf8.CompareTo(o.utf8);
            }
        }

        /// <summary>
        /// Default number of buckets.
        /// </summary>
        public const int DEFAULT_BUCKETS = 10;

        /// <summary>
        /// An empty result. Keep this an <seealso cref="ArrayList"/> to keep all the returned
        /// lists of single type (monomorphic calls).
        /// </summary>
        private static readonly List<Completion> EMPTY_RESULT = new List<Completion>();

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

        /// <seealso cref= #FSTCompletion(FST, boolean, boolean) </seealso>
        private readonly bool exactFirst;

        /// <seealso cref= #FSTCompletion(FST, boolean, boolean) </seealso>
        private readonly bool higherWeightsFirst;

        /// <summary>
        /// Constructs an FSTCompletion, specifying higherWeightsFirst and exactFirst. </summary>
        /// <param name="automaton">
        ///          Automaton with completions. See <seealso cref="FSTCompletionBuilder"/>. </param>
        /// <param name="exactFirst">
        ///          Return most popular suggestions first. This is the default
        ///          behavior for this implementation. Setting it to <code>false</code>
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
                this.rootArcs = new FST.Arc[0];
            }
            this.higherWeightsFirst = higherWeightsFirst;
            this.exactFirst = exactFirst;
        }

        /// <summary>
        /// Defaults to higher weights first and exact first. </summary>
        /// <seealso cref= #FSTCompletion(FST, boolean, boolean) </seealso>
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
                IList<FST.Arc<object>> rootArcs = new List<FST.Arc<object>>();
                FST.Arc<object> arc = automaton.GetFirstArc(new FST.Arc<object>());
                FST.BytesReader fstReader = automaton.BytesReader;
                automaton.ReadFirstTargetArc(arc, arc, fstReader);
                while (true)
                {
                    rootArcs.Add((new FST.Arc<>()).copyFrom(arc));
                    if (arc.Last)
                    {
                        break;
                    }
                    automaton.ReadNextArc(arc, fstReader);
                }

                rootArcs.Reverse(); // we want highest weights first.
                return rootArcs.ToArray();
            }
            catch (IOException e)
            {
                throw new Exception(e);
            }
        }

        /// <summary>
        /// Returns the first exact match by traversing root arcs, starting from the
        /// arc <code>rootArcIndex</code>.
        /// </summary>
        /// <param name="rootArcIndex">
        ///          The first root arc index in <seealso cref="#rootArcs"/> to consider when
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
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Object> scratch = new org.apache.lucene.util.fst.FST.Arc<>();
                FST.Arc<object> scratch = new FST.Arc<object>();
                FST.BytesReader fstReader = automaton.BytesReader;
                for (; rootArcIndex < rootArcs.Length; rootArcIndex++)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Object> rootArc = rootArcs[rootArcIndex];
                    FST.Arc<object> rootArc = rootArcs[rootArcIndex];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Object> arc = scratch.copyFrom(rootArc);
                    FST.Arc<object> arc = scratch.CopyFrom(rootArc);

                    // Descend into the automaton using the key as prefix.
                    if (descendWithPrefix(arc, utf8))
                    {
                        automaton.ReadFirstTargetArc(arc, arc, fstReader);
                        if (arc.Label == FST.END_LABEL)
                        {
                            // Normalize prefix-encoded weight.
                            return rootArc.Label;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                // Should never happen, but anyway.
                throw new Exception(e);
            }

            // No match.
            return -1;
        }

        /// <summary>
        /// Lookup suggestions to <code>key</code>.
        /// </summary>
        /// <param name="key">
        ///          The prefix to which suggestions should be sought. </param>
        /// <param name="num">
        ///          At most this number of suggestions will be returned. </param>
        /// <returns> Returns the suggestions, sorted by their approximated weight first
        ///         (decreasing) and then alphabetically (UTF-8 codepoint order). </returns>
        public virtual IList<Completion> Lookup(string key, int num)
        {
            if (key.Length == 0 || automaton == null)
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
            catch (IOException e)
            {
                // Should never happen, but anyway.
                throw new Exception(e);
            }
        }

        /// <summary>
        /// Lookup suggestions sorted alphabetically <b>if weights are not
        /// constant</b>. This is a workaround: in general, use constant weights for
        /// alphabetically sorted result.
        /// </summary>
        private IList<Completion> LookupSortedAlphabetically(BytesRef key, int num)
        {
            // Greedily get num results from each weight branch.
            var res = LookupSortedByWeight(key, num, true);

            // Sort and trim.
            res.Sort();
            if (res.Count > num)
            {
                res = res.SubList(0, num);
            }
            return res;
        }

        /// <summary>
        /// Lookup suggestions sorted by weight (descending order).
        /// </summary>
        /// <param name="collectAll">
        ///          If <code>true</code>, the routine terminates immediately when
        ///          <code>num</code> suggestions have been collected. If
        ///          <code>false</code>, it will collect suggestions from all weight
        ///          arcs (needed for <seealso cref="#lookupSortedAlphabetically"/>. </param>
        private List<Completion> LookupSortedByWeight(BytesRef key, int num, bool collectAll)
        {
            // Don't overallocate the results buffers. This also serves the purpose of
            // allowing the user of this class to request all matches using Integer.MAX_VALUE as
            // the number of results.
            List<Completion> res = new List<Completion>(Math.Min(10, num));

            BytesRef output = BytesRef.DeepCopyOf(key);
            for (int i = 0; i < rootArcs.Length; i++)
            {
                FST.Arc<object> rootArc = rootArcs[i];
                FST.Arc<object> arc = (new FST.Arc<object>()).CopyFrom(rootArc);

                // Descend into the automaton using the key as prefix.
                if (descendWithPrefix(arc, key))
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
                                        res.Remove(res.Count - 1);
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
        /// <seealso cref="Suggest.Lookup.LookupResult"/>s already has a
        /// <code>key</code>. If so, reorders that
        /// <seealso cref="Suggest.Lookup.LookupResult"/> to the first
        /// position.
        /// </summary>
        /// <returns> Returns <code>true<code> if and only if <code>list</code> contained
        ///         <code>key</code>. </returns>
        private bool CheckExistingAndReorder(List<Completion> list, BytesRef key)
        {
            // We assume list does not have duplicates (because of how the FST is created).
            for (int i = list.Count; --i >= 0; )
            {
                if (key.Equals(list[i].utf8))
                {
                    // Key found. Unless already at i==0, remove it and push up front so
                    // that the ordering
                    // remains identical with the exception of the exact match.
                    list.Insert(0, list.Remove(i));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Descend along the path starting at <code>arc</code> and going through bytes
        /// in the argument.
        /// </summary>
        /// <param name="arc">
        ///          The starting arc. This argument is modified in-place. </param>
        /// <param name="utf8">
        ///          The term to descend along. </param>
        /// <returns> If <code>true</code>, <code>arc</code> will be set to the arc
        ///         matching last byte of <code>term</code>. <code>false</code> is
        ///         returned if no such prefix exists. </returns>
        private bool descendWithPrefix(FST.Arc<object> arc, BytesRef utf8)
        {
            int max = utf8.Offset + utf8.Length;
            // Cannot save as instance var since multiple threads
            // can use FSTCompletion at once...
            FST.BytesReader fstReader = automaton.BytesReader;
            for (int i = utf8.Offset; i < max; i++)
            {
                if (automaton.FindTargetArc(utf8.Bytes[i] & 0xff, arc, arc, fstReader) == null)
                {
                    // No matching prefixes, return an empty result.
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Recursive collect lookup results from the automaton subgraph starting at
        /// <code>arc</code>.
        /// </summary>
        /// <param name="num">
        ///          Maximum number of results needed (early termination). </param>
        private bool Collect(IList<Completion> res, int num, int bucket, BytesRef output, FST.Arc<object> arc)
        {
            if (output.Length == output.Bytes.Length)
            {
                output.Bytes = ArrayUtil.Grow(output.Bytes);
            }
            Debug.Assert(output.Offset == 0);
            output.Bytes[output.Length++] = (sbyte) arc.Label;
            FST.BytesReader fstReader = automaton.BytesReader;
            automaton.ReadFirstTargetArc(arc, arc, fstReader);
            while (true)
            {
                if (arc.Label == FST.END_LABEL)
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
                    if (Collect(res, num, bucket, output, (new FST.Arc<>()).copyFrom(arc)))
                    {
                        return true;
                    }
                    output.Length = save;
                }

                if (arc.Last)
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
        public virtual int BucketCount
        {
            get
            {
                return rootArcs.Length;
            }
        }

        /// <summary>
        /// Returns the bucket assigned to a given key (if found) or <code>-1</code> if
        /// no exact match exists.
        /// </summary>
        public virtual int GetBucket(string key)
        {
            return GetExactMatchStartingFromRootArc(0, new BytesRef(key));
        }

        /// <summary>
        /// Returns the internal automaton.
        /// </summary>
        public virtual FST<object> FST
        {
            get
            {
                return automaton;
            }
        }
    }
}