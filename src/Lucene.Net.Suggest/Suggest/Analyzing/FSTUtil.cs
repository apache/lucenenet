using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    public class FSTUtil
    {
        private FSTUtil()
        {
        }

        /// <summary>
        /// Holds a pair (automaton, fst) of states and accumulated output in the intersected machine. </summary>
        public sealed class Path<T>
        {
            /// <summary>
            /// Node in the automaton where path ends: </summary>
            public readonly State state;

            /// <summary>
            /// Node in the <see cref="FST"/> where path ends: </summary>
            public readonly FST.Arc<T> fstNode;

            /// <summary>
            /// Output of the path so far: </summary>
            internal T output;

            /// <summary>
            /// Input of the path so far: </summary>
            public readonly IntsRef input;

            /// <summary>
            /// Sole constructor. </summary>
            public Path(State state, FST.Arc<T> fstNode, T output, IntsRef input)
            {
                this.state = state;
                this.fstNode = fstNode;
                this.output = output;
                this.input = input;
            }
        }

        /// <summary>
        /// Enumerates all minimal prefix paths in the automaton that also intersect the <see cref="FST"/>,
        /// accumulating the <see cref="FST"/> end node and output for each path.
        /// </summary>
        public static List<Path<T>> IntersectPrefixPaths<T>(Automaton a, FST<T> fst)
        {
            Debug.Assert(a.IsDeterministic);
            IList<Path<T>> queue = new List<Path<T>>();
            List<Path<T>> endNodes = new List<Path<T>>();
            queue.Add(new Path<T>(a.GetInitialState(), fst.GetFirstArc(new FST.Arc<T>()), fst.Outputs.NoOutput, new IntsRef()));

            FST.Arc<T> scratchArc = new FST.Arc<T>();
            FST.BytesReader fstReader = fst.BytesReader;

            while (queue.Count != 0)
            {
                Path<T> path = queue.ElementAt(queue.Count - 1);
                queue.Remove(path);
                if (path.state.Accept)
                {
                    endNodes.Add(path);
                    // we can stop here if we accept this path,
                    // we accept all further paths too
                    continue;
                }

                IntsRef currentInput = path.input;
                foreach (Transition t in path.state.GetTransitions())
                {
                    int min = t.Min;
                    int max = t.Max;
                    if (min == max)
                    {
                        FST.Arc<T> nextArc = fst.FindTargetArc(t.Min, path.fstNode, scratchArc, fstReader);
                        if (nextArc != null)
                        {
                            IntsRef newInput = new IntsRef(currentInput.Length + 1);
                            newInput.CopyInts(currentInput);
                            newInput.Ints[currentInput.Length] = t.Min;
                            newInput.Length = currentInput.Length + 1;
                            queue.Add(new Path<T>(t.Dest, new FST.Arc<T>()
                              .CopyFrom(nextArc), fst.Outputs.Add(path.output, nextArc.Output), newInput));
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

                        FST.Arc<T> nextArc = Lucene.Net.Util.Fst.Util.ReadCeilArc(min, fst, path.fstNode, scratchArc, fstReader);
                        while (nextArc != null && nextArc.Label <= max)
                        {
                            Debug.Assert(nextArc.Label <= max);
                            Debug.Assert(nextArc.Label >= min, nextArc.Label + " " + min);
                            IntsRef newInput = new IntsRef(currentInput.Length + 1);
                            newInput.CopyInts(currentInput);
                            newInput.Ints[currentInput.Length] = nextArc.Label;
                            newInput.Length = currentInput.Length + 1;
                            queue.Add(new Path<T>(t.Dest, new FST.Arc<T>()
                              .CopyFrom(nextArc), fst.Outputs.Add(path.output, nextArc.Output), newInput));
                            int label = nextArc.Label; // used in assert
                            nextArc = nextArc.IsLast ? null : fst.ReadNextRealArc(nextArc, fstReader);
                            Debug.Assert(nextArc == null || label < nextArc.Label, "last: " + label + " next: " + (nextArc == null ? "" : nextArc.Label.ToString()));
                        }
                    }
                }
            }
            return endNodes;
        }
    }
}