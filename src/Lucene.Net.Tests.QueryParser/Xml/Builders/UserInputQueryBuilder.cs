/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Queryparser.Classic;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// UserInputQueryBuilder uses 1 of 2 strategies for thread-safe parsing:
	/// 1) Synchronizing access to "parse" calls on a previously supplied QueryParser
	/// or..
	/// </summary>
	/// <remarks>
	/// UserInputQueryBuilder uses 1 of 2 strategies for thread-safe parsing:
	/// 1) Synchronizing access to "parse" calls on a previously supplied QueryParser
	/// or..
	/// 2) creating a new QueryParser object for each parse request
	/// </remarks>
	public class UserInputQueryBuilder : QueryBuilder
	{
		private QueryParser unSafeParser;

		private Analyzer analyzer;

		private string defaultField;

		/// <summary>This constructor has the disadvantage of not being able to change choice of default field name
		/// 	</summary>
		/// <param name="parser">thread un-safe query parser</param>
		public UserInputQueryBuilder(QueryParser parser)
		{
			this.unSafeParser = parser;
		}

		public UserInputQueryBuilder(string defaultField, Analyzer analyzer)
		{
			this.analyzer = analyzer;
			this.defaultField = defaultField;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			string text = DOMUtils.GetText(e);
			try
			{
				Query q = null;
				if (unSafeParser != null)
				{
					//synchronize on unsafe parser
					lock (unSafeParser)
					{
						q = unSafeParser.Parse(text);
					}
				}
				else
				{
					string fieldName = DOMUtils.GetAttribute(e, "fieldName", defaultField);
					//Create new parser
					QueryParser parser = CreateQueryParser(fieldName, analyzer);
					q = parser.Parse(text);
				}
				q.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
				return q;
			}
			catch (ParseException e1)
			{
				throw new ParserException(e1.Message);
			}
		}

		/// <summary>Method to create a QueryParser - designed to be overridden</summary>
		/// <returns>QueryParser</returns>
		protected internal virtual QueryParser CreateQueryParser(string fieldName, Analyzer
			 analyzer)
		{
			return new QueryParser(Version.LUCENE_CURRENT, fieldName, analyzer);
		}
	}
}
