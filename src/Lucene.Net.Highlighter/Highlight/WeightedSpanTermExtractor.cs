/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Index.Memory;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>
	/// Class used to extract
	/// <see cref="WeightedSpanTerm">WeightedSpanTerm</see>
	/// s from a
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// based on whether
	/// <see cref="Lucene.Net.Index.Term">Lucene.Net.Index.Term</see>
	/// s from the
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// are contained in a supplied
	/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
	/// 	</see>
	/// .
	/// </summary>
	public class WeightedSpanTermExtractor
	{
		private string fieldName;

		private TokenStream tokenStream;

		private string defaultField;

		private bool expandMultiTermQuery;

		private bool cachedTokenStream;

		private bool wrapToCaching = true;

		private int maxDocCharsToAnalyze;

		private AtomicReader internalReader = null;

		public WeightedSpanTermExtractor()
		{
		}

		public WeightedSpanTermExtractor(string defaultField)
		{
			if (defaultField != null)
			{
				this.defaultField = defaultField;
			}
		}

		/// <summary>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
		/// 	</summary>
		/// <remarks>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
		/// 	</remarks>
		/// <param name="query">Query to extract Terms from</param>
		/// <param name="terms">Map to place created WeightedSpanTerms in</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		protected internal virtual void Extract(Query query, IDictionary<string, WeightedSpanTerm
			> terms)
		{
			if (query is BooleanQuery)
			{
				BooleanClause[] queryClauses = ((BooleanQuery)query).GetClauses();
				for (int i = 0; i < queryClauses.Length; i++)
				{
					if (!queryClauses[i].IsProhibited())
					{
						Extract(queryClauses[i].GetQuery(), terms);
					}
				}
			}
			else
			{
				if (query is PhraseQuery)
				{
					PhraseQuery phraseQuery = ((PhraseQuery)query);
					Term[] phraseQueryTerms = phraseQuery.GetTerms();
					SpanQuery[] clauses = new SpanQuery[phraseQueryTerms.Length];
					for (int i = 0; i < phraseQueryTerms.Length; i++)
					{
						clauses[i] = new SpanTermQuery(phraseQueryTerms[i]);
					}
					int slop = phraseQuery.GetSlop();
					int[] positions = phraseQuery.GetPositions();
					// add largest position increment to slop
					if (positions.Length > 0)
					{
						int lastPos = positions[0];
						int largestInc = 0;
						int sz = positions.Length;
						for (int i_1 = 1; i_1 < sz; i_1++)
						{
							int pos = positions[i_1];
							int inc = pos - lastPos;
							if (inc > largestInc)
							{
								largestInc = inc;
							}
							lastPos = pos;
						}
						if (largestInc > 1)
						{
							slop += largestInc;
						}
					}
					bool inorder = false;
					if (slop == 0)
					{
						inorder = true;
					}
					SpanNearQuery sp = new SpanNearQuery(clauses, slop, inorder);
					sp.SetBoost(query.GetBoost());
					ExtractWeightedSpanTerms(terms, sp);
				}
				else
				{
					if (query is TermQuery)
					{
						ExtractWeightedTerms(terms, query);
					}
					else
					{
						if (query is SpanQuery)
						{
							ExtractWeightedSpanTerms(terms, (SpanQuery)query);
						}
						else
						{
							if (query is FilteredQuery)
							{
								Extract(((FilteredQuery)query).GetQuery(), terms);
							}
							else
							{
								if (query is ConstantScoreQuery)
								{
									Query q = ((ConstantScoreQuery)query).GetQuery();
									if (q != null)
									{
										Extract(q, terms);
									}
								}
								else
								{
									if (query is CommonTermsQuery)
									{
										// specialized since rewriting would change the result query 
										// this query is TermContext sensitive.
										ExtractWeightedTerms(terms, query);
									}
									else
									{
										if (query is DisjunctionMaxQuery)
										{
											for (Iterator<Query> iterator = ((DisjunctionMaxQuery)query).Iterator(); iterator
												.HasNext(); )
											{
												Extract(iterator.Next(), terms);
											}
										}
										else
										{
											if (query is MultiPhraseQuery)
											{
												MultiPhraseQuery mpq = (MultiPhraseQuery)query;
												IList<Term[]> termArrays = mpq.GetTermArrays();
												int[] positions = mpq.GetPositions();
												if (positions.Length > 0)
												{
													int maxPosition = positions[positions.Length - 1];
													for (int i = 0; i < positions.Length - 1; ++i)
													{
														if (positions[i] > maxPosition)
														{
															maxPosition = positions[i];
														}
													}
													IList<SpanQuery>[] disjunctLists = new IList[maxPosition + 1];
													int distinctPositions = 0;
													for (int i_1 = 0; i_1 < termArrays.Count; ++i_1)
													{
														Term[] termArray = termArrays[i_1];
														IList<SpanQuery> disjuncts = disjunctLists[positions[i_1]];
														if (disjuncts == null)
														{
															disjuncts = (disjunctLists[positions[i_1]] = new AList<SpanQuery>(termArray.Length
																));
															++distinctPositions;
														}
														for (int j = 0; j < termArray.Length; ++j)
														{
															disjuncts.AddItem(new SpanTermQuery(termArray[j]));
														}
													}
													int positionGaps = 0;
													int position = 0;
													SpanQuery[] clauses = new SpanQuery[distinctPositions];
													for (int i_2 = 0; i_2 < disjunctLists.Length; ++i_2)
													{
														IList<SpanQuery> disjuncts = disjunctLists[i_2];
														if (disjuncts != null)
														{
															clauses[position++] = new SpanOrQuery(Sharpen.Collections.ToArray(disjuncts, new 
																SpanQuery[disjuncts.Count]));
														}
														else
														{
															++positionGaps;
														}
													}
													int slop = mpq.GetSlop();
													bool inorder = (slop == 0);
													SpanNearQuery sp = new SpanNearQuery(clauses, slop + positionGaps, inorder);
													sp.SetBoost(query.GetBoost());
													ExtractWeightedSpanTerms(terms, sp);
												}
											}
											else
											{
												Query origQuery = query;
												if (query is MultiTermQuery)
												{
													if (!expandMultiTermQuery)
													{
														return;
													}
													MultiTermQuery copy = (MultiTermQuery)query.Clone();
													copy.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
													origQuery = copy;
												}
												IndexReader reader = ((AtomicReader)GetLeafContext().Reader());
												Query rewritten = origQuery.Rewrite(reader);
												if (rewritten != origQuery)
												{
													// only rewrite once and then flatten again - the rewritten query could have a speacial treatment
													// if this method is overwritten in a subclass or above in the next recursion
													Extract(rewritten, terms);
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
			ExtractUnknownQuery(query, terms);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ExtractUnknownQuery(Query query, IDictionary<string
			, WeightedSpanTerm> terms)
		{
		}

		// for sub-classing to extract custom queries
		/// <summary>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>SpanQuery</code>.
		/// 	</summary>
		/// <remarks>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>SpanQuery</code>.
		/// 	</remarks>
		/// <param name="terms">Map to place created WeightedSpanTerms in</param>
		/// <param name="spanQuery">SpanQuery to extract Terms from</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		protected internal virtual void ExtractWeightedSpanTerms(IDictionary<string, WeightedSpanTerm
			> terms, SpanQuery spanQuery)
		{
			ICollection<string> fieldNames;
			if (fieldName == null)
			{
				fieldNames = new HashSet<string>();
				CollectSpanQueryFields(spanQuery, fieldNames);
			}
			else
			{
				fieldNames = new HashSet<string>(1);
				fieldNames.AddItem(fieldName);
			}
			// To support the use of the default field name
			if (defaultField != null)
			{
				fieldNames.AddItem(defaultField);
			}
			IDictionary<string, SpanQuery> queries = new Dictionary<string, SpanQuery>();
			ICollection<Term> nonWeightedTerms = new HashSet<Term>();
			bool mustRewriteQuery = MustRewriteQuery(spanQuery);
			if (mustRewriteQuery)
			{
				foreach (string field in fieldNames)
				{
					SpanQuery rewrittenQuery = (SpanQuery)spanQuery.Rewrite(((AtomicReader)GetLeafContext
						().Reader()));
					queries.Put(field, rewrittenQuery);
					rewrittenQuery.ExtractTerms(nonWeightedTerms);
				}
			}
			else
			{
				spanQuery.ExtractTerms(nonWeightedTerms);
			}
			IList<PositionSpan> spanPositions = new AList<PositionSpan>();
			foreach (string field_1 in fieldNames)
			{
				SpanQuery q;
				if (mustRewriteQuery)
				{
					q = queries.Get(field_1);
				}
				else
				{
					q = spanQuery;
				}
				AtomicReaderContext context = GetLeafContext();
				IDictionary<Term, TermContext> termContexts = new Dictionary<Term, TermContext>();
				TreeSet<Term> extractedTerms = new TreeSet<Term>();
				q.ExtractTerms(extractedTerms);
				foreach (Term term in extractedTerms)
				{
					termContexts.Put(term, TermContext.Build(context, term));
				}
				Bits acceptDocs = ((AtomicReader)context.Reader()).GetLiveDocs();
				Lucene.Net.Search.Spans.Spans spans = q.GetSpans(context, acceptDocs, termContexts
					);
				// collect span positions
				while (spans.Next())
				{
					spanPositions.AddItem(new PositionSpan(spans.Start(), spans.End() - 1));
				}
			}
			if (spanPositions.Count == 0)
			{
				// no spans found
				return;
			}
			foreach (Term queryTerm in nonWeightedTerms)
			{
				if (FieldNameComparator(queryTerm.Field()))
				{
					WeightedSpanTerm weightedSpanTerm = terms.Get(queryTerm.Text());
					if (weightedSpanTerm == null)
					{
						weightedSpanTerm = new WeightedSpanTerm(spanQuery.GetBoost(), queryTerm.Text());
						weightedSpanTerm.AddPositionSpans(spanPositions);
						weightedSpanTerm.positionSensitive = true;
						terms.Put(queryTerm.Text(), weightedSpanTerm);
					}
					else
					{
						if (spanPositions.Count > 0)
						{
							weightedSpanTerm.AddPositionSpans(spanPositions);
						}
					}
				}
			}
		}

		/// <summary>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
		/// 	</summary>
		/// <remarks>Fills a <code>Map</code> with <@link WeightedSpanTerm>s using the terms from the supplied <code>Query</code>.
		/// 	</remarks>
		/// <param name="terms">Map to place created WeightedSpanTerms in</param>
		/// <param name="query">Query to extract Terms from</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		protected internal virtual void ExtractWeightedTerms(IDictionary<string, WeightedSpanTerm
			> terms, Query query)
		{
			ICollection<Term> nonWeightedTerms = new HashSet<Term>();
			query.ExtractTerms(nonWeightedTerms);
			foreach (Term queryTerm in nonWeightedTerms)
			{
				if (FieldNameComparator(queryTerm.Field()))
				{
					WeightedSpanTerm weightedSpanTerm = new WeightedSpanTerm(query.GetBoost(), queryTerm
						.Text());
					terms.Put(queryTerm.Text(), weightedSpanTerm);
				}
			}
		}

		/// <summary>Necessary to implement matches for queries against <code>defaultField</code>
		/// 	</summary>
		protected internal virtual bool FieldNameComparator(string fieldNameToCheck)
		{
			bool rv = fieldName == null || fieldName.Equals(fieldNameToCheck) || (defaultField
				 != null && defaultField.Equals(fieldNameToCheck));
			return rv;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual AtomicReaderContext GetLeafContext()
		{
			if (internalReader == null)
			{
				if (wrapToCaching && !(tokenStream is CachingTokenFilter))
				{
					!cachedTokenStream = new CachingTokenFilter(new OffsetLimitTokenFilter(tokenStream
						, maxDocCharsToAnalyze));
					cachedTokenStream = true;
				}
				MemoryIndex indexer = new MemoryIndex(true);
				indexer.AddField(WeightedSpanTermExtractor.DelegatingAtomicReader.FIELD_NAME, tokenStream
					);
				tokenStream.Reset();
				IndexSearcher searcher = indexer.CreateSearcher();
				// MEM index has only atomic ctx
				internalReader = new WeightedSpanTermExtractor.DelegatingAtomicReader(((AtomicReader
					)((AtomicReaderContext)searcher.GetTopReaderContext()).Reader()));
			}
			return ((AtomicReaderContext)internalReader.GetContext());
		}

		internal sealed class DelegatingAtomicReader : FilterAtomicReader
		{
			private static readonly string FIELD_NAME = "shadowed_field";

			public DelegatingAtomicReader(AtomicReader @in) : base(@in)
			{
			}

			public override FieldInfos GetFieldInfos()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.Index.Fields Fields()
			{
				return new _FilterFields_388(base.Fields());
			}

			private sealed class _FilterFields_388 : FilterAtomicReader.FilterFields
			{
				public _FilterFields_388(Lucene.Net.Index.Fields baseArg1) : base(baseArg1
					)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Terms Terms(string field)
				{
					return base.Terms(WeightedSpanTermExtractor.DelegatingAtomicReader.FIELD_NAME);
				}

				public override Iterator<string> Iterator()
				{
					return Sharpen.Collections.SingletonList(WeightedSpanTermExtractor.DelegatingAtomicReader
						.FIELD_NAME).Iterator();
				}

				public override int Size()
				{
					return 1;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override NumericDocValues GetNumericDocValues(string field)
			{
				return base.GetNumericDocValues(FIELD_NAME);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BinaryDocValues GetBinaryDocValues(string field)
			{
				return base.GetBinaryDocValues(FIELD_NAME);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortedDocValues GetSortedDocValues(string field)
			{
				return base.GetSortedDocValues(FIELD_NAME);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override NumericDocValues GetNormValues(string field)
			{
				return base.GetNormValues(FIELD_NAME);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Bits GetDocsWithField(string field)
			{
				return base.GetDocsWithField(FIELD_NAME);
			}
		}

		/// <summary>Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
		/// 	</summary>
		/// <remarks>
		/// Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
		/// <p>
		/// </remarks>
		/// <param name="query">that caused hit</param>
		/// <param name="tokenStream">of text to be highlighted</param>
		/// <returns>Map containing WeightedSpanTerms</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual IDictionary<string, WeightedSpanTerm> GetWeightedSpanTerms(Query query
			, TokenStream tokenStream)
		{
			return GetWeightedSpanTerms(query, tokenStream, null);
		}

		/// <summary>Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
		/// 	</summary>
		/// <remarks>
		/// Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
		/// <p>
		/// </remarks>
		/// <param name="query">that caused hit</param>
		/// <param name="tokenStream">of text to be highlighted</param>
		/// <param name="fieldName">restricts Term's used based on field name</param>
		/// <returns>Map containing WeightedSpanTerms</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual IDictionary<string, WeightedSpanTerm> GetWeightedSpanTerms(Query query
			, TokenStream tokenStream, string fieldName)
		{
			if (fieldName != null)
			{
				this.fieldName = fieldName;
			}
			else
			{
				this.fieldName = null;
			}
			IDictionary<string, WeightedSpanTerm> terms = new WeightedSpanTermExtractor.PositionCheckingMap
				<string>();
			this.tokenStream = tokenStream;
			try
			{
				Extract(query, terms);
			}
			finally
			{
				IOUtils.Close(internalReader);
			}
			return terms;
		}

		/// <summary>Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>.
		/// 	</summary>
		/// <remarks>
		/// Creates a Map of <code>WeightedSpanTerms</code> from the given <code>Query</code> and <code>TokenStream</code>. Uses a supplied
		/// <code>IndexReader</code> to properly weight terms (for gradient highlighting).
		/// <p>
		/// </remarks>
		/// <param name="query">that caused hit</param>
		/// <param name="tokenStream">of text to be highlighted</param>
		/// <param name="fieldName">restricts Term's used based on field name</param>
		/// <param name="reader">to use for scoring</param>
		/// <returns>Map of WeightedSpanTerms with quasi tf/idf scores</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public virtual IDictionary<string, WeightedSpanTerm> GetWeightedSpanTermsWithScores
			(Query query, TokenStream tokenStream, string fieldName, IndexReader reader)
		{
			if (fieldName != null)
			{
				this.fieldName = fieldName;
			}
			else
			{
				this.fieldName = null;
			}
			this.tokenStream = tokenStream;
			IDictionary<string, WeightedSpanTerm> terms = new WeightedSpanTermExtractor.PositionCheckingMap
				<string>();
			Extract(query, terms);
			int totalNumDocs = reader.MaxDoc();
			ICollection<string> weightedTerms = terms.Keys;
			Iterator<string> it = weightedTerms.Iterator();
			try
			{
				while (it.HasNext())
				{
					WeightedSpanTerm weightedSpanTerm = terms.Get(it.Next());
					int docFreq = reader.DocFreq(new Term(fieldName, weightedSpanTerm.term));
					// IDF algorithm taken from DefaultSimilarity class
					float idf = (float)(Math.Log(totalNumDocs / (double)(docFreq + 1)) + 1.0);
					weightedSpanTerm.weight *= idf;
				}
			}
			finally
			{
				IOUtils.Close(internalReader);
			}
			return terms;
		}

		protected internal virtual void CollectSpanQueryFields(SpanQuery spanQuery, ICollection
			<string> fieldNames)
		{
			if (spanQuery is FieldMaskingSpanQuery)
			{
				CollectSpanQueryFields(((FieldMaskingSpanQuery)spanQuery).GetMaskedQuery(), fieldNames
					);
			}
			else
			{
				if (spanQuery is SpanFirstQuery)
				{
					CollectSpanQueryFields(((SpanFirstQuery)spanQuery).GetMatch(), fieldNames);
				}
				else
				{
					if (spanQuery is SpanNearQuery)
					{
						foreach (SpanQuery clause in ((SpanNearQuery)spanQuery).GetClauses())
						{
							CollectSpanQueryFields(clause, fieldNames);
						}
					}
					else
					{
						if (spanQuery is SpanNotQuery)
						{
							CollectSpanQueryFields(((SpanNotQuery)spanQuery).GetInclude(), fieldNames);
						}
						else
						{
							if (spanQuery is SpanOrQuery)
							{
								foreach (SpanQuery clause in ((SpanOrQuery)spanQuery).GetClauses())
								{
									CollectSpanQueryFields(clause, fieldNames);
								}
							}
							else
							{
								fieldNames.AddItem(spanQuery.GetField());
							}
						}
					}
				}
			}
		}

		protected internal virtual bool MustRewriteQuery(SpanQuery spanQuery)
		{
			if (!expandMultiTermQuery)
			{
				return false;
			}
			else
			{
				// Will throw UnsupportedOperationException in case of a SpanRegexQuery.
				if (spanQuery is FieldMaskingSpanQuery)
				{
					return MustRewriteQuery(((FieldMaskingSpanQuery)spanQuery).GetMaskedQuery());
				}
				else
				{
					if (spanQuery is SpanFirstQuery)
					{
						return MustRewriteQuery(((SpanFirstQuery)spanQuery).GetMatch());
					}
					else
					{
						if (spanQuery is SpanNearQuery)
						{
							foreach (SpanQuery clause in ((SpanNearQuery)spanQuery).GetClauses())
							{
								if (MustRewriteQuery(clause))
								{
									return true;
								}
							}
							return false;
						}
						else
						{
							if (spanQuery is SpanNotQuery)
							{
								SpanNotQuery spanNotQuery = (SpanNotQuery)spanQuery;
								return MustRewriteQuery(spanNotQuery.GetInclude()) || MustRewriteQuery(spanNotQuery
									.GetExclude());
							}
							else
							{
								if (spanQuery is SpanOrQuery)
								{
									foreach (SpanQuery clause in ((SpanOrQuery)spanQuery).GetClauses())
									{
										if (MustRewriteQuery(clause))
										{
											return true;
										}
									}
									return false;
								}
								else
								{
									if (spanQuery is SpanTermQuery)
									{
										return false;
									}
									else
									{
										return true;
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// This class makes sure that if both position sensitive and insensitive
		/// versions of the same term are added, the position insensitive one wins.
		/// </summary>
		/// <remarks>
		/// This class makes sure that if both position sensitive and insensitive
		/// versions of the same term are added, the position insensitive one wins.
		/// </remarks>
		[System.Serializable]
		protected internal class PositionCheckingMap<K> : Dictionary<K, WeightedSpanTerm>
		{
			public override void PutAll<_T0>(IDictionary<_T0> m)
			{
				foreach (KeyValuePair<K, WeightedSpanTerm> entry in m.EntrySet())
				{
					this.Put(entry.Key, entry.Value);
				}
			}

			public override WeightedSpanTerm Put(K key, WeightedSpanTerm value)
			{
				WeightedSpanTerm prev = base.Put(key, value);
				if (prev == null)
				{
					return prev;
				}
				WeightedSpanTerm prevTerm = prev;
				WeightedSpanTerm newTerm = value;
				if (!prevTerm.positionSensitive)
				{
					newTerm.positionSensitive = false;
				}
				return prev;
			}
		}

		public virtual bool GetExpandMultiTermQuery()
		{
			return expandMultiTermQuery;
		}

		public virtual void SetExpandMultiTermQuery(bool expandMultiTermQuery)
		{
			this.expandMultiTermQuery = expandMultiTermQuery;
		}

		public virtual bool IsCachedTokenStream()
		{
			return cachedTokenStream;
		}

		public virtual TokenStream GetTokenStream()
		{
			return tokenStream;
		}

		/// <summary>
		/// By default,
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// s that are not of the type
		/// <see cref="Lucene.Net.Analysis.CachingTokenFilter">Lucene.Net.Analysis.CachingTokenFilter
		/// 	</see>
		/// are wrapped in a
		/// <see cref="Lucene.Net.Analysis.CachingTokenFilter">Lucene.Net.Analysis.CachingTokenFilter
		/// 	</see>
		/// to
		/// ensure an efficient reset - if you are already using a different caching
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// impl and you don't want it to be wrapped, set this to
		/// false.
		/// </summary>
		public virtual void SetWrapIfNotCachingTokenFilter(bool wrap)
		{
			this.wrapToCaching = wrap;
		}

		protected internal void SetMaxDocCharsToAnalyze(int maxDocCharsToAnalyze)
		{
			this.maxDocCharsToAnalyze = maxDocCharsToAnalyze;
		}
	}
}
