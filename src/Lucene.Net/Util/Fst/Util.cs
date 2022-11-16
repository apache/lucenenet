using J2N;
using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using BitSet = Lucene.Net.Util.OpenBitSet;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Util.Fst
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
    /// Static helper methods.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public static class Util // LUCENENET specific - made static // LUCENENET TODO: Fix naming conflict with containing namespace
    {
        /// <summary>
        /// Looks up the output for this input, or <c>null</c> if the
        /// input is not accepted.
        /// </summary>
        public static T Get<T>(FST<T> fst, Int32sRef input) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            // TODO: would be nice not to alloc this on every lookup
            var arc = fst.GetFirstArc(new FST.Arc<T>());

            var fstReader = fst.GetBytesReader();

            // Accumulate output as we go
            T output = fst.Outputs.NoOutput;
            for (int i = 0; i < input.Length; i++)
            {
                if (fst.FindTargetArc(input.Int32s[input.Offset + i], arc, arc, fstReader) is null)
                {
                    return default;
                }
                output = fst.Outputs.Add(output, arc.Output);
            }

            if (arc.IsFinal)
            {
                return fst.Outputs.Add(output, arc.NextFinalOutput);
            }
            else
            {
                return default;
            }
        }

        // TODO: maybe a CharsRef version for BYTE2

        /// <summary>
        /// Looks up the output for this input, or <c>null</c> if the
        /// input is not accepted
        /// </summary>
        public static T Get<T>(FST<T> fst, BytesRef input) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(fst.InputType == FST.INPUT_TYPE.BYTE1);

            var fstReader = fst.GetBytesReader();

            // TODO: would be nice not to alloc this on every lookup
            var arc = fst.GetFirstArc(new FST.Arc<T>());

            // Accumulate output as we go
            T output = fst.Outputs.NoOutput;
            for (int i = 0; i < input.Length; i++)
            {
                if (fst.FindTargetArc(input.Bytes[i + input.Offset] & 0xFF, arc, arc, fstReader) is null)
                {
                    return default;
                }
                output = fst.Outputs.Add(output, arc.Output);
            }

            if (arc.IsFinal)
            {
                return fst.Outputs.Add(output, arc.NextFinalOutput);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Reverse lookup (lookup by output instead of by input),
        /// in the special case when your FSTs outputs are
        /// strictly ascending.  This locates the input/output
        /// pair where the output is equal to the target, and will
        /// return <c>null</c> if that output does not exist.
        ///
        /// <para/>NOTE: this only works with <see cref="FST{T}"/>, only
        /// works when the outputs are ascending in order with
        /// the inputs.
        /// For example, simple ordinals (0, 1,
        /// 2, ...), or file offets (when appending to a file)
        /// fit this.
        /// </summary>
        public static Int32sRef GetByOutput(FST<Int64> fst, long targetOutput)
        {
            var @in = fst.GetBytesReader();

            // TODO: would be nice not to alloc this on every lookup
            FST.Arc<Int64> arc = fst.GetFirstArc(new FST.Arc<Int64>());

            FST.Arc<Int64> scratchArc = new FST.Arc<Int64>();

            Int32sRef result = new Int32sRef();

            return GetByOutput(fst, targetOutput, @in, arc, scratchArc, result);
        }

        /// <summary>
        /// Expert: like <see cref="Util.GetByOutput(FST{Int64}, long)"/> except reusing
        /// <see cref="FST.BytesReader"/>, initial and scratch Arc, and result.
        /// </summary>
        public static Int32sRef GetByOutput(FST<Int64> fst, long targetOutput, FST.BytesReader @in, FST.Arc<Int64> arc, FST.Arc<Int64> scratchArc, Int32sRef result)
        {
            long output = arc.Output;
            int upto = 0;

            //System.out.println("reverseLookup output=" + targetOutput);

            while (true)
            {
                //System.out.println("loop: output=" + output + " upto=" + upto + " arc=" + arc);
                if (arc.IsFinal)
                {
                    long finalOutput = output + arc.NextFinalOutput;
                    //System.out.println("  isFinal finalOutput=" + finalOutput);
                    if (finalOutput == targetOutput)
                    {
                        result.Length = upto;
                        //System.out.println("    found!");
                        return result;
                    }
                    else if (finalOutput > targetOutput)
                    {
                        //System.out.println("    not found!");
                        return null;
                    }
                }

                if (FST<Int64>.TargetHasArcs(arc))
                {
                    //System.out.println("  targetHasArcs");
                    if (result.Int32s.Length == upto)
                    {
                        result.Grow(1 + upto);
                    }

                    fst.ReadFirstRealTargetArc(arc.Target, arc, @in);

                    if (arc.BytesPerArc != 0)
                    {
                        int low = 0;
                        int high = arc.NumArcs - 1;
                        int mid = 0;
                        //System.out.println("bsearch: numArcs=" + arc.numArcs + " target=" + targetOutput + " output=" + output);
                        bool exact = false;
                        while (low <= high)
                        {
                            mid = (low + high).TripleShift(1);
                            @in.Position = arc.PosArcsStart;
                            @in.SkipBytes(arc.BytesPerArc * mid);
                            var flags = (sbyte)@in.ReadByte();
                            fst.ReadLabel(@in);
                            long minArcOutput;
                            if ((flags & FST.BIT_ARC_HAS_OUTPUT) != 0)
                            {
                                long arcOutput = fst.Outputs.Read(@in);
                                minArcOutput = output + arcOutput;
                            }
                            else
                            {
                                minArcOutput = output;
                            }
                            if (minArcOutput == targetOutput)
                            {
                                exact = true;
                                break;
                            }
                            else if (minArcOutput < targetOutput)
                            {
                                low = mid + 1;
                            }
                            else
                            {
                                high = mid - 1;
                            }
                        }

                        if (high == -1)
                        {
                            return null;
                        }
                        else if (exact)
                        {
                            arc.ArcIdx = mid - 1;
                        }
                        else
                        {
                            arc.ArcIdx = low - 2;
                        }

                        fst.ReadNextRealArc(arc, @in);
                        result.Int32s[upto++] = arc.Label;
                        output += arc.Output;
                    }
                    else
                    {
                        FST.Arc<Int64> prevArc = null;

                        while (true)
                        {
                            //System.out.println("    cycle label=" + arc.label + " output=" + arc.output);

                            // this is the min output we'd hit if we follow
                            // this arc:
                            long minArcOutput = output + arc.Output;

                            if (minArcOutput == targetOutput)
                            {
                                // Recurse on this arc:
                                //System.out.println("  match!  break");
                                output = minArcOutput;
                                result.Int32s[upto++] = arc.Label;
                                break;
                            }
                            else if (minArcOutput > targetOutput)
                            {
                                if (prevArc is null)
                                {
                                    // Output doesn't exist
                                    return null;
                                }
                                else
                                {
                                    // Recurse on previous arc:
                                    arc.CopyFrom(prevArc);
                                    result.Int32s[upto++] = arc.Label;
                                    output += arc.Output;
                                    //System.out.println("    recurse prev label=" + (char) arc.label + " output=" + output);
                                    break;
                                }
                            }
                            else if (arc.IsLast)
                            {
                                // Recurse on this arc:
                                output = minArcOutput;
                                //System.out.println("    recurse last label=" + (char) arc.label + " output=" + output);
                                result.Int32s[upto++] = arc.Label;
                                break;
                            }
                            else
                            {
                                // Read next arc in this node:
                                prevArc = scratchArc;
                                prevArc.CopyFrom(arc);
                                //System.out.println("      after copy label=" + (char) prevArc.label + " vs " + (char) arc.label);
                                fst.ReadNextRealArc(arc, @in);
                            }
                        }
                    }
                }
                else
                {
                    //System.out.println("  no target arcs; not found!");
                    return null;
                }
            }
        }

        /// <summary>
        /// Represents a path in TopNSearcher.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public class FSTPath<T> where T : class  // LUCENENET specific - added class constraint, since we compare reference equality
        {
            public FST.Arc<T> Arc { get; set; }
            public T Cost { get; set; }
            public Int32sRef Input { get; private set; }

            /// <summary>
            /// Sole constructor </summary>
            public FSTPath(T cost, FST.Arc<T> arc, Int32sRef input)
            {
                this.Arc = (new FST.Arc<T>()).CopyFrom(arc);
                this.Cost = cost;
                this.Input = input;
            }

            public override string ToString()
            {
                return "input=" + Input + " cost=" + Cost;
            }
        }

        /// <summary>
        /// Compares first by the provided comparer, and then
        /// tie breaks by <see cref="FSTPath{T}.Input"/>.
        /// </summary>
        private class TieBreakByInputComparer<T> : IComparer<FSTPath<T>> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            internal readonly IComparer<T> comparer;

            public TieBreakByInputComparer(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public virtual int Compare(FSTPath<T> a, FSTPath<T> b)
            {
                int cmp = comparer.Compare(a.Cost, b.Cost);
                if (cmp == 0)
                {
                    return a.Input.CompareTo(b.Input);
                }
                else
                {
                    return cmp;
                }
            }
        }

        /// <summary>
        /// Utility class to find top N shortest paths from start
        /// point(s).
        /// </summary>
        public class TopNSearcher<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            private readonly FST<T> fst;
            private readonly FST.BytesReader bytesReader;
            private readonly int topN;
            private readonly int maxQueueDepth;

            private readonly FST.Arc<T> scratchArc = new FST.Arc<T>();

            internal readonly IComparer<T> comparer;

            internal JCG.SortedSet<FSTPath<T>> queue = null;

            /// <summary>
            /// Creates an unbounded TopNSearcher </summary>
            /// <param name="fst"> the <see cref="Lucene.Net.Util.Fst.FST{T}"/> to search on </param>
            /// <param name="topN"> the number of top scoring entries to retrieve </param>
            /// <param name="maxQueueDepth"> the maximum size of the queue of possible top entries </param>
            /// <param name="comparer"> the comparer to select the top N </param>
            public TopNSearcher(FST<T> fst, int topN, int maxQueueDepth, IComparer<T> comparer)
            {
                this.fst = fst;
                this.bytesReader = fst.GetBytesReader();
                this.topN = topN;
                this.maxQueueDepth = maxQueueDepth;
                this.comparer = comparer;

                queue = new JCG.SortedSet<FSTPath<T>>(new TieBreakByInputComparer<T>(comparer));
            }

            /// <summary>
            /// If back plus this arc is competitive then add to queue:
            /// </summary>
            protected virtual void AddIfCompetitive(FSTPath<T> path)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(queue != null);

                T cost = fst.Outputs.Add(path.Cost, path.Arc.Output);
                //System.out.println("  addIfCompetitive queue.size()=" + queue.size() + " path=" + path + " + label=" + path.arc.label);

                if (queue.Count == maxQueueDepth)
                {
                    FSTPath<T> bottom = queue.Max;
                    int comp = comparer.Compare(cost, bottom.Cost);
                    if (comp > 0)
                    {
                        // Doesn't compete
                        return;
                    }
                    else if (comp == 0)
                    {
                        // Tie break by alpha sort on the input:
                        path.Input.Grow(path.Input.Length + 1);
                        path.Input.Int32s[path.Input.Length++] = path.Arc.Label;
                        int cmp = bottom.Input.CompareTo(path.Input);
                        path.Input.Length--;

                        // We should never see dups:
                        if (Debugging.AssertsEnabled) Debugging.Assert(cmp != 0);

                        if (cmp < 0)
                        {
                            // Doesn't compete
                            return;
                        }
                    }
                    // Competes
                }
                else
                {
                    // Queue isn't full yet, so any path we hit competes:
                }

                // copy over the current input to the new input
                // and add the arc.label to the end
                Int32sRef newInput = new Int32sRef(path.Input.Length + 1);
                Arrays.Copy(path.Input.Int32s, 0, newInput.Int32s, 0, path.Input.Length);
                newInput.Int32s[path.Input.Length] = path.Arc.Label;
                newInput.Length = path.Input.Length + 1;
                FSTPath<T> newPath = new FSTPath<T>(cost, path.Arc, newInput);

                queue.Add(newPath);

                if (queue.Count == maxQueueDepth + 1)
                {
                    queue.Remove(queue.Max);
                }
            }

            /// <summary>
            /// Adds all leaving arcs, including 'finished' arc, if
            /// the node is final, from this node into the queue.
            /// </summary>
            public virtual void AddStartPaths(FST.Arc<T> node, T startOutput, bool allowEmptyString, Int32sRef input)
            {
                // De-dup NO_OUTPUT since it must be a singleton:
                if (startOutput.Equals(fst.Outputs.NoOutput))
                {
                    startOutput = fst.Outputs.NoOutput;
                }

                FSTPath<T> path = new FSTPath<T>(startOutput, node, input);
                fst.ReadFirstTargetArc(node, path.Arc, bytesReader);

                //System.out.println("add start paths");

                // Bootstrap: find the min starting arc
                while (true)
                {
                    if (allowEmptyString || path.Arc.Label != FST.END_LABEL)
                    {
                        AddIfCompetitive(path);
                    }
                    if (path.Arc.IsLast)
                    {
                        break;
                    }
                    fst.ReadNextArc(path.Arc, bytesReader);
                }
            }

            public virtual TopResults<T> Search()
            {
                IList<Result<T>> results = new JCG.List<Result<T>>();

                //System.out.println("search topN=" + topN);

                var fstReader = fst.GetBytesReader();
                T NO_OUTPUT = fst.Outputs.NoOutput;

                // TODO: we could enable FST to sorting arcs by weight
                // as it freezes... can easily do this on first pass
                // (w/o requiring rewrite)

                // TODO: maybe we should make an FST.INPUT_TYPE.BYTE0.5!?
                // (nibbles)
                int rejectCount = 0;

                // For each top N path:
                while (results.Count < topN)
                {
                    //System.out.println("\nfind next path: queue.size=" + queue.size());

                    FSTPath<T> path;

                    if (queue is null)
                    {
                        // Ran out of paths
                        //System.out.println("  break queue=null");
                        break;
                    }

                    // Remove top path since we are now going to
                    // pursue it:
                    path = queue.Min;
                    if (path != null)
                    {
                        queue.Remove(path);
                    }
                    else
                    {
                        // There were less than topN paths available:
                        //System.out.println("  break no more paths");
                        break;
                    }

                    if (path.Arc.Label == FST.END_LABEL)
                    {
                        //System.out.println("    empty string!  cost=" + path.cost);
                        // Empty string!
                        path.Input.Length--;
                        results.Add(new Result<T>(path.Input, path.Cost));
                        continue;
                    }

                    if (results.Count == topN - 1 && maxQueueDepth == topN)
                    {
                        // Last path -- don't bother w/ queue anymore:
                        queue = null;
                    }

                    //System.out.println("  path: " + path);

                    // We take path and find its "0 output completion",
                    // ie, just keep traversing the first arc with
                    // NO_OUTPUT that we can find, since this must lead
                    // to the minimum path that completes from
                    // path.arc.

                    // For each input letter:
                    while (true)
                    {
                        //System.out.println("\n    cycle path: " + path);
                        fst.ReadFirstTargetArc(path.Arc, path.Arc, fstReader);

                        // For each arc leaving this node:
                        bool foundZero = false;
                        while (true)
                        {
                            //System.out.println("      arc=" + (char) path.arc.label + " cost=" + path.arc.output);
                            // tricky: instead of comparing output == 0, we must
                            // express it via the comparer compare(output, 0) == 0
                            if (comparer.Compare(NO_OUTPUT, path.Arc.Output) == 0)
                            {
                                if (queue is null)
                                {
                                    foundZero = true;
                                    break;
                                }
                                else if (!foundZero)
                                {
                                    scratchArc.CopyFrom(path.Arc);
                                    foundZero = true;
                                }
                                else
                                {
                                    AddIfCompetitive(path);
                                }
                            }
                            else if (queue != null)
                            {
                                AddIfCompetitive(path);
                            }
                            if (path.Arc.IsLast)
                            {
                                break;
                            }
                            fst.ReadNextArc(path.Arc, fstReader);
                        }

                        if (Debugging.AssertsEnabled) Debugging.Assert(foundZero);

                        if (queue != null)
                        {
                            // TODO: maybe we can save this copyFrom if we
                            // are more clever above... eg on finding the
                            // first NO_OUTPUT arc we'd switch to using
                            // scratchArc
                            path.Arc.CopyFrom(scratchArc);
                        }

                        if (path.Arc.Label == FST.END_LABEL)
                        {
                            // Add final output:
                            //Debug.WriteLine("    done!: " + path);
                            T finalOutput = fst.Outputs.Add(path.Cost, path.Arc.Output);
                            if (AcceptResult(path.Input, finalOutput))
                            {
                                //Debug.WriteLine("    add result: " + path);
                                results.Add(new Result<T>(path.Input, finalOutput));
                            }
                            else
                            {
                                rejectCount++;
                            }
                            break;
                        }
                        else
                        {
                            path.Input.Grow(1 + path.Input.Length);
                            path.Input.Int32s[path.Input.Length] = path.Arc.Label;
                            path.Input.Length++;
                            path.Cost = fst.Outputs.Add(path.Cost, path.Arc.Output);
                        }
                    }
                }
                return new TopResults<T>(rejectCount + topN <= maxQueueDepth, results);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected virtual bool AcceptResult(Int32sRef input, T output)
            {
                return true;
            }
        }

        /// <summary>
        /// Holds a single input (<see cref="Int32sRef"/>) + output, returned by
        /// <see cref="ShortestPaths"/>.
        /// </summary>
        public sealed class Result<T> where T : class // LUCENENET specific - added class constraint because we compare reference equality
        {
            public Int32sRef Input { get; private set; }
            public T Output { get; private set; }

            public Result(Int32sRef input, T output)
            {
                this.Input = input;
                this.Output = output;
            }
        }

        /// <summary>
        /// Holds the results for a top N search using <see cref="TopNSearcher{T}"/>
        /// </summary>
        public sealed class TopResults<T> : IEnumerable<Result<T>> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            /// <summary>
            /// <c>true</c> iff this is a complete result ie. if
            /// the specified queue size was large enough to find the complete list of results. this might
            /// be <c>false</c> if the <see cref="TopNSearcher{T}"/> rejected too many results.
            /// </summary>
            public bool IsComplete { get; private set; }

            /// <summary>
            /// The top results
            /// </summary>
            public IList<Result<T>> TopN { get; private set; }

            internal TopResults(bool isComplete, IList<Result<T>> topN)
            {
                this.TopN = topN;
                this.IsComplete = isComplete;
            }

            public IEnumerator<Result<T>> GetEnumerator()
            {
                return TopN.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Starting from node, find the top N min cost
        /// completions to a final node.
        /// </summary>
        public static TopResults<T> ShortestPaths<T>(FST<T> fst, FST.Arc<T> fromNode, T startOutput, IComparer<T> comparer, int topN, bool allowEmptyString)
            where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            // All paths are kept, so we can pass topN for
            // maxQueueDepth and the pruning is admissible:
            TopNSearcher<T> searcher = new TopNSearcher<T>(fst, topN, topN, comparer);

            // since this search is initialized with a single start node
            // it is okay to start with an empty input path here
            searcher.AddStartPaths(fromNode, startOutput, allowEmptyString, new Int32sRef());
            return searcher.Search();
        }

        /// <summary>
        /// Dumps an <see cref="FST{T}"/> to a GraphViz's <c>dot</c> language description
        /// for visualization. Example of use:
        ///
        /// <code>
        /// using (TextWriter sw = new StreamWriter(&quot;out.dot&quot;))
        /// {
        ///     Util.ToDot(fst, sw, true, true);
        /// }
        /// </code>
        ///
        /// and then, from command line:
        ///
        /// <code>
        /// dot -Tpng -o out.png out.dot
        /// </code>
        ///
        /// <para/>
        /// Note: larger FSTs (a few thousand nodes) won't even
        /// render, don't bother.  If the FST is &gt; 2.1 GB in size
        /// then this method will throw strange exceptions.
        /// <para/>
        /// See also <a href="http://www.graphviz.org/">http://www.graphviz.org/</a>.
        /// </summary>
        /// <param name="sameRank">
        ///          If <c>true</c>, the resulting <c>dot</c> file will try
        ///          to order states in layers of breadth-first traversal. This may
        ///          mess up arcs, but makes the output FST's structure a bit clearer.
        /// </param>
        /// <param name="labelStates">
        ///          If <c>true</c> states will have labels equal to their offsets in their
        ///          binary format. Expands the graph considerably.
        /// </param>
        public static void ToDot<T>(FST<T> fst, TextWriter @out, bool sameRank, bool labelStates) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            const string expandedNodeColor = "blue";

            // this is the start arc in the automaton (from the epsilon state to the first state
            // with outgoing transitions.
            FST.Arc<T> startArc = fst.GetFirstArc(new FST.Arc<T>());

            // A queue of transitions to consider for the next level.
            IList<FST.Arc<T>> thisLevelQueue = new JCG.List<FST.Arc<T>>();

            // A queue of transitions to consider when processing the next level.
            IList<FST.Arc<T>> nextLevelQueue = new JCG.List<FST.Arc<T>>
            {
                startArc
            };
            //System.out.println("toDot: startArc: " + startArc);

            // A list of states on the same level (for ranking).
            IList<int> sameLevelStates = new JCG.List<int>();

            // A bitset of already seen states (target offset).
            BitSet seen = new BitSet();
            seen.Set((int)startArc.Target);

            // Shape for states.
            const string stateShape = "circle";
            const string finalStateShape = "doublecircle";

            // Emit DOT prologue.
            @out.Write("digraph FST {\n");
            @out.Write("  rankdir = LR; splines=true; concentrate=true; ordering=out; ranksep=2.5; \n");

            if (!labelStates)
            {
                @out.Write("  node [shape=circle, width=.2, height=.2, style=filled]\n");
            }

            EmitDotState(@out, "initial", "point", "white", "");

            T NO_OUTPUT = fst.Outputs.NoOutput;
            var r = fst.GetBytesReader();

            // final FST.Arc<T> scratchArc = new FST.Arc<>();

            {
                string stateColor;
                if (fst.IsExpandedTarget(startArc, r))
                {
                    stateColor = expandedNodeColor;
                }
                else
                {
                    stateColor = null;
                }

                bool isFinal;
                T finalOutput;
                if (startArc.IsFinal)
                {
                    isFinal = true;
                    finalOutput = startArc.NextFinalOutput == NO_OUTPUT ? null : startArc.NextFinalOutput;
                }
                else
                {
                    isFinal = false;
                    finalOutput = null;
                }

                EmitDotState(@out, Convert.ToString(startArc.Target), isFinal ? finalStateShape : stateShape, stateColor, finalOutput is null ? "" : fst.Outputs.OutputToString(finalOutput));
            }

            @out.Write("  initial -> " + startArc.Target + "\n");

            int level = 0;

            while (nextLevelQueue.Count > 0)
            {
                // we could double buffer here, but it doesn't matter probably.
                //System.out.println("next level=" + level);
                thisLevelQueue.AddRange(nextLevelQueue);
                nextLevelQueue.Clear();

                level++;
                @out.Write("\n  // Transitions and states at level: " + level + "\n");
                while (thisLevelQueue.Count > 0)
                {
                    FST.Arc<T> arc = thisLevelQueue[thisLevelQueue.Count - 1];
                    thisLevelQueue.RemoveAt(thisLevelQueue.Count - 1);
                    //System.out.println("  pop: " + arc);
                    if (FST<T>.TargetHasArcs(arc))
                    {
                        // scan all target arcs
                        //System.out.println("  readFirstTarget...");

                        long node = arc.Target;

                        fst.ReadFirstRealTargetArc(arc.Target, arc, r);

                        //System.out.println("    firstTarget: " + arc);

                        while (true)
                        {
                            //System.out.println("  cycle arc=" + arc);
                            // Emit the unseen state and add it to the queue for the next level.
                            if (arc.Target >= 0 && !seen.Get((int)arc.Target))
                            {
                                /*
                                boolean isFinal = false;
                                T finalOutput = null;
                                fst.readFirstTargetArc(arc, scratchArc);
                                if (scratchArc.isFinal() && fst.targetHasArcs(scratchArc)) {
                                  // target is final
                                  isFinal = true;
                                  finalOutput = scratchArc.output == NO_OUTPUT ? null : scratchArc.output;
                                  System.out.println("dot hit final label=" + (char) scratchArc.label);
                                }
                                */
                                string stateColor;
                                if (fst.IsExpandedTarget(arc, r))
                                {
                                    stateColor = expandedNodeColor;
                                }
                                else
                                {
                                    stateColor = null;
                                }

                                string finalOutput;
                                if (arc.NextFinalOutput != null && arc.NextFinalOutput != NO_OUTPUT)
                                {
                                    finalOutput = fst.Outputs.OutputToString(arc.NextFinalOutput);
                                }
                                else
                                {
                                    finalOutput = "";
                                }

                                EmitDotState(@out, Convert.ToString(arc.Target, CultureInfo.InvariantCulture), stateShape, stateColor, finalOutput);
                                // To see the node address, use this instead:
                                //emitDotState(out, Integer.toString(arc.target), stateShape, stateColor, String.valueOf(arc.target));
                                seen.Set((int)arc.Target);
                                nextLevelQueue.Add((new FST.Arc<T>()).CopyFrom(arc));
                                sameLevelStates.Add((int)arc.Target);
                            }

                            string outs;
                            if (arc.Output != NO_OUTPUT)
                            {
                                outs = "/" + fst.Outputs.OutputToString(arc.Output);
                            }
                            else
                            {
                                outs = "";
                            }

                            if (!FST<T>.TargetHasArcs(arc) && arc.IsFinal && arc.NextFinalOutput != NO_OUTPUT)
                            {
                                // Tricky special case: sometimes, due to
                                // pruning, the builder can [sillily] produce
                                // an FST with an arc into the final end state
                                // (-1) but also with a next final output; in
                                // this case we pull that output up onto this
                                // arc
                                outs = outs + "/[" + fst.Outputs.OutputToString(arc.NextFinalOutput) + "]";
                            }

                            string arcColor;
                            if (arc.Flag(FST.BIT_TARGET_NEXT))
                            {
                                arcColor = "red";
                            }
                            else
                            {
                                arcColor = "black";
                            }

                            if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label != FST.END_LABEL);
                            @out.Write("  " + node + " -> " + arc.Target + " [label=\"" + PrintableLabel(arc.Label) + outs + "\"" + (arc.IsFinal ? " style=\"bold\"" : "") + " color=\"" + arcColor + "\"]\n");

                            // Break the loop if we're on the last arc of this state.
                            if (arc.IsLast)
                            {
                                //System.out.println("    break");
                                break;
                            }
                            fst.ReadNextRealArc(arc, r);
                        }
                    }
                }

                // Emit state ranking information.
                if (sameRank && sameLevelStates.Count > 1)
                {
                    @out.Write("  {rank=same; ");
                    foreach (int state in sameLevelStates)
                    {
                        @out.Write(state + "; ");
                    }
                    @out.Write(" }\n");
                }
                sameLevelStates.Clear();
            }

            // Emit terminating state (always there anyway).
            @out.Write("  -1 [style=filled, color=black, shape=doublecircle, label=\"\"]\n\n");
            @out.Write("  {rank=sink; -1 }\n");

            @out.Write("}\n");
            @out.Flush();
        }

        /// <summary>
        /// Emit a single state in the <c>dot</c> language.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EmitDotState(TextWriter @out, string name, string shape, string color, string label)
        {
            @out.Write("  " + name 
                + " [" 
                + (shape != null ? "shape=" + shape : "") + " " 
                + (color != null ? "color=" + color : "") + " " 
                + (label != null ? "label=\"" + label + "\"" : "label=\"\"") + " " 
                + "]\n");
        }

        /// <summary>
        /// Ensures an arc's label is indeed printable (dot uses US-ASCII).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string PrintableLabel(int label)
        {
            // Any ordinary ascii character, except for " or \, are
            // printed as the character; else, as a hex string:
            if (label >= 0x20 && label <= 0x7d && label != 0x22 && label != 0x5c) // " OR \
            {
                return char.ToString((char)label);
            }
            return "0x" + label.ToString("x", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Just maps each UTF16 unit (char) to the <see cref="int"/>s in an
        /// <see cref="Int32sRef"/>.
        /// </summary>
        public static Int32sRef ToUTF16(string s, Int32sRef scratch)
        {
            int charLimit = s.Length;
            scratch.Offset = 0;
            scratch.Length = charLimit;
            scratch.Grow(charLimit);
            for (int idx = 0; idx < charLimit; idx++)
            {
                scratch.Int32s[idx] = (int)s[idx];
            }
            return scratch;
        }

        /// <summary>
        /// Decodes the Unicode codepoints from the provided
        /// <see cref="ICharSequence"/> and places them in the provided scratch
        /// <see cref="Int32sRef"/>, which must not be <c>null</c>, returning it.
        /// </summary>
        public static Int32sRef ToUTF32(string s, Int32sRef scratch)
        {
            int charIdx = 0;
            int intIdx = 0;
            int charLimit = s.Length;
            while (charIdx < charLimit)
            {
                scratch.Grow(intIdx + 1);
                int utf32 = Character.CodePointAt(s, charIdx);
                scratch.Int32s[intIdx] = utf32;
                charIdx += Character.CharCount(utf32);
                intIdx++;
            }
            scratch.Length = intIdx;
            return scratch;
        }

        /// <summary>
        /// Decodes the Unicode codepoints from the provided
        /// <see cref="T:char[]"/> and places them in the provided scratch
        /// <see cref="Int32sRef"/>, which must not be <c>null</c>, returning it.
        /// </summary>
        public static Int32sRef ToUTF32(char[] s, int offset, int length, Int32sRef scratch)
        {
            int charIdx = offset;
            int intIdx = 0;
            int charLimit = offset + length;
            while (charIdx < charLimit)
            {
                scratch.Grow(intIdx + 1);
                int utf32 = Character.CodePointAt(s, charIdx, charLimit);
                scratch.Int32s[intIdx] = utf32;
                charIdx += Character.CharCount(utf32);
                intIdx++;
            }
            scratch.Length = intIdx;
            return scratch;
        }

        /// <summary>
        /// Just takes unsigned byte values from the <see cref="BytesRef"/> and
        /// converts into an <see cref="Int32sRef"/>.
        /// <para/>
        /// NOTE: This was toIntsRef() in Lucene
        /// </summary>
        public static Int32sRef ToInt32sRef(BytesRef input, Int32sRef scratch)
        {
            scratch.Grow(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                scratch.Int32s[i] = input.Bytes[i + input.Offset] & 0xFF;
            }
            scratch.Length = input.Length;
            return scratch;
        }

        /// <summary>
        /// Just converts <see cref="Int32sRef"/> to <see cref="BytesRef"/>; you must ensure the
        /// <see cref="int"/> values fit into a <see cref="byte"/>.
        /// </summary>
        public static BytesRef ToBytesRef(Int32sRef input, BytesRef scratch)
        {
            scratch.Grow(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                int value = input.Int32s[i + input.Offset];
                // NOTE: we allow -128 to 255
                if (Debugging.AssertsEnabled) Debugging.Assert(value >= sbyte.MinValue && value <= 255, "value {0} doesn't fit into byte", value);
                scratch.Bytes[i] = (byte)value;
            }
            scratch.Length = input.Length;
            return scratch;
        }

        // Uncomment for debugging:

        /*
        public static <T> void dotToFile(FST<T> fst, String filePath) throws IOException {
          Writer w = new OutputStreamWriter(new FileOutputStream(filePath));
          toDot(fst, w, true, true);
          w.Dispose();
        }
        */

        /// <summary>
        /// Reads the first arc greater or equal that the given label into the provided
        /// arc in place and returns it iff found, otherwise return <c>null</c>.
        /// </summary>
        /// <param name="label"> the label to ceil on </param>
        /// <param name="fst"> the fst to operate on </param>
        /// <param name="follow"> the arc to follow reading the label from </param>
        /// <param name="arc"> the arc to read into in place </param>
        /// <param name="in"> the fst's <see cref="FST.BytesReader"/> </param>
        public static FST.Arc<T> ReadCeilArc<T>(int label, FST<T> fst, FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            // TODO maybe this is a useful in the FST class - we could simplify some other code like FSTEnum?
            if (label == FST.END_LABEL)
            {
                if (follow.IsFinal)
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = (sbyte)FST.BIT_LAST_ARC;
                    }
                    else
                    {
                        arc.Flags = 0;
                        // NOTE: nextArc is a node (not an address!) in this case:
                        arc.NextArc = follow.Target;
                        arc.Node = follow.Target;
                    }
                    arc.Output = follow.NextFinalOutput;
                    arc.Label = FST.END_LABEL;
                    return arc;
                }
                else
                {
                    return null;
                }
            }

            if (!FST<T>.TargetHasArcs(follow))
            {
                return null;
            }
            fst.ReadFirstTargetArc(follow, arc, @in);
            if (arc.BytesPerArc != 0 && arc.Label != FST.END_LABEL)
            {
                // Arcs are fixed array -- use binary search to find
                // the target.

                int low = arc.ArcIdx;
                int high = arc.NumArcs - 1;
                int mid /*= 0*/; // LUCENENET: Removed unnecessary assignment
                // System.out.println("do arc array low=" + low + " high=" + high +
                // " targetLabel=" + targetLabel);
                while (low <= high)
                {
                    mid = (low + high).TripleShift(1);
                    @in.Position = arc.PosArcsStart;
                    @in.SkipBytes(arc.BytesPerArc * mid + 1);
                    int midLabel = fst.ReadLabel(@in);
                    int cmp = midLabel - label;
                    // System.out.println("  cycle low=" + low + " high=" + high + " mid=" +
                    // mid + " midLabel=" + midLabel + " cmp=" + cmp);
                    if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        arc.ArcIdx = mid - 1;
                        return fst.ReadNextRealArc(arc, @in);
                    }
                }
                if (low == arc.NumArcs)
                {
                    // DEAD END!
                    return null;
                }

                arc.ArcIdx = (low > high ? high : low);
                return fst.ReadNextRealArc(arc, @in);
            }

            // Linear scan
            fst.ReadFirstRealTargetArc(follow.Target, arc, @in);

            while (true)
            {
                // System.out.println("  non-bs cycle");
                // TODO: we should fix this code to not have to create
                // object for the output of every arc we scan... only
                // for the matching arc, if found
                if (arc.Label >= label)
                {
                    // System.out.println("    found!");
                    return arc;
                }
                else if (arc.IsLast)
                {
                    return null;
                }
                else
                {
                    fst.ReadNextRealArc(arc, @in);
                }
            }
        }
    }
}