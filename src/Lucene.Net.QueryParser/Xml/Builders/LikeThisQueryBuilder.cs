/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Queries.Mlt.MoreLikeThisQuery">Lucene.Net.Queries.Mlt.MoreLikeThisQuery
	/// 	</see>
	/// </summary>
	public class LikeThisQueryBuilder : QueryBuilder
	{
		private const int DEFAULT_MAX_QUERY_TERMS = 20;

		private const int DEFAULT_MIN_TERM_FREQUENCY = 1;

		private const float DEFAULT_PERCENT_TERMS_TO_MATCH = 30;

		private readonly Analyzer analyzer;

		private readonly string defaultFieldNames;

		public LikeThisQueryBuilder(Analyzer analyzer, string[] defaultFieldNames)
		{
			//default is a 3rd of selected terms must match
			this.analyzer = analyzer;
			this.defaultFieldNames = defaultFieldNames;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			string fieldsList = e.GetAttribute("fieldNames");
			//a comma-delimited list of fields
			string[] fields = defaultFieldNames;
			if ((fieldsList != null) && (fieldsList.Trim().Length > 0))
			{
				fields = fieldsList.Trim().Split(",");
				//trim the fieldnames
				for (int i = 0; i < fields.Length; i++)
				{
					fields[i] = fields[i].Trim();
				}
			}
			//Parse any "stopWords" attribute
			//TODO MoreLikeThis needs to ideally have per-field stopWords lists - until then
			//I use all analyzers/fields to generate multi-field compatible stop list
			string stopWords = e.GetAttribute("stopWords");
			ICollection<string> stopWordsSet = null;
			if ((stopWords != null) && (fields != null))
			{
				stopWordsSet = new HashSet<string>();
				foreach (string field in fields)
				{
					TokenStream ts = null;
					try
					{
						ts = analyzer.TokenStream(field, stopWords);
						CharTermAttribute termAtt = ts.AddAttribute<CharTermAttribute>();
						ts.Reset();
						while (ts.IncrementToken())
						{
							stopWordsSet.AddItem(termAtt.ToString());
						}
						ts.End();
					}
					catch (IOException ioe)
					{
						throw new ParserException("IoException parsing stop words list in " + GetType().FullName
							 + ":" + ioe.GetLocalizedMessage());
					}
					finally
					{
						IOUtils.CloseWhileHandlingException(ts);
					}
				}
			}
			MoreLikeThisQuery mlt = new MoreLikeThisQuery(DOMUtils.GetText(e), fields, analyzer
				, fields[0]);
			mlt.SetMaxQueryTerms(DOMUtils.GetAttribute(e, "maxQueryTerms", DEFAULT_MAX_QUERY_TERMS
				));
			mlt.SetMinTermFrequency(DOMUtils.GetAttribute(e, "minTermFrequency", DEFAULT_MIN_TERM_FREQUENCY
				));
			mlt.SetPercentTermsToMatch(DOMUtils.GetAttribute(e, "percentTermsToMatch", DEFAULT_PERCENT_TERMS_TO_MATCH
				) / 100);
			mlt.SetStopWords(stopWordsSet);
			int minDocFreq = DOMUtils.GetAttribute(e, "minDocFreq", -1);
			if (minDocFreq >= 0)
			{
				mlt.SetMinDocFreq(minDocFreq);
			}
			mlt.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return mlt;
		}
	}
}
