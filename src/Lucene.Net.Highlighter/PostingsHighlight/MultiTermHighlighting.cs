#if FEATURE_BREAKITERATOR
using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// Support for highlighting multiterm queries in PostingsHighlighter.
    /// </summary>
    internal class MultiTermHighlighting
    {
        /// <summary>
        /// Extracts all <see cref="MultiTermQuery"/>s for <paramref name="field"/>, and returns equivalent 
        /// automata that will match terms.
        /// </summary>
        internal static CharacterRunAutomaton[] ExtractAutomata(Query query, string field)
        {
            JCG.List<CharacterRunAutomaton> list = new JCG.List<CharacterRunAutomaton>();
            if (query is BooleanQuery booleanQuery)
            {
                foreach (BooleanClause clause in booleanQuery.GetClauses())
                {
                    if (!clause.IsProhibited)
                    {
                        list.AddRange(ExtractAutomata(clause.Query, field));
                    }
                }
            }
            else if (query is DisjunctionMaxQuery disjunctionMaxQuery)
            {
                foreach (Query sub in disjunctionMaxQuery.Disjuncts)
                {
                    list.AddRange(ExtractAutomata(sub, field));
                }
            }
            else if (query is SpanOrQuery spanOrQuery)
            {
                foreach (Query sub in spanOrQuery.GetClauses())
                {
                    list.AddRange(ExtractAutomata(sub, field));
                }
            }
            else if (query is SpanNearQuery spanNearQuery)
            {
                foreach (Query sub in spanNearQuery.GetClauses())
                {
                    list.AddRange(ExtractAutomata(sub, field));
                }
            }
            else if (query is SpanNotQuery spanNotQuery)
            {
                list.AddRange(ExtractAutomata(spanNotQuery.Include, field));
            }
            else if (query is SpanPositionCheckQuery spanPositionCheckQuery)
            {
                list.AddRange(ExtractAutomata(spanPositionCheckQuery.Match, field));
            }
            else if (query is ISpanMultiTermQueryWrapper spanMultiTermQueryWrapper)
            {
                list.AddRange(ExtractAutomata(spanMultiTermQueryWrapper.WrappedQuery, field));
            }
            else if (query is AutomatonQuery aq)
            {
                if (aq.Field.Equals(field, StringComparison.Ordinal))
                {
                    list.Add(new CharacterRunAutomatonToStringAnonymousClass(aq.Automaton, () => aq.ToString()));
                }
            }
            else if (query is PrefixQuery pq)
            {
                Term prefix = pq.Prefix;
                if (prefix.Field.Equals(field, StringComparison.Ordinal))
                {
                    list.Add(new CharacterRunAutomatonToStringAnonymousClass(
                        BasicOperations.Concatenate(BasicAutomata.MakeString(prefix.Text), BasicAutomata.MakeAnyString()),
                        () => pq.ToString()));
                }
            }
            else if (query is FuzzyQuery fq)
            {
                if (fq.Field.Equals(field, StringComparison.Ordinal))
                {
                    string utf16 = fq.Term.Text;
                    int[] termText = new int[utf16.CodePointCount(0, utf16.Length)];
                    for (int cp, i = 0, j = 0; i < utf16.Length; i += Character.CharCount(cp))
                    {
                        termText[j++] = cp = utf16.CodePointAt(i);
                    }
                    int termLength = termText.Length;
                    int prefixLength = Math.Min(fq.PrefixLength, termLength);
                    string suffix = UnicodeUtil.NewString(termText, prefixLength, termText.Length - prefixLength);
                    LevenshteinAutomata builder = new LevenshteinAutomata(suffix, fq.Transpositions);
                    Automaton automaton = builder.ToAutomaton(fq.MaxEdits);
                    if (prefixLength > 0)
                    {
                        Automaton prefix = BasicAutomata.MakeString(UnicodeUtil.NewString(termText, 0, prefixLength));
                        automaton = BasicOperations.Concatenate(prefix, automaton);
                    }
                    list.Add(new CharacterRunAutomatonToStringAnonymousClass(automaton, () => fq.ToString()));
                }
            }
            else if (query is TermRangeQuery tq)
            {
                if (tq.Field.Equals(field, StringComparison.Ordinal))
                {
                    // this is *not* an automaton, but its very simple
                    list.Add(new SimpleCharacterRunAutomatonAnonymousClass(BasicAutomata.MakeEmpty(), tq));
                }
            }
            return list.ToArray(/*new CharacterRunAutomaton[list.size()]*/);
        }

        private sealed class CharacterRunAutomatonToStringAnonymousClass : CharacterRunAutomaton
        {
            private readonly Func<string> toStringMethod;

            public CharacterRunAutomatonToStringAnonymousClass(Automaton a, Func<string> toStringMethod)
                : base(a)
            {
                this.toStringMethod = toStringMethod;
            }

            public override string ToString()
            {
                return toStringMethod();
            }
        }

        private sealed class SimpleCharacterRunAutomatonAnonymousClass : CharacterRunAutomaton
        {
            private readonly CharsRef lowerBound;
            private readonly CharsRef upperBound;

            private readonly bool includeLower;
            private readonly bool includeUpper;
#pragma warning disable 612, 618
            private static readonly IComparer<CharsRef> comparer = CharsRef.UTF16SortedAsUTF8Comparer; // LUCENENET specific - made static
#pragma warning restore 612, 618

            public SimpleCharacterRunAutomatonAnonymousClass(Automaton a, TermRangeQuery tq)
                : base(a)
            {
                if (tq.LowerTerm is null)
                {
                    lowerBound = null;
                }
                else
                {
                    lowerBound = new CharsRef(tq.LowerTerm.Utf8ToString());
                }

                if (tq.UpperTerm is null)
                {
                    upperBound = null;
                }
                else
                {
                    upperBound = new CharsRef(tq.UpperTerm.Utf8ToString());
                }

                includeLower = tq.IncludesLower;
                includeUpper = tq.IncludesUpper;
            }

            public override bool Run(char[] s, int offset, int length)
            {
                CharsRef scratch = new CharsRef(s, offset, length);

                if (lowerBound != null)
                {
                    int cmp = comparer.Compare(scratch, lowerBound);
                    if (cmp < 0 || (!includeLower && cmp == 0))
                    {
                        return false;
                    }
                }

                if (upperBound != null)
                {
                    int cmp = comparer.Compare(scratch, upperBound);
                    if (cmp > 0 || (!includeUpper && cmp == 0))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Returns a "fake" <see cref="DocsAndPositionsEnum"/> over the tokenstream, returning offsets where <paramref name="matchers"/>
        /// matches tokens.
        /// <para/>
        /// This is solely used internally by <see cref="ICUPostingsHighlighter"/>: <b>DO NOT USE THIS METHOD!</b>
        /// </summary>
        internal static DocsAndPositionsEnum GetDocsEnum(TokenStream ts, CharacterRunAutomaton[] matchers)
        {
            ICharTermAttribute charTermAtt = ts.AddAttribute<ICharTermAttribute>();
            IOffsetAttribute offsetAtt = ts.AddAttribute<IOffsetAttribute>();
            ts.Reset();

            // TODO: we could use CachingWrapperFilter, (or consume twice) to allow us to have a true freq()
            // but this would have a performance cost for likely little gain in the user experience, it
            // would only serve to make this method less bogus.
            // instead, we always return freq() = Integer.MAX_VALUE and let PH terminate based on offset...

            return new DocsAndPositionsEnumAnonymousClass(ts, matchers, charTermAtt, offsetAtt);
        }

        private sealed class DocsAndPositionsEnumAnonymousClass : DocsAndPositionsEnum
        {
            private readonly CharacterRunAutomaton[] matchers;
            private readonly ICharTermAttribute charTermAtt;
            private readonly IOffsetAttribute offsetAtt;

            public DocsAndPositionsEnumAnonymousClass(
                TokenStream ts, CharacterRunAutomaton[] matchers, ICharTermAttribute charTermAtt, IOffsetAttribute offsetAtt)
            {
                this.matchers = matchers;
                this.charTermAtt = charTermAtt;
                this.offsetAtt = offsetAtt;

                stream = ts;
                matchDescriptions = new BytesRef[matchers.Length];
            }


            internal int currentDoc = -1;
            internal int currentMatch = -1;
            internal int currentStartOffset = -1;
            internal int currentEndOffset = -1;
            internal TokenStream stream;

            private readonly BytesRef[] matchDescriptions;


            public override int NextPosition()
            {
                if (stream != null)
                {
                    while (stream.IncrementToken())
                    {
                        for (int i = 0; i < matchers.Length; i++)
                        {
                            if (matchers[i].Run(charTermAtt.Buffer, 0, charTermAtt.Length))
                            {
                                currentStartOffset = offsetAtt.StartOffset;
                                currentEndOffset = offsetAtt.EndOffset;
                                currentMatch = i;
                                return 0;
                            }
                        }
                    }
                    stream.End();
                    stream.Dispose();
                    stream = null;
                }
                // exhausted
                currentStartOffset = currentEndOffset = int.MaxValue;
                return int.MaxValue;
            }

            public override int Freq => int.MaxValue; // lie

            public override int StartOffset
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(currentStartOffset >= 0);
                    return currentStartOffset;
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(currentEndOffset >= 0);
                    return currentEndOffset;
                }
            }


            public override BytesRef GetPayload()
            {
                if (matchDescriptions[currentMatch] is null)
                {
                    matchDescriptions[currentMatch] = new BytesRef(matchers[currentMatch].ToString());
                }
                return matchDescriptions[currentMatch];
            }

            public override int DocID => currentDoc;

            public override int NextDoc()
            {
                throw UnsupportedOperationException.Create();
            }

            public override int Advance(int target)
            {
                return currentDoc = target;
            }

            public override long GetCost()
            {
                return 0;
            }
        }
    }
}
#endif