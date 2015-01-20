/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Messages
{
	/// <summary>Flexible Query Parser message bundle class</summary>
	public class QueryParserMessages : NLS
	{
		private static readonly string BUNDLE_NAME = typeof(Org.Apache.Lucene.Queryparser.Flexible.Core.Messages.QueryParserMessages
			).FullName;

		public QueryParserMessages()
		{
		}

		static QueryParserMessages()
		{
			// Do not instantiate
			// register all string ids with NLS class and initialize static string
			// values
			NLS.InitializeMessages(BUNDLE_NAME, typeof(Org.Apache.Lucene.Queryparser.Flexible.Core.Messages.QueryParserMessages
				));
		}

		public static string INVALID_SYNTAX;

		public static string INVALID_SYNTAX_CANNOT_PARSE;

		public static string INVALID_SYNTAX_FUZZY_LIMITS;

		public static string INVALID_SYNTAX_FUZZY_EDITS;

		public static string INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

		public static string INVALID_SYNTAX_ESCAPE_CHARACTER;

		public static string INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

		public static string NODE_ACTION_NOT_SUPPORTED;

		public static string PARAMETER_VALUE_NOT_SUPPORTED;

		public static string LUCENE_QUERY_CONVERSION_ERROR;

		public static string EMPTY_MESSAGE;

		public static string WILDCARD_NOT_SUPPORTED;

		public static string TOO_MANY_BOOLEAN_CLAUSES;

		public static string LEADING_WILDCARD_NOT_ALLOWED;

		public static string COULD_NOT_PARSE_NUMBER;

		public static string NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY;

		public static string UNSUPPORTED_NUMERIC_DATA_TYPE;

		public static string NUMERIC_CANNOT_BE_EMPTY;
		// static string must match the strings in the property files.
	}
}
