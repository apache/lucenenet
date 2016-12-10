using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /** 
   * Extracts all MultiTermQueries for {@code field}, and returns equivalent 
   * automata that will match terms.
   */
        internal static CharacterRunAutomaton[] ExtractAutomata(Query query, String field)
        {
            List<CharacterRunAutomaton> list = new List<CharacterRunAutomaton>();
            if (query is BooleanQuery)
            {
                BooleanClause[] clauses = ((BooleanQuery)query).Clauses;
                foreach (BooleanClause clause in clauses)
                {
                    if (!clause.Prohibited)
                    {
                        list.AddAll(Arrays.AsList(ExtractAutomata(clause.Query, field)));
                    }
                }
            }
            else if (query is DisjunctionMaxQuery)
            {
                foreach (Query sub in ((DisjunctionMaxQuery)query).Disjuncts)
                {
                    list.AddAll(Arrays.AsList(ExtractAutomata(sub, field)));
                }
            }
            else if (query is SpanOrQuery)
            {
                foreach (Query sub in ((SpanOrQuery)query).Clauses)
                {
                    list.AddAll(Arrays.AsList(ExtractAutomata(sub, field)));
                }
            }
            else if (query is SpanNearQuery)
            {
                foreach (Query sub in ((SpanNearQuery)query).Clauses)
                {
                    list.AddAll(Arrays.AsList(ExtractAutomata(sub, field)));
                }
            }
            else if (query is SpanNotQuery)
            {
                list.AddAll(Arrays.AsList(ExtractAutomata(((SpanNotQuery)query).Include, field)));
            }
            else if (query is SpanPositionCheckQuery)
            {
                list.AddAll(Arrays.AsList(ExtractAutomata(((SpanPositionCheckQuery)query).Match, field)));
            }
            else if (query is ISpanMultiTermQueryWrapper)
            //else if (query.GetType().IsAssignableFrom(typeof(SpanMultiTermQueryWrapper<>)))
            {
                list.AddAll(Arrays.AsList(ExtractAutomata(((ISpanMultiTermQueryWrapper)query).WrappedQuery, field)));
            }
            else if (query is AutomatonQuery)
            {
                AutomatonQuery aq = (AutomatonQuery)query;
                if (aq.Field.Equals(field))
                {
                    list.Add(new CharacterRunAutomatonToStringAnonymousHelper(aq.Automaton, () => aq.ToString()));

                    //                list.Add(new CharacterRunAutomaton(aq.Automaton) {
                    //      @Override
                    //      public String toString()
                    //    {
                    //        return aq.toString();
                    //    }
                    //});
                }
            }
            else if (query is PrefixQuery)
            {
                PrefixQuery pq = (PrefixQuery)query;
                Term prefix = pq.Prefix;
                if (prefix.Field.Equals(field))
                {
                    list.Add(new CharacterRunAutomatonToStringAnonymousHelper(
                        BasicOperations.Concatenate(BasicAutomata.MakeString(prefix.Text()), BasicAutomata.MakeAnyString()), 
                        () => pq.ToString()));

                    //        list.Add(new CharacterRunAutomaton(BasicOperations.Concatenate(BasicAutomata.MakeString(prefix.Text()), 
                    //                                                                       BasicAutomata.MakeAnyString())) {
                    //          @Override
                    //          public String toString()
                    //{
                    //    return pq.toString();
                    //}
                    //        });
                }
            }
            else if (query is FuzzyQuery)
            {
                FuzzyQuery fq = (FuzzyQuery)query;
                if (fq.Field.Equals(field))
                {
                    String utf16 = fq.Term.Text();
                    int[] termText = new int[utf16.CodePointCount(0, utf16.Length)];
                    for (int cp, i = 0, j = 0; i < utf16.Length; i += Character.CharCount(cp))
                    {
                        termText[j++] = cp = utf16.CodePointAt(i);
                    }
                    int termLength = termText.Length;
                    int prefixLength = Math.Min(fq.PrefixLength, termLength);
                    String suffix = UnicodeUtil.NewString(termText, prefixLength, termText.Length - prefixLength);
                    LevenshteinAutomata builder = new LevenshteinAutomata(suffix, fq.Transpositions);
                    Automaton automaton = builder.ToAutomaton(fq.MaxEdits);
                    if (prefixLength > 0)
                    {
                        Automaton prefix = BasicAutomata.MakeString(UnicodeUtil.NewString(termText, 0, prefixLength));
                        automaton = BasicOperations.Concatenate(prefix, automaton);
                    }
                    list.Add(new CharacterRunAutomatonToStringAnonymousHelper(automaton, () => fq.ToString()));

                    //        list.Add(new CharacterRunAutomaton(automaton)
                    //{
                    //    @Override
                    //          public String toString()
                    //{
                    //    return fq.toString();
                    //}
                    //        });
                }
            }
            else if (query is TermRangeQuery)
            {
                TermRangeQuery tq = (TermRangeQuery)query;
                if (tq.Field.Equals(field))
                {
                    // this is *not* an automaton, but its very simple
                    list.Add(new SimpleCharacterRunAutomatonAnonymousHelper(BasicAutomata.MakeEmpty(), tq));

                    //CharsRef lowerBound;
                    //if (tq.LowerTerm == null)
                    //{
                    //    lowerBound = null;
                    //}
                    //else
                    //{
                    //    lowerBound = new CharsRef(tq.LowerTerm.Utf8ToString());
                    //}

                    //CharsRef upperBound;
                    //if (tq.UpperTerm == null)
                    //{
                    //    upperBound = null;
                    //}
                    //else
                    //{
                    //    upperBound = new CharsRef(tq.UpperTerm.Utf8ToString());
                    //}



                    //bool includeLower = tq.IncludesLower();
                    //bool includeUpper = tq.IncludesUpper();
                    //CharsRef scratch = new CharsRef();
                    //IComparer<CharsRef> comparator = CharsRef.UTF16SortedAsUTF8Comparer;



                    //list.Add(new CharacterRunAutomaton(BasicAutomata.MakeEmpty()) {
                    //          @Override
                    //          public boolean run(char[] s, int offset, int length)
                    //{
                    //    scratch.chars = s;
                    //    scratch.offset = offset;
                    //    scratch.length = length;

                    //    if (lowerBound != null)
                    //    {
                    //        int cmp = comparator.compare(scratch, lowerBound);
                    //        if (cmp < 0 || (!includeLower && cmp == 0))
                    //        {
                    //            return false;
                    //        }
                    //    }

                    //    if (upperBound != null)
                    //    {
                    //        int cmp = comparator.compare(scratch, upperBound);
                    //        if (cmp > 0 || (!includeUpper && cmp == 0))
                    //        {
                    //            return false;
                    //        }
                    //    }
                    //    return true;
                    //}

                    //@Override
                    //          public String toString()
                    //{
                    //    return tq.toString();
                    //}
                    //        });
                }
            }
            return list.ToArray(/*new CharacterRunAutomaton[list.size()]*/);
        }

        internal class CharacterRunAutomatonToStringAnonymousHelper : CharacterRunAutomaton
        {
            private readonly Func<string> toStringMethod;

            public CharacterRunAutomatonToStringAnonymousHelper(Automaton a, Func<string> toStringMethod)
                : base(a)
            {
                this.toStringMethod = toStringMethod;
            }

            public override string ToString()
            {
                return toStringMethod();
            }
        }

        internal class SimpleCharacterRunAutomatonAnonymousHelper : CharacterRunAutomaton
        {
            private readonly CharsRef lowerBound;
            private readonly CharsRef upperBound;

            private bool includeLower;
            private bool includeUpper;
            //private CharsRef scratch = new CharsRef();
            private IComparer<CharsRef> comparator = CharsRef.UTF16SortedAsUTF8Comparer;

            public SimpleCharacterRunAutomatonAnonymousHelper(Automaton a, TermRangeQuery tq)
                : base(a)
            {
                if (tq.LowerTerm == null)
                {
                    lowerBound = null;
                }
                else
                {
                    lowerBound = new CharsRef(tq.LowerTerm.Utf8ToString());
                }

                if (tq.UpperTerm == null)
                {
                    upperBound = null;
                }
                else
                {
                    upperBound = new CharsRef(tq.UpperTerm.Utf8ToString());
                }

                includeLower = tq.IncludesLower();
                includeUpper = tq.IncludesUpper();
            }

            public override bool Run(char[] s, int offset, int length)
            {
                CharsRef scratch = new CharsRef(s, offset, length);

                //scratch.Chars = s;
                //scratch.Offset = offset;
                //scratch.Length = length;

                if (lowerBound != null)
                {
                    int cmp = comparator.Compare(scratch, lowerBound);
                    if (cmp < 0 || (!includeLower && cmp == 0))
                    {
                        return false;
                    }
                }

                if (upperBound != null)
                {
                    int cmp = comparator.Compare(scratch, upperBound);
                    if (cmp > 0 || (!includeUpper && cmp == 0))
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        /** 
         * Returns a "fake" DocsAndPositionsEnum over the tokenstream, returning offsets where {@code matchers}
         * matches tokens.
         * <p>
         * This is solely used internally by PostingsHighlighter: <b>DO NOT USE THIS METHOD!</b>
         */
        internal static DocsAndPositionsEnum GetDocsEnum(TokenStream ts, CharacterRunAutomaton[] matchers)
        {
            ICharTermAttribute charTermAtt = ts.AddAttribute<ICharTermAttribute>();
            IOffsetAttribute offsetAtt = ts.AddAttribute<IOffsetAttribute>();
            ts.Reset();

            // TODO: we could use CachingWrapperFilter, (or consume twice) to allow us to have a true freq()
            // but this would have a performance cost for likely little gain in the user experience, it
            // would only serve to make this method less bogus.
            // instead, we always return freq() = Integer.MAX_VALUE and let PH terminate based on offset...

            return new DocsAndPositionsEnumAnonymousHelper(ts, matchers, charTermAtt, offsetAtt);

            //    return new DocsAndPositionsEnum()
            //{
            //    int currentDoc = -1;
            //    int currentMatch = -1;
            //    int currentStartOffset = -1;
            //    int currentEndOffset = -1;
            //    TokenStream stream = ts;

            //    final BytesRef matchDescriptions[] = new BytesRef[matchers.length];

            //    @Override
            //      public int nextPosition() throws IOException
            //{
            //        if (stream != null) {
            //        while (stream.incrementToken())
            //        {
            //            for (int i = 0; i < matchers.length; i++)
            //            {
            //                if (matchers[i].run(charTermAtt.buffer(), 0, charTermAtt.length()))
            //                {
            //                    currentStartOffset = offsetAtt.startOffset();
            //                    currentEndOffset = offsetAtt.endOffset();
            //                    currentMatch = i;
            //                    return 0;
            //                }
            //            }
            //        }
            //        stream.end();
            //        stream.close();
            //        stream = null;
            //    }
            //    // exhausted
            //    currentStartOffset = currentEndOffset = Integer.MAX_VALUE;
            //        return Integer.MAX_VALUE;
            //}

            //@Override
            //      public int freq() throws IOException
            //{
            //        return Integer.MAX_VALUE; // lie
            //}

            //@Override
            //      public int startOffset() throws IOException
            //{
            //    assert currentStartOffset >= 0;
            //        return currentStartOffset;
            //}

            //@Override
            //      public int endOffset() throws IOException
            //{
            //    assert currentEndOffset >= 0;
            //        return currentEndOffset;
            //}

            //@Override
            //      public BytesRef getPayload() throws IOException
            //{
            //        if (matchDescriptions [currentMatch] == null) {
            //        matchDescriptions[currentMatch] = new BytesRef(matchers[currentMatch].toString());
            //    }
            //        return matchDescriptions [currentMatch];
            //}

            //@Override
            //      public int docID()
            //{
            //    return currentDoc;
            //}

            //@Override
            //      public int nextDoc() throws IOException
            //{
            //        throw new UnsupportedOperationException();
            //      }

            //      @Override
            //      public int advance(int target) throws IOException
            //{
            //        return currentDoc = target;
            //}

            //@Override
            //      public long cost()
            //{
            //    return 0;
            //}
            //    };
        }

        internal class DocsAndPositionsEnumAnonymousHelper : DocsAndPositionsEnum
        {
            private readonly CharacterRunAutomaton[] matchers;
            private readonly ICharTermAttribute charTermAtt;
            private readonly IOffsetAttribute offsetAtt;

            public DocsAndPositionsEnumAnonymousHelper(
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

            readonly BytesRef[] matchDescriptions;


            public override int NextPosition()
            {
                if (stream != null)
                {
                    while (stream.IncrementToken())
                    {
                        for (int i = 0; i < matchers.Length; i++)
                        {
                            if (matchers[i].Run(charTermAtt.Buffer(), 0, charTermAtt.Length))
                            {
                                currentStartOffset = offsetAtt.StartOffset();
                                currentEndOffset = offsetAtt.EndOffset();
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

            public override int Freq()
            {
                return int.MaxValue; // lie
            }

            public override int StartOffset()
            {
                Debug.Assert(currentStartOffset >= 0);
                return currentStartOffset;
            }

            public override int EndOffset()
            {
                Debug.Assert(currentEndOffset >= 0);
                return currentEndOffset;
            }


            public override BytesRef Payload
            {
                get
                {
                    if (matchDescriptions[currentMatch] == null)
                    {
                        matchDescriptions[currentMatch] = new BytesRef(matchers[currentMatch].ToString());
                    }
                    return matchDescriptions[currentMatch];
                }
            }

            public override int DocID()
            {
                return currentDoc;
            }

            public override int NextDoc()
            {
                throw new NotSupportedException();
            }

            public override int Advance(int target)
            {
                return currentDoc = target;
            }

            public override long Cost()
            {
                return 0;
            }
        }
    }
}
