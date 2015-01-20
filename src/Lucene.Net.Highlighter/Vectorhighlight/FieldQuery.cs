/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// FieldQuery breaks down query object into terms/phrases and keeps
	/// them in a QueryPhraseMap structure.
	/// </summary>
	/// <remarks>
	/// FieldQuery breaks down query object into terms/phrases and keeps
	/// them in a QueryPhraseMap structure.
	/// </remarks>
	public class FieldQuery
	{
		internal readonly bool fieldMatch;

		internal IDictionary<string, FieldQuery.QueryPhraseMap> rootMaps = new Dictionary
			<string, FieldQuery.QueryPhraseMap>();

		internal IDictionary<string, ICollection<string>> termSetMap = new Dictionary<string
			, ICollection<string>>();

		internal int termOrPhraseNumber;

		private const int MAX_MTQ_TERMS = 1024;

		/// <exception cref="System.IO.IOException"></exception>
		internal FieldQuery(Query query, IndexReader reader, bool phraseHighlight, bool fieldMatch
			)
		{
			// fieldMatch==true,  Map<fieldName,QueryPhraseMap>
			// fieldMatch==false, Map<null,QueryPhraseMap>
			// fieldMatch==true,  Map<fieldName,setOfTermsInQueries>
			// fieldMatch==false, Map<null,setOfTermsInQueries>
			// used for colored tag support
			// The maximum number of different matching terms accumulated from any one MultiTermQuery
			this.fieldMatch = fieldMatch;
			ICollection<Query> flatQueries = new LinkedHashSet<Query>();
			Flatten(query, reader, flatQueries);
			SaveTerms(flatQueries, reader);
			ICollection<Query> expandQueries = Expand(flatQueries);
			foreach (Query flatQuery in expandQueries)
			{
				FieldQuery.QueryPhraseMap rootMap = GetRootMap(flatQuery);
				rootMap.Add(flatQuery, reader);
				if (!phraseHighlight && flatQuery is PhraseQuery)
				{
					PhraseQuery pq = (PhraseQuery)flatQuery;
					if (pq.GetTerms().Length > 1)
					{
						foreach (Term term in pq.GetTerms())
						{
							rootMap.AddTerm(term, flatQuery.GetBoost());
						}
					}
				}
			}
		}

		/// <summary>
		/// For backwards compatibility you can initialize FieldQuery without
		/// an IndexReader, which is only required to support MultiTermQuery
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal FieldQuery(Query query, bool phraseHighlight, bool fieldMatch) : this(query
			, null, phraseHighlight, fieldMatch)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Flatten(Query sourceQuery, IndexReader reader, ICollection<
			Query> flatQueries)
		{
			if (sourceQuery is BooleanQuery)
			{
				BooleanQuery bq = (BooleanQuery)sourceQuery;
				foreach (BooleanClause clause in bq)
				{
					if (!clause.IsProhibited())
					{
						Flatten(ApplyParentBoost(clause.GetQuery(), bq), reader, flatQueries);
					}
				}
			}
			else
			{
				if (sourceQuery is DisjunctionMaxQuery)
				{
					DisjunctionMaxQuery dmq = (DisjunctionMaxQuery)sourceQuery;
					foreach (Query query in dmq)
					{
						Flatten(ApplyParentBoost(query, dmq), reader, flatQueries);
					}
				}
				else
				{
					if (sourceQuery is TermQuery)
					{
						if (!flatQueries.Contains(sourceQuery))
						{
							flatQueries.AddItem(sourceQuery);
						}
					}
					else
					{
						if (sourceQuery is PhraseQuery)
						{
							if (!flatQueries.Contains(sourceQuery))
							{
								PhraseQuery pq = (PhraseQuery)sourceQuery;
								if (pq.GetTerms().Length > 1)
								{
									flatQueries.AddItem(pq);
								}
								else
								{
									if (pq.GetTerms().Length == 1)
									{
										Query flat = new TermQuery(pq.GetTerms()[0]);
										flat.SetBoost(pq.GetBoost());
										flatQueries.AddItem(flat);
									}
								}
							}
						}
						else
						{
							if (sourceQuery is ConstantScoreQuery)
							{
								Query q = ((ConstantScoreQuery)sourceQuery).GetQuery();
								if (q != null)
								{
									Flatten(ApplyParentBoost(q, sourceQuery), reader, flatQueries);
								}
							}
							else
							{
								if (sourceQuery is FilteredQuery)
								{
									Query q = ((FilteredQuery)sourceQuery).GetQuery();
									if (q != null)
									{
										Flatten(ApplyParentBoost(q, sourceQuery), reader, flatQueries);
									}
								}
								else
								{
									if (reader != null)
									{
										Query query = sourceQuery;
										if (sourceQuery is MultiTermQuery)
										{
											MultiTermQuery copy = (MultiTermQuery)sourceQuery.Clone();
											copy.SetRewriteMethod(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(MAX_MTQ_TERMS
												));
											query = copy;
										}
										Query rewritten = query.Rewrite(reader);
										if (rewritten != query)
										{
											// only rewrite once and then flatten again - the rewritten query could have a speacial treatment
											// if this method is overwritten in a subclass.
											Flatten(rewritten, reader, flatQueries);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// if the query is already rewritten we discard it
		// else discard queries
		/// <summary>Push parent's boost into a clone of query if parent has a non 1 boost.</summary>
		/// <remarks>Push parent's boost into a clone of query if parent has a non 1 boost.</remarks>
		protected internal virtual Query ApplyParentBoost(Query query, Query parent)
		{
			if (parent.GetBoost() == 1)
			{
				return query;
			}
			Query cloned = query.Clone();
			cloned.SetBoost(query.GetBoost() * parent.GetBoost());
			return cloned;
		}

		internal virtual ICollection<Query> Expand(ICollection<Query> flatQueries)
		{
			ICollection<Query> expandQueries = new LinkedHashSet<Query>();
			for (Iterator<Query> i = flatQueries.Iterator(); i.HasNext(); )
			{
				Query query = i.Next();
				i.Remove();
				expandQueries.AddItem(query);
				if (!(query is PhraseQuery))
				{
					continue;
				}
				for (Iterator<Query> j = flatQueries.Iterator(); j.HasNext(); )
				{
					Query qj = j.Next();
					if (!(qj is PhraseQuery))
					{
						continue;
					}
					CheckOverlap(expandQueries, (PhraseQuery)query, (PhraseQuery)qj);
				}
			}
			return expandQueries;
		}

		private void CheckOverlap(ICollection<Query> expandQueries, PhraseQuery a, PhraseQuery
			 b)
		{
			if (a.GetSlop() != b.GetSlop())
			{
				return;
			}
			Term[] ats = a.GetTerms();
			Term[] bts = b.GetTerms();
			if (fieldMatch && !ats[0].Field().Equals(bts[0].Field()))
			{
				return;
			}
			CheckOverlap(expandQueries, ats, bts, a.GetSlop(), a.GetBoost());
			CheckOverlap(expandQueries, bts, ats, b.GetSlop(), b.GetBoost());
		}

		private void CheckOverlap(ICollection<Query> expandQueries, Term[] src, Term[] dest
			, int slop, float boost)
		{
			// beginning from 1 (not 0) is safe because that the PhraseQuery has multiple terms
			// is guaranteed in flatten() method (if PhraseQuery has only one term, flatten()
			// converts PhraseQuery to TermQuery)
			for (int i = 1; i < src.Length; i++)
			{
				bool overlap = true;
				for (int j = i; j < src.Length; j++)
				{
					if ((j - i) < dest.Length && !src[j].Text().Equals(dest[j - i].Text()))
					{
						overlap = false;
						break;
					}
				}
				if (overlap && src.Length - i < dest.Length)
				{
					PhraseQuery pq = new PhraseQuery();
					foreach (Term srcTerm in src)
					{
						pq.Add(srcTerm);
					}
					for (int k = src.Length - i; k < dest.Length; k++)
					{
						pq.Add(new Term(src[0].Field(), dest[k].Text()));
					}
					pq.SetSlop(slop);
					pq.SetBoost(boost);
					if (!expandQueries.Contains(pq))
					{
						expandQueries.AddItem(pq);
					}
				}
			}
		}

		internal virtual FieldQuery.QueryPhraseMap GetRootMap(Query query)
		{
			string key = GetKey(query);
			FieldQuery.QueryPhraseMap map = rootMaps.Get(key);
			if (map == null)
			{
				map = new FieldQuery.QueryPhraseMap(this);
				rootMaps.Put(key, map);
			}
			return map;
		}

		private string GetKey(Query query)
		{
			if (!fieldMatch)
			{
				return null;
			}
			if (query is TermQuery)
			{
				return ((TermQuery)query).GetTerm().Field();
			}
			else
			{
				if (query is PhraseQuery)
				{
					PhraseQuery pq = (PhraseQuery)query;
					Term[] terms = pq.GetTerms();
					return terms[0].Field();
				}
				else
				{
					if (query is MultiTermQuery)
					{
						return ((MultiTermQuery)query).GetField();
					}
					else
					{
						throw new RuntimeException("query \"" + query.ToString() + "\" must be flatten first."
							);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void SaveTerms(ICollection<Query> flatQueries, IndexReader reader
			)
		{
			foreach (Query query in flatQueries)
			{
				ICollection<string> termSet = GetTermSet(query);
				if (query is TermQuery)
				{
					termSet.AddItem(((TermQuery)query).GetTerm().Text());
				}
				else
				{
					if (query is PhraseQuery)
					{
						foreach (Term term in ((PhraseQuery)query).GetTerms())
						{
							termSet.AddItem(term.Text());
						}
					}
					else
					{
						if (query is MultiTermQuery && reader != null)
						{
							BooleanQuery mtqTerms = (BooleanQuery)query.Rewrite(reader);
							foreach (BooleanClause clause in mtqTerms.GetClauses())
							{
								termSet.AddItem(((TermQuery)clause.GetQuery()).GetTerm().Text());
							}
						}
						else
						{
							throw new RuntimeException("query \"" + query.ToString() + "\" must be flatten first."
								);
						}
					}
				}
			}
		}

		private ICollection<string> GetTermSet(Query query)
		{
			string key = GetKey(query);
			ICollection<string> set = termSetMap.Get(key);
			if (set == null)
			{
				set = new HashSet<string>();
				termSetMap.Put(key, set);
			}
			return set;
		}

		internal virtual ICollection<string> GetTermSet(string field)
		{
			return termSetMap.Get(fieldMatch ? field : null);
		}

		/// <returns>QueryPhraseMap</returns>
		public virtual FieldQuery.QueryPhraseMap GetFieldTermMap(string fieldName, string
			 term)
		{
			FieldQuery.QueryPhraseMap rootMap = GetRootMap(fieldName);
			return rootMap == null ? null : rootMap.subMap.Get(term);
		}

		/// <returns>QueryPhraseMap</returns>
		public virtual FieldQuery.QueryPhraseMap SearchPhrase(string fieldName, IList<FieldTermStack.TermInfo
			> phraseCandidate)
		{
			FieldQuery.QueryPhraseMap root = GetRootMap(fieldName);
			if (root == null)
			{
				return null;
			}
			return root.SearchPhrase(phraseCandidate);
		}

		private FieldQuery.QueryPhraseMap GetRootMap(string fieldName)
		{
			return rootMaps.Get(fieldMatch ? fieldName : null);
		}

		internal virtual int NextTermOrPhraseNumber()
		{
			return termOrPhraseNumber++;
		}

		/// <summary>
		/// Internal structure of a query for highlighting: represents
		/// a nested query structure
		/// </summary>
		public class QueryPhraseMap
		{
			internal bool terminal;

			internal int slop;

			internal float boost;

			internal int termOrPhraseNumber;

			internal FieldQuery fieldQuery;

			internal IDictionary<string, FieldQuery.QueryPhraseMap> subMap = new Dictionary<string
				, FieldQuery.QueryPhraseMap>();

			public QueryPhraseMap(FieldQuery fieldQuery)
			{
				// valid if terminal == true and phraseHighlight == true
				// valid if terminal == true
				// valid if terminal == true
				this.fieldQuery = fieldQuery;
			}

			internal virtual void AddTerm(Term term, float boost)
			{
				FieldQuery.QueryPhraseMap map = GetOrNewMap(subMap, term.Text());
				map.MarkTerminal(boost);
			}

			private FieldQuery.QueryPhraseMap GetOrNewMap(IDictionary<string, FieldQuery.QueryPhraseMap
				> subMap, string term)
			{
				FieldQuery.QueryPhraseMap map = subMap.Get(term);
				if (map == null)
				{
					map = new FieldQuery.QueryPhraseMap(fieldQuery);
					subMap.Put(term, map);
				}
				return map;
			}

			internal virtual void Add(Query query, IndexReader reader)
			{
				if (query is TermQuery)
				{
					AddTerm(((TermQuery)query).GetTerm(), query.GetBoost());
				}
				else
				{
					if (query is PhraseQuery)
					{
						PhraseQuery pq = (PhraseQuery)query;
						Term[] terms = pq.GetTerms();
						IDictionary<string, FieldQuery.QueryPhraseMap> map = subMap;
						FieldQuery.QueryPhraseMap qpm = null;
						foreach (Term term in terms)
						{
							qpm = GetOrNewMap(map, term.Text());
							map = qpm.subMap;
						}
						qpm.MarkTerminal(pq.GetSlop(), pq.GetBoost());
					}
					else
					{
						throw new RuntimeException("query \"" + query.ToString() + "\" must be flatten first."
							);
					}
				}
			}

			public virtual FieldQuery.QueryPhraseMap GetTermMap(string term)
			{
				return subMap.Get(term);
			}

			private void MarkTerminal(float boost)
			{
				MarkTerminal(0, boost);
			}

			private void MarkTerminal(int slop, float boost)
			{
				this.terminal = true;
				this.slop = slop;
				this.boost = boost;
				this.termOrPhraseNumber = fieldQuery.NextTermOrPhraseNumber();
			}

			public virtual bool IsTerminal()
			{
				return terminal;
			}

			public virtual int GetSlop()
			{
				return slop;
			}

			public virtual float GetBoost()
			{
				return boost;
			}

			public virtual int GetTermOrPhraseNumber()
			{
				return termOrPhraseNumber;
			}

			public virtual FieldQuery.QueryPhraseMap SearchPhrase(IList<FieldTermStack.TermInfo
				> phraseCandidate)
			{
				FieldQuery.QueryPhraseMap currMap = this;
				foreach (FieldTermStack.TermInfo ti in phraseCandidate)
				{
					currMap = currMap.subMap.Get(ti.GetText());
					if (currMap == null)
					{
						return null;
					}
				}
				return currMap.IsValidTermOrPhrase(phraseCandidate) ? currMap : null;
			}

			public virtual bool IsValidTermOrPhrase(IList<FieldTermStack.TermInfo> phraseCandidate
				)
			{
				// check terminal
				if (!terminal)
				{
					return false;
				}
				// if the candidate is a term, it is valid
				if (phraseCandidate.Count == 1)
				{
					return true;
				}
				// else check whether the candidate is valid phrase
				// compare position-gaps between terms to slop
				int pos = phraseCandidate[0].GetPosition();
				for (int i = 1; i < phraseCandidate.Count; i++)
				{
					int nextPos = phraseCandidate[i].GetPosition();
					if (Math.Abs(nextPos - pos - 1) > slop)
					{
						return false;
					}
					pos = nextPos;
				}
				return true;
			}
		}
	}
}
