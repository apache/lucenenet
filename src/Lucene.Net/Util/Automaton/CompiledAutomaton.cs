using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util.Automaton
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

    using PrefixTermsEnum = Lucene.Net.Search.PrefixTermsEnum;
    using SingleTermsEnum = Lucene.Net.Index.SingleTermsEnum;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Immutable class holding compiled details for a given
    /// <see cref="Automaton"/>.  The <see cref="Automaton"/> is deterministic, must not have
    /// dead states but is not necessarily minimal.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class CompiledAutomaton
    {
        /// <summary>
        /// Automata are compiled into different internal forms for the
        /// most efficient execution depending upon the language they accept.
        /// </summary>
        public enum AUTOMATON_TYPE
        {
            /// <summary>
            /// Automaton that accepts no strings. </summary>
            NONE,

            /// <summary>
            /// Automaton that accepts all possible strings. </summary>
            ALL,

            /// <summary>
            /// Automaton that accepts only a single fixed string. </summary>
            SINGLE,

            /// <summary>
            /// Automaton that matches all strings with a constant prefix. </summary>
            PREFIX,

            /// <summary>
            /// Catch-all for any other automata. </summary>
            NORMAL
        }

        public AUTOMATON_TYPE Type { get; private set; }

        /// <summary>
        /// For <see cref="AUTOMATON_TYPE.PREFIX"/>, this is the prefix term;
        /// for <see cref="AUTOMATON_TYPE.SINGLE"/> this is the singleton term.
        /// </summary>
        public BytesRef Term { get; private set; }

        /// <summary>
        /// Matcher for quickly determining if a <see cref="T:byte[]"/> is accepted.
        /// only valid for <see cref="AUTOMATON_TYPE.NORMAL"/>.
        /// </summary>
        public ByteRunAutomaton RunAutomaton { get; private set; }

        // TODO: would be nice if these sortedTransitions had "int
        // to;" instead of "State to;" somehow:
        /// <summary>
        /// Two dimensional array of transitions, indexed by state
        /// number for traversal. The state numbering is consistent with
        /// <see cref="RunAutomaton"/>.
        /// <para/>
        /// Only valid for <see cref="AUTOMATON_TYPE.NORMAL"/>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public Transition[][] SortedTransitions => sortedTransitions;

        private readonly Transition[][] sortedTransitions;

        /// <summary>
        /// Shared common suffix accepted by the automaton. Only valid
        /// for <see cref="AUTOMATON_TYPE.NORMAL"/>, and only when the
        /// automaton accepts an infinite language.
        /// </summary>
        public BytesRef CommonSuffixRef { get; private set; }

        /// <summary>
        /// Indicates if the automaton accepts a finite set of strings.
        /// Null if this was not computed.
        /// Only valid for <see cref="AUTOMATON_TYPE.NORMAL"/>.
        /// </summary>
        public bool? Finite { get; private set; }

        public CompiledAutomaton(Automaton automaton)
            : this(automaton, null, true)
        {
        }

        public CompiledAutomaton(Automaton automaton, bool? finite, bool simplify)
        {
            if (simplify)
            {
                // Test whether the automaton is a "simple" form and
                // if so, don't create a runAutomaton.  Note that on a
                // large automaton these tests could be costly:
                if (BasicOperations.IsEmpty(automaton))
                {
                    // matches nothing
                    Type = AUTOMATON_TYPE.NONE;
                    Term = null;
                    CommonSuffixRef = null;
                    RunAutomaton = null;
                    sortedTransitions = null;
                    this.Finite = null;
                    return;
                }
                else if (BasicOperations.IsTotal(automaton))
                {
                    // matches all possible strings
                    Type = AUTOMATON_TYPE.ALL;
                    Term = null;
                    CommonSuffixRef = null;
                    RunAutomaton = null;
                    sortedTransitions = null;
                    this.Finite = null;
                    return;
                }
                else
                {
                    string commonPrefix;
                    string singleton;
                    if (automaton.Singleton is null)
                    {
                        commonPrefix = SpecialOperations.GetCommonPrefix(automaton);
                        if (commonPrefix.Length > 0 && BasicOperations.SameLanguage(automaton, BasicAutomata.MakeString(commonPrefix)))
                        {
                            singleton = commonPrefix;
                        }
                        else
                        {
                            singleton = null;
                        }
                    }
                    else
                    {
                        commonPrefix = null;
                        singleton = automaton.Singleton;
                    }

                    if (singleton != null)
                    {
                        // matches a fixed string in singleton or expanded
                        // representation
                        Type = AUTOMATON_TYPE.SINGLE;
                        Term = new BytesRef(singleton);
                        CommonSuffixRef = null;
                        RunAutomaton = null;
                        sortedTransitions = null;
                        this.Finite = null;
                        return;
                    }
                    else if (BasicOperations.SameLanguage(automaton, BasicOperations.Concatenate(BasicAutomata.MakeString(commonPrefix), BasicAutomata.MakeAnyString())))
                    {
                        // matches a constant prefix
                        Type = AUTOMATON_TYPE.PREFIX;
                        Term = new BytesRef(commonPrefix);
                        CommonSuffixRef = null;
                        RunAutomaton = null;
                        sortedTransitions = null;
                        this.Finite = null;
                        return;
                    }
                }
            }

            Type = AUTOMATON_TYPE.NORMAL;
            Term = null;
            if (finite is null)
            {
                this.Finite = SpecialOperations.IsFinite(automaton);
            }
            else
            {
                this.Finite = finite;
            }
            Automaton utf8 = (new UTF32ToUTF8()).Convert(automaton);
            if (this.Finite == true)
            {
                CommonSuffixRef = null;
            }
            else
            {
                CommonSuffixRef = SpecialOperations.GetCommonSuffixBytesRef(utf8);
            }
            RunAutomaton = new ByteRunAutomaton(utf8, true);
            sortedTransitions = utf8.GetSortedTransitions();
        }

        //private static final boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BytesRef AddTail(int state, BytesRef term, int idx, int leadLabel)
        {
            // Find biggest transition that's < label
            // TODO: use binary search here
            Transition maxTransition = null;
            foreach (Transition transition in sortedTransitions[state])
            {
                if (transition.min < leadLabel)
                {
                    maxTransition = transition;
                }
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(maxTransition != null);

            // Append floorLabel
            int floorLabel;
            if (maxTransition.max > leadLabel - 1)
            {
                floorLabel = leadLabel - 1;
            }
            else
            {
                floorLabel = maxTransition.max;
            }
            if (idx >= term.Bytes.Length)
            {
                term.Grow(1 + idx);
            }
            //if (DEBUG) System.out.println("  add floorLabel=" + (char) floorLabel + " idx=" + idx);
            term.Bytes[idx] = (byte)floorLabel;

            state = maxTransition.to.Number;
            idx++;

            // Push down to last accept state
            while (true)
            {
                Transition[] transitions = sortedTransitions[state];
                if (transitions.Length == 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(RunAutomaton.IsAccept(state));
                    term.Length = idx;
                    //if (DEBUG) System.out.println("  return " + term.utf8ToString());
                    return term;
                }
                else
                {
                    // We are pushing "top" -- so get last label of
                    // last transition:
                    if (Debugging.AssertsEnabled) Debugging.Assert(transitions.Length != 0);
                    Transition lastTransition = transitions[transitions.Length - 1];
                    if (idx >= term.Bytes.Length)
                    {
                        term.Grow(1 + idx);
                    }
                    //if (DEBUG) System.out.println("  push maxLabel=" + (char) lastTransition.max + " idx=" + idx);
                    term.Bytes[idx] = (byte)lastTransition.max;
                    state = lastTransition.to.Number;
                    idx++;
                }
            }
        }

        // TODO: should this take startTerm too?  this way
        // Terms.intersect could forward to this method if type !=
        // NORMAL:
        public virtual TermsEnum GetTermsEnum(Terms terms)
        {
            return Type switch
            {
                AUTOMATON_TYPE.NONE => TermsEnum.EMPTY,
                AUTOMATON_TYPE.ALL => terms.GetEnumerator(),
                AUTOMATON_TYPE.SINGLE => new SingleTermsEnum(terms.GetEnumerator(), Term),
                AUTOMATON_TYPE.PREFIX => new PrefixTermsEnum(terms.GetEnumerator(), Term),// TODO: this is very likely faster than .intersect,
                                                                                            // but we should test and maybe cutover
                AUTOMATON_TYPE.NORMAL => terms.Intersect(this, null),
                _ => throw RuntimeException.Create("unhandled case"),// unreachable
            };
        }

        /// <summary>
        /// Finds largest term accepted by this Automaton, that's
        /// &lt;= the provided input term.  The result is placed in
        /// output; it's fine for output and input to point to
        /// the same <see cref="BytesRef"/>.  The returned result is either the
        /// provided output, or <c>null</c> if there is no floor term
        /// (ie, the provided input term is before the first term
        /// accepted by this <see cref="Automaton"/>).
        /// </summary>
        public virtual BytesRef Floor(BytesRef input, BytesRef output)
        {
            output.Offset = 0;
            //if (DEBUG) System.out.println("CA.floor input=" + input.utf8ToString());

            int state = RunAutomaton.InitialState;

            // Special case empty string:
            if (input.Length == 0)
            {
                if (RunAutomaton.IsAccept(state))
                {
                    output.Length = 0;
                    return output;
                }
                else
                {
                    return null;
                }
            }

            IList<int> stack = new JCG.List<int>();

            int idx = 0;
            while (true)
            {
                int label = ((sbyte)input.Bytes[input.Offset + idx]) & 0xff;
                int nextState = RunAutomaton.Step(state, label);
                //if (DEBUG) System.out.println("  cycle label=" + (char) label + " nextState=" + nextState);

                if (idx == input.Length - 1)
                {
                    if (nextState != -1 && RunAutomaton.IsAccept(nextState))
                    {
                        // Input string is accepted
                        if (idx >= output.Bytes.Length)
                        {
                            output.Grow(1 + idx);
                        }
                        output.Bytes[idx] = (byte)label;
                        output.Length = input.Length;
                        //if (DEBUG) System.out.println("  input is accepted; return term=" + output.utf8ToString());
                        return output;
                    }
                    else
                    {
                        nextState = -1;
                    }
                }

                if (nextState == -1)
                {
                    // Pop back to a state that has a transition
                    // <= our label:
                    while (true)
                    {
                        Transition[] transitions = sortedTransitions[state];
                        if (transitions.Length == 0)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(RunAutomaton.IsAccept(state));
                            output.Length = idx;
                            //if (DEBUG) System.out.println("  return " + output.utf8ToString());
                            return output;
                        }
                        else if (label - 1 < transitions[0].min)
                        {
                            if (RunAutomaton.IsAccept(state))
                            {
                                output.Length = idx;
                                //if (DEBUG) System.out.println("  return " + output.utf8ToString());
                                return output;
                            }
                            // pop
                            if (stack.Count == 0)
                            {
                                //if (DEBUG) System.out.println("  pop ord=" + idx + " return null");
                                return null;
                            }
                            else
                            {
                                state = stack[stack.Count - 1];
                                stack.RemoveAt(stack.Count - 1);
                                idx--;
                                //if (DEBUG) System.out.println("  pop ord=" + (idx+1) + " label=" + (char) label + " first trans.min=" + (char) transitions[0].min);
                                label = input.Bytes[input.Offset + idx] & 0xff;
                            }
                        }
                        else
                        {
                            //if (DEBUG) System.out.println("  stop pop ord=" + idx + " first trans.min=" + (char) transitions[0].min);
                            break;
                        }
                    }

                    //if (DEBUG) System.out.println("  label=" + (char) label + " idx=" + idx);

                    return AddTail(state, output, idx, label);
                }
                else
                {
                    if (idx >= output.Bytes.Length)
                    {
                        output.Grow(1 + idx);
                    }
                    output.Bytes[idx] = (byte)label;
                    stack.Add(state);
                    state = nextState;
                    idx++;
                }
            }
        }

        public virtual string ToDot()
        {
            StringBuilder b = new StringBuilder("digraph CompiledAutomaton {\n");
            b.Append("  rankdir = LR;\n");
            int initial = RunAutomaton.InitialState;
            for (int i = 0; i < sortedTransitions.Length; i++)
            {
                b.Append("  ").Append(i);
                if (RunAutomaton.IsAccept(i))
                {
                    b.Append(" [shape=doublecircle,label=\"\"];\n");
                }
                else
                {
                    b.Append(" [shape=circle,label=\"\"];\n");
                }
                if (i == initial)
                {
                    b.Append("  initial [shape=plaintext,label=\"\"];\n");
                    b.Append("  initial -> ").Append(i).Append("\n");
                }
                for (int j = 0; j < sortedTransitions[i].Length; j++)
                {
                    b.Append("  ").Append(i);
                    sortedTransitions[i][j].AppendDot(b);
                }
            }
            return b.Append("}\n").ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((RunAutomaton is null) ? 0 : RunAutomaton.GetHashCode());
            result = prime * result + ((Term is null) ? 0 : Term.GetHashCode());
            result = prime * result + Type.GetHashCode(); //((Type is null) ? 0 : Type.GetHashCode()); // LUCENENET NOTE: Enum cannot be null in .NET
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            CompiledAutomaton other = (CompiledAutomaton)obj;
            if (Type != other.Type)
            {
                return false;
            }
            if (Type == AUTOMATON_TYPE.SINGLE || Type == AUTOMATON_TYPE.PREFIX)
            {
                if (!Term.Equals(other.Term))
                {
                    return false;
                }
            }
            else if (Type == AUTOMATON_TYPE.NORMAL)
            {
                if (!RunAutomaton.Equals(other.RunAutomaton))
                {
                    return false;
                }
            }

            return true;
        }
    }
}