using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Analyzing
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

    // TODO: move to core?  nobody else uses it yet though...

    /// <summary>
    /// Exposes a utility method to enumerate all paths
    /// intersecting an <see cref="Automaton"/> with an <see cref="FST"/>.
    /// </summary>
    public static class FSTUtil // LUCENENET specific - made static since all members are static
    {
        /// <summary>
        /// Holds a pair (automaton, fst) of states and accumulated output in the intersected machine. </summary>
        public sealed class Path<T> where T : class // LUCENENET specific - added class constraint because we are comparing reference equality
        {
            /// <summary>
            /// Node in the automaton where path ends: </summary>
            public State State { get; private set; }

            /// <summary>
            /// Node in the <see cref="FST"/> where path ends: </summary>
            public FST.Arc<T> FstNode { get; private set; }

            /// <summary>
            /// Output of the path so far: </summary>
            public T Output { get; set; } // LUCENENET NOTE: This was made public in Lucene 5.1, but our users require it now, so we are doing it in 4.8.

            /// <summary>
            /// Input of the path so far: </summary>
            public Int32sRef Input { get; private set; }

            /// <summary>
            /// Sole constructor. </summary>
            public Path(State state, FST.Arc<T> fstNode, T output, Int32sRef input)
            {
                this.State = state;
                this.FstNode = fstNode;
                this.Output = output;
                this.Input = input;
            }
        }

        /// <summary>
        /// Enumerates all minimal prefix paths in the automaton that also intersect the <see cref="FST"/>,
        /// accumulating the <see cref="FST"/> end node and output for each path.
        /// </summary>
        public static IList<Path<T>> IntersectPrefixPaths<T>(Automaton a, FST<T> fst) where T : class // LUCENENET specific - added class constraint because we are comparing reference equality
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(a.IsDeterministic);
            IList<Path<T>> queue = new JCG.List<Path<T>>();
            IList<Path<T>> endNodes = new JCG.List<Path<T>>();
            queue.Add(new Path<T>(a.GetInitialState(), fst.GetFirstArc(new FST.Arc<T>()), fst.Outputs.NoOutput, new Int32sRef()));

            FST.Arc<T> scratchArc = new FST.Arc<T>();
            FST.BytesReader fstReader = fst.GetBytesReader();

            while (queue.Count != 0)
            {
                Path<T> path = queue[queue.Count - 1];
                queue.Remove(path);
                if (path.State.Accept)
                {
                    endNodes.Add(path);
                    // we can stop here if we accept this path,
                    // we accept all further paths too
                    continue;
                }

                Int32sRef currentInput = path.Input;
                foreach (Transition t in path.State.GetTransitions())
                {
                    int min = t.Min;
                    int max = t.Max;
                    if (min == max)
                    {
                        FST.Arc<T> nextArc = fst.FindTargetArc(t.Min, path.FstNode, scratchArc, fstReader);
                        if (nextArc != null)
                        {
                            Int32sRef newInput = new Int32sRef(currentInput.Length + 1);
                            newInput.CopyInt32s(currentInput);
                            newInput.Int32s[currentInput.Length] = t.Min;
                            newInput.Length = currentInput.Length + 1;
                            queue.Add(new Path<T>(t.Dest, new FST.Arc<T>()
                              .CopyFrom(nextArc), fst.Outputs.Add(path.Output, nextArc.Output), newInput));
                        }
                    }
                    else
                    {
                        // TODO: if this transition's TO state is accepting, and
                        // it accepts the entire range possible in the FST (ie. 0 to 255),
                        // we can simply use the prefix as the accepted state instead of
                        // looking up all the ranges and terminate early
                        // here.  This just shifts the work from one queue
                        // (this one) to another (the completion search
                        // done in AnalyzingSuggester).

                        FST.Arc<T> nextArc = Lucene.Net.Util.Fst.Util.ReadCeilArc(min, fst, path.FstNode, scratchArc, fstReader);
                        while (nextArc != null && nextArc.Label <= max)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(nextArc.Label <= max);
                            if (Debugging.AssertsEnabled) Debugging.Assert(nextArc.Label >= min, "{0} {1}", nextArc.Label, min);
                            Int32sRef newInput = new Int32sRef(currentInput.Length + 1);
                            newInput.CopyInt32s(currentInput);
                            newInput.Int32s[currentInput.Length] = nextArc.Label;
                            newInput.Length = currentInput.Length + 1;
                            queue.Add(new Path<T>(t.Dest, new FST.Arc<T>()
                              .CopyFrom(nextArc), fst.Outputs.Add(path.Output, nextArc.Output), newInput));
                            int label = nextArc.Label; // used in assert
                            nextArc = nextArc.IsLast ? null : fst.ReadNextRealArc(nextArc, fstReader);
                            if (Debugging.AssertsEnabled) Debugging.Assert(nextArc is null || label < nextArc.Label, "last: {0} next: {1}", label, nextArc?.Label);
                        }
                    }
                }
            }
            return endNodes;
        }
    }
}