using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class CompiledAutomaton
    {
        public enum AUTOMATON_TYPE
        {
            /** Automaton that accepts no strings. */
            NONE,
            /** Automaton that accepts all possible strings. */
            ALL,
            /** Automaton that accepts only a single fixed string. */
            SINGLE,
            /** Automaton that matches all Strings with a constant prefix. */
            PREFIX,
            /** Catch-all for any other automata. */
            NORMAL
        }

        public readonly AUTOMATON_TYPE type;

        public readonly BytesRef term;

        public readonly ByteRunAutomaton runAutomaton;

        public readonly Transition[][] sortedTransitions;

        public readonly BytesRef commonSuffixRef;

        public readonly bool? finite;

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
                    type = AUTOMATON_TYPE.NONE;
                    term = null;
                    commonSuffixRef = null;
                    runAutomaton = null;
                    sortedTransitions = null;
                    this.finite = null;
                    return;
                }
                else if (BasicOperations.IsTotal(automaton))
                {
                    // matches all possible strings
                    type = AUTOMATON_TYPE.ALL;
                    term = null;
                    commonSuffixRef = null;
                    runAutomaton = null;
                    sortedTransitions = null;
                    this.finite = null;
                    return;
                }
                else
                {
                    String commonPrefix;
                    String singleton;
                    if (automaton.Singleton == null)
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
                        type = AUTOMATON_TYPE.SINGLE;
                        term = new BytesRef(singleton);
                        commonSuffixRef = null;
                        runAutomaton = null;
                        sortedTransitions = null;
                        this.finite = null;
                        return;
                    }
                    else if (BasicOperations.SameLanguage(automaton, BasicOperations.Concatenate(
                            BasicAutomata.MakeString(commonPrefix), BasicAutomata.MakeAnyString())))
                    {
                        // matches a constant prefix
                        type = AUTOMATON_TYPE.PREFIX;
                        term = new BytesRef(commonPrefix);
                        commonSuffixRef = null;
                        runAutomaton = null;
                        sortedTransitions = null;
                        this.finite = null;
                        return;
                    }
                }
            }

            type = AUTOMATON_TYPE.NORMAL;
            term = null;
            if (finite == null)
            {
                this.finite = SpecialOperations.IsFinite(automaton);
            }
            else
            {
                this.finite = finite;
            }
            Automaton utf8 = new UTF32ToUTF8().Convert(automaton);
            if (this.finite.GetValueOrDefault())
            {
                commonSuffixRef = null;
            }
            else
            {
                commonSuffixRef = SpecialOperations.GetCommonSuffixBytesRef(utf8);
            }
            runAutomaton = new ByteRunAutomaton(utf8, true);
            sortedTransitions = utf8.GetSortedTransitions();
        }

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

            //assert maxTransition != null;

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
            if (idx >= term.bytes.Length)
            {
                term.Grow(1 + idx);
            }
            //if (DEBUG) System.out.println("  add floorLabel=" + (char) floorLabel + " idx=" + idx);
            term.bytes[idx] = (sbyte)floorLabel;

            state = maxTransition.to.Number;
            idx++;

            // Push down to last accept state
            while (true)
            {
                Transition[] transitions = sortedTransitions[state];
                if (transitions.Length == 0)
                {
                    //assert runAutomaton.isAccept(state);
                    term.length = idx;
                    //if (DEBUG) System.out.println("  return " + term.utf8ToString());
                    return term;
                }
                else
                {
                    // We are pushing "top" -- so get last label of
                    // last transition:
                    //assert transitions.length != 0;
                    Transition lastTransition = transitions[transitions.Length - 1];
                    if (idx >= term.bytes.Length)
                    {
                        term.Grow(1 + idx);
                    }
                    //if (DEBUG) System.out.println("  push maxLabel=" + (char) lastTransition.max + " idx=" + idx);
                    term.bytes[idx] = (sbyte)lastTransition.max;
                    state = lastTransition.to.Number;
                    idx++;
                }
            }
        }

        public TermsEnum GetTermsEnum(Terms terms)
        {
            switch (type)
            {
                case AUTOMATON_TYPE.NONE:
                    return TermsEnum.EMPTY;
                case AUTOMATON_TYPE.ALL:
                    return terms.Iterator(null);
                case AUTOMATON_TYPE.SINGLE:
                    return new SingleTermsEnum(terms.Iterator(null), term);
                case AUTOMATON_TYPE.PREFIX:
                    // TODO: this is very likely faster than .intersect,
                    // but we should test and maybe cutover
                    return new PrefixTermsEnum(terms.Iterator(null), term);
                case AUTOMATON_TYPE.NORMAL:
                    return terms.Intersect(this, null);
                default:
                    // unreachable
                    throw new Exception("unhandled case");
            }
        }

        public BytesRef Floor(BytesRef input, BytesRef output)
        {
            output.offset = 0;
            //if (DEBUG) System.out.println("CA.floor input=" + input.utf8ToString());

            int state = runAutomaton.InitialState;

            // Special case empty string:
            if (input.length == 0)
            {
                if (runAutomaton.IsAccept(state))
                {
                    output.length = 0;
                    return output;
                }
                else
                {
                    return null;
                }
            }

            List<int> stack = new List<int>();

            int idx = 0;
            while (true)
            {
                int label = input.bytes[input.offset + idx] & 0xff;
                int nextState = runAutomaton.Step(state, label);
                //if (DEBUG) System.out.println("  cycle label=" + (char) label + " nextState=" + nextState);

                if (idx == input.length - 1)
                {
                    if (nextState != -1 && runAutomaton.IsAccept(nextState))
                    {
                        // Input string is accepted
                        if (idx >= output.bytes.Length)
                        {
                            output.Grow(1 + idx);
                        }
                        output.bytes[idx] = (sbyte)label;
                        output.length = input.length;
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
                            //assert runAutomaton.isAccept(state);
                            output.length = idx;
                            //if (DEBUG) System.out.println("  return " + output.utf8ToString());
                            return output;
                        }
                        else if (label - 1 < transitions[0].min)
                        {

                            if (runAutomaton.IsAccept(state))
                            {
                                output.length = idx;
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
                                stack.Remove(stack.Count - 1);
                                idx--;
                                //if (DEBUG) System.out.println("  pop ord=" + (idx+1) + " label=" + (char) label + " first trans.min=" + (char) transitions[0].min);
                                label = input.bytes[input.offset + idx] & 0xff;
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
                    if (idx >= output.bytes.Length)
                    {
                        output.Grow(1 + idx);
                    }
                    output.bytes[idx] = (sbyte)label;
                    stack.Add(state);
                    state = nextState;
                    idx++;
                }
            }
        }
    }
}
