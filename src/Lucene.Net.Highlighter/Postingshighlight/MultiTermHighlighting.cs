/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Postingshighlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search.Postingshighlight
{
	/// <summary>Support for highlighting multiterm queries in PostingsHighlighter.</summary>
	/// <remarks>Support for highlighting multiterm queries in PostingsHighlighter.</remarks>
	internal class MultiTermHighlighting
	{
		/// <summary>
		/// Extracts all MultiTermQueries for
		/// <code>field</code>
		/// , and returns equivalent
		/// automata that will match terms.
		/// </summary>
		internal static CharacterRunAutomaton[] ExtractAutomata(Query query, string field
			)
		{
			IList<CharacterRunAutomaton> list = new AList<CharacterRunAutomaton>();
			if (query is BooleanQuery)
			{
				BooleanClause[] clauses = ((BooleanQuery)query).GetClauses();
				foreach (BooleanClause clause in clauses)
				{
					if (!clause.IsProhibited())
					{
						Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(clause.GetQuery(), 
							field)));
					}
				}
			}
			else
			{
				if (query is DisjunctionMaxQuery)
				{
					foreach (Query sub in ((DisjunctionMaxQuery)query).GetDisjuncts())
					{
						Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(sub, field)));
					}
				}
				else
				{
					if (query is SpanOrQuery)
					{
						foreach (Query sub in ((SpanOrQuery)query).GetClauses())
						{
							Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(sub, field)));
						}
					}
					else
					{
						if (query is SpanNearQuery)
						{
							foreach (Query sub in ((SpanNearQuery)query).GetClauses())
							{
								Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(sub, field)));
							}
						}
						else
						{
							if (query is SpanNotQuery)
							{
								Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(((SpanNotQuery)query
									).GetInclude(), field)));
							}
							else
							{
								if (query is SpanPositionCheckQuery)
								{
									Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(((SpanPositionCheckQuery
										)query).GetMatch(), field)));
								}
								else
								{
									if (query is SpanMultiTermQueryWrapper)
									{
										Sharpen.Collections.AddAll(list, Arrays.AsList(ExtractAutomata(((SpanMultiTermQueryWrapper
											<object>)query).GetWrappedQuery(), field)));
									}
									else
									{
										if (query is AutomatonQuery)
										{
											AutomatonQuery aq = (AutomatonQuery)query;
											if (aq.GetField().Equals(field))
											{
												list.AddItem(new _CharacterRunAutomaton_92(aq, aq.GetAutomaton()));
											}
										}
										else
										{
											if (query is PrefixQuery)
											{
												PrefixQuery pq = (PrefixQuery)query;
												Term prefix = pq.GetPrefix();
												if (prefix.Field().Equals(field))
												{
													list.AddItem(new _CharacterRunAutomaton_104(pq, BasicOperations.Concatenate(BasicAutomata
														.MakeString(prefix.Text()), BasicAutomata.MakeAnyString())));
												}
											}
											else
											{
												if (query is FuzzyQuery)
												{
													FuzzyQuery fq = (FuzzyQuery)query;
													if (fq.GetField().Equals(field))
													{
														string utf16 = fq.GetTerm().Text();
														int[] termText = new int[utf16.CodePointCount(0, utf16.Length)];
														for (int cp; i < utf16.Length; i += char.CharCount(cp))
														{
															termText[j++] = cp = utf16.CodePointAt(i);
														}
														int termLength = termText.Length;
														int prefixLength = Math.Min(fq.GetPrefixLength(), termLength);
														string suffix = UnicodeUtil.NewString(termText, prefixLength, termText.Length - prefixLength
															);
														LevenshteinAutomata builder = new LevenshteinAutomata(suffix, fq.GetTranspositions
															());
														Lucene.Net.Util.Automaton.Automaton automaton = builder.ToAutomaton(fq.GetMaxEdits
															());
														if (prefixLength > 0)
														{
															Lucene.Net.Util.Automaton.Automaton prefix = BasicAutomata.MakeString(UnicodeUtil
																.NewString(termText, 0, prefixLength));
															automaton = BasicOperations.Concatenate(prefix, automaton);
														}
														list.AddItem(new _CharacterRunAutomaton_128(fq, automaton));
													}
												}
												else
												{
													if (query is TermRangeQuery)
													{
														TermRangeQuery tq = (TermRangeQuery)query;
														if (tq.GetField().Equals(field))
														{
															CharsRef lowerBound;
															if (tq.GetLowerTerm() == null)
															{
																lowerBound = null;
															}
															else
															{
																lowerBound = new CharsRef(tq.GetLowerTerm().Utf8ToString());
															}
															CharsRef upperBound;
															if (tq.GetUpperTerm() == null)
															{
																upperBound = null;
															}
															else
															{
																upperBound = new CharsRef(tq.GetUpperTerm().Utf8ToString());
															}
															bool includeLower = tq.IncludesLower();
															bool includeUpper = tq.IncludesUpper();
															CharsRef scratch = new CharsRef();
															IComparer<CharsRef> comparator = CharsRef.GetUTF16SortedAsUTF8Comparator();
															// this is *not* an automaton, but its very simple
															list.AddItem(new _CharacterRunAutomaton_158(scratch, lowerBound, comparator, includeLower
																, upperBound, includeUpper, tq, BasicAutomata.MakeEmpty()));
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return Sharpen.Collections.ToArray(list, new CharacterRunAutomaton[list.Count]);
		}

		private sealed class _CharacterRunAutomaton_92 : CharacterRunAutomaton
		{
			public _CharacterRunAutomaton_92(AutomatonQuery aq, Lucene.Net.Util.Automaton.Automaton
				 baseArg1) : base(baseArg1)
			{
				this.aq = aq;
			}

			public override string ToString()
			{
				return aq.ToString();
			}

			private readonly AutomatonQuery aq;
		}

		private sealed class _CharacterRunAutomaton_104 : CharacterRunAutomaton
		{
			public _CharacterRunAutomaton_104(PrefixQuery pq, Lucene.Net.Util.Automaton.Automaton
				 baseArg1) : base(baseArg1)
			{
				this.pq = pq;
			}

			public override string ToString()
			{
				return pq.ToString();
			}

			private readonly PrefixQuery pq;
		}

		private sealed class _CharacterRunAutomaton_128 : CharacterRunAutomaton
		{
			public _CharacterRunAutomaton_128(FuzzyQuery fq, Lucene.Net.Util.Automaton.Automaton
				 baseArg1) : base(baseArg1)
			{
				this.fq = fq;
			}

			public override string ToString()
			{
				return fq.ToString();
			}

			private readonly FuzzyQuery fq;
		}

		private sealed class _CharacterRunAutomaton_158 : CharacterRunAutomaton
		{
			public _CharacterRunAutomaton_158(CharsRef scratch, CharsRef lowerBound, IComparer
				<CharsRef> comparator, bool includeLower, CharsRef upperBound, bool includeUpper
				, TermRangeQuery tq, Lucene.Net.Util.Automaton.Automaton baseArg1) : base
				(baseArg1)
			{
				this.scratch = scratch;
				this.lowerBound = lowerBound;
				this.comparator = comparator;
				this.includeLower = includeLower;
				this.upperBound = upperBound;
				this.includeUpper = includeUpper;
				this.tq = tq;
			}

			public override bool Run(char[] s, int offset, int length)
			{
				scratch.chars = s;
				scratch.offset = offset;
				scratch.length = length;
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

			public override string ToString()
			{
				return tq.ToString();
			}

			private readonly CharsRef scratch;

			private readonly CharsRef lowerBound;

			private readonly IComparer<CharsRef> comparator;

			private readonly bool includeLower;

			private readonly CharsRef upperBound;

			private readonly bool includeUpper;

			private readonly TermRangeQuery tq;
		}

		/// <summary>
		/// Returns a "fake" DocsAndPositionsEnum over the tokenstream, returning offsets where
		/// <code>matchers</code>
		/// matches tokens.
		/// <p>
		/// This is solely used internally by PostingsHighlighter: <b>DO NOT USE THIS METHOD!</b>
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal static DocsAndPositionsEnum GetDocsEnum(TokenStream ts, CharacterRunAutomaton
			[] matchers)
		{
			MultiTermHighlighting mh = new MultiTermHighlighting();
			MultiTermHighlighting.DocsAndPositionsEnumImpl docImpl = new MultiTermHighlighting.DocsAndPositionsEnumImpl
				(this);
			docImpl.SetTokenStream(ts);
			docImpl.SetMatchers(matchers);
			return docImpl;
		}

		internal class DocsAndPositionsEnumImpl : DocsAndPositionsEnum
		{
			internal TokenStream ts;

			internal CharacterRunAutomaton[] matchers;

			internal CharTermAttribute charTermAtt = this.ts.AddAttribute<CharTermAttribute>(
				);

			internal OffsetAttribute offsetAtt = this.ts.AddAttribute<OffsetAttribute>();

			internal int currentDoc = -1;

			internal int currentMatch = -1;

			internal int currentStartOffset = -1;

			internal int currentEndOffset = -1;

			internal TokenStream stream = this.ts;

			//HM:revisit this was an internal anon class refactored to DocsAndPositionsEnumImpl
			// TODO: we could use CachingWrapperFilter, (or consume twice) to allow us to have a true freq()
			// but this would have a performance cost for likely little gain in the user experience, it
			// would only serve to make this method less bogus.
			// instead, we always return freq() = Integer.MAX_VALUE and let PH terminate based on offset...
			//HM:revisit. this shouldnt be commented out
			//ts.reset();
			public virtual void SetTokenStream(TokenStream t)
			{
				this.ts = t;
			}

			public virtual void SetMatchers(CharacterRunAutomaton[] m)
			{
				this.matchers = m;
			}

			internal readonly BytesRef matchDescriptions = new BytesRef[this.matchers.Length]
				;

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				if (this.stream != null)
				{
					while (this.stream.IncrementToken())
					{
						for (int i = 0; i < this.matchers.Length; i++)
						{
							if (this.matchers[i].Run(this.chartermAtt.Buffer, 0, this.charTermAtt.Length))
							{
								this.currentStartOffset = this.offsetAtt.StartOffset();
								this.currentEndOffset = this.offsetAtt.EndOffset();
								this.currentMatch = i;
								return 0;
							}
						}
					}
					this.stream.End();
					this.stream.Close();
					this.stream = null;
				}
				// exhausted
				this.currentStartOffset = this.currentEndOffset = int.MaxValue;
				return int.MaxValue;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return int.MaxValue;
			}

			// lie
			/// <exception cref="System.IO.IOException"></exception>
			public override int StartOffset()
			{
				//HM:revisit
				//assert currentStartOffset >= 0;
				return this.currentStartOffset;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int EndOffset()
			{
				//HM:revisit
				//assert currentEndOffset >= 0;
				return this.currentEndOffset;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				if (this.matchDescriptions[this.currentMatch] == null)
				{
					this.matchDescriptions[this.currentMatch] = new BytesRef(this.matchers[this.currentMatch
						].ToString());
				}
				return this.matchDescriptions[this.currentMatch];
			}

			public override int DocID()
			{
				return this.currentDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return this.currentDoc = target;
			}

			public override long Cost()
			{
				return 0;
			}

			internal DocsAndPositionsEnumImpl(MultiTermHighlighting _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly MultiTermHighlighting _enclosing;
		}
	}
}
