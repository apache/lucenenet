/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>Utility class used to extract the terms used in a query, plus any weights.
	/// 	</summary>
	/// <remarks>
	/// Utility class used to extract the terms used in a query, plus any weights.
	/// This class will not find terms for MultiTermQuery, TermRangeQuery and PrefixQuery classes
	/// so the caller must pass a rewritten query (see Query.rewrite) to obtain a list of
	/// expanded terms.
	/// </remarks>
	public sealed class QueryTermExtractor
	{
		/// <summary>Extracts all terms texts of a given Query into an array of WeightedTerms
		/// 	</summary>
		/// <param name="query">Query to extract term texts from</param>
		/// <returns>an array of the terms used in a query, plus their weights.</returns>
		public static WeightedTerm[] GetTerms(Query query)
		{
			return GetTerms(query, false);
		}

		/// <summary>Extracts all terms texts of a given Query into an array of WeightedTerms
		/// 	</summary>
		/// <param name="query">Query to extract term texts from</param>
		/// <param name="reader">
		/// used to compute IDF which can be used to a) score selected fragments better
		/// b) use graded highlights eg changing intensity of font color
		/// </param>
		/// <param name="fieldName">the field on which Inverse Document Frequency (IDF) calculations are based
		/// 	</param>
		/// <returns>an array of the terms used in a query, plus their weights.</returns>
		public static WeightedTerm[] GetIdfWeightedTerms(Query query, IndexReader reader, 
			string fieldName)
		{
			WeightedTerm[] terms = GetTerms(query, false, fieldName);
			int totalNumDocs = reader.MaxDoc();
			for (int i = 0; i < terms.Length; i++)
			{
				try
				{
					int docFreq = reader.DocFreq(new Term(fieldName, terms[i].term));
					//IDF algorithm taken from DefaultSimilarity class
					float idf = (float)(Math.Log(totalNumDocs / (double)(docFreq + 1)) + 1.0);
					terms[i].weight *= idf;
				}
				catch (IOException)
				{
				}
			}
			//ignore
			return terms;
		}

		/// <summary>Extracts all terms texts of a given Query into an array of WeightedTerms
		/// 	</summary>
		/// <param name="query">Query to extract term texts from</param>
		/// <param name="prohibited"><code>true</code> to extract "prohibited" terms, too</param>
		/// <param name="fieldName">The fieldName used to filter query terms</param>
		/// <returns>an array of the terms used in a query, plus their weights.</returns>
		public static WeightedTerm[] GetTerms(Query query, bool prohibited, string fieldName
			)
		{
			HashSet<WeightedTerm> terms = new HashSet<WeightedTerm>();
			GetTerms(query, terms, prohibited, fieldName);
			return Sharpen.Collections.ToArray(terms, new WeightedTerm[0]);
		}

		/// <summary>Extracts all terms texts of a given Query into an array of WeightedTerms
		/// 	</summary>
		/// <param name="query">Query to extract term texts from</param>
		/// <param name="prohibited"><code>true</code> to extract "prohibited" terms, too</param>
		/// <returns>an array of the terms used in a query, plus their weights.</returns>
		public static WeightedTerm[] GetTerms(Query query, bool prohibited)
		{
			return GetTerms(query, prohibited, null);
		}

		private static void GetTerms(Query query, HashSet<WeightedTerm> terms, bool prohibited
			, string fieldName)
		{
			try
			{
				if (query is BooleanQuery)
				{
					GetTermsFromBooleanQuery((BooleanQuery)query, terms, prohibited, fieldName);
				}
				else
				{
					if (query is FilteredQuery)
					{
						GetTermsFromFilteredQuery((FilteredQuery)query, terms, prohibited, fieldName);
					}
					else
					{
						HashSet<Term> nonWeightedTerms = new HashSet<Term>();
						query.ExtractTerms(nonWeightedTerms);
						for (Iterator<Term> iter = nonWeightedTerms.Iterator(); iter.HasNext(); )
						{
							Term term = iter.Next();
							if ((fieldName == null) || (term.Field().Equals(fieldName)))
							{
								terms.AddItem(new WeightedTerm(query.GetBoost(), term.Text()));
							}
						}
					}
				}
			}
			catch (NotSupportedException)
			{
			}
		}

		//this is non-fatal for our purposes
		/// <summary>
		/// extractTerms is currently the only query-independent means of introspecting queries but it only reveals
		/// a list of terms for that query - not the boosts each individual term in that query may or may not have.
		/// </summary>
		/// <remarks>
		/// extractTerms is currently the only query-independent means of introspecting queries but it only reveals
		/// a list of terms for that query - not the boosts each individual term in that query may or may not have.
		/// "Container" queries such as BooleanQuery should be unwrapped to get at the boost info held
		/// in each child element.
		/// Some discussion around this topic here:
		/// http://www.gossamer-threads.com/lists/lucene/java-dev/34208?search_string=introspection;#34208
		/// Unfortunately there seemed to be limited interest in requiring all Query objects to implement
		/// something common which would allow access to child queries so what follows here are query-specific
		/// implementations for accessing embedded query elements.
		/// </remarks>
		private static void GetTermsFromBooleanQuery(BooleanQuery query, HashSet<WeightedTerm
			> terms, bool prohibited, string fieldName)
		{
			BooleanClause[] queryClauses = query.GetClauses();
			for (int i = 0; i < queryClauses.Length; i++)
			{
				if (prohibited || queryClauses[i].GetOccur() != BooleanClause.Occur.MUST_NOT)
				{
					GetTerms(queryClauses[i].GetQuery(), terms, prohibited, fieldName);
				}
			}
		}

		private static void GetTermsFromFilteredQuery(FilteredQuery query, HashSet<WeightedTerm
			> terms, bool prohibited, string fieldName)
		{
			GetTerms(query.GetQuery(), terms, prohibited, fieldName);
		}
	}
}
