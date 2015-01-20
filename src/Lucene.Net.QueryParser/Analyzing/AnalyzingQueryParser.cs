/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Analyzing
{
	/// <summary>
	/// Overrides Lucene's default QueryParser so that Fuzzy-, Prefix-, Range-, and WildcardQuerys
	/// are also passed through the given analyzer, but wildcard characters <code>*</code> and
	/// <code>?</code> don't get removed from the search terms.
	/// </summary>
	/// <remarks>
	/// Overrides Lucene's default QueryParser so that Fuzzy-, Prefix-, Range-, and WildcardQuerys
	/// are also passed through the given analyzer, but wildcard characters <code>*</code> and
	/// <code>?</code> don't get removed from the search terms.
	/// <p><b>Warning:</b> This class should only be used with analyzers that do not use stopwords
	/// or that add tokens. Also, several stemming analyzers are inappropriate: for example, GermanAnalyzer
	/// will turn <code>H&auml;user</code> into <code>hau</code>, but <code>H?user</code> will
	/// become <code>h?user</code> when using this parser and thus no match would be found (i.e.
	/// using this parser will be no improvement over QueryParser in such cases).
	/// </remarks>
	public class AnalyzingQueryParser : QueryParser
	{
		private readonly Sharpen.Pattern wildcardPattern = Sharpen.Pattern.Compile("(\\.)|([?*]+)"
			);

		public AnalyzingQueryParser(Version matchVersion, string field, Analyzer analyzer
			) : base(matchVersion, field, analyzer)
		{
			// gobble escaped chars or find a wildcard character 
			SetAnalyzeRangeTerms(true);
		}

		/// <summary>
		/// Called when parser parses an input term that contains one or more wildcard
		/// characters (like <code>*</code>), but is not a prefix term (one that has
		/// just a single <code>*</code> character at the end).
		/// </summary>
		/// <remarks>
		/// Called when parser parses an input term that contains one or more wildcard
		/// characters (like <code>*</code>), but is not a prefix term (one that has
		/// just a single <code>*</code> character at the end).
		/// <p>
		/// Example: will be called for <code>H?user</code> or for <code>H*user</code>.
		/// <p>
		/// Depending on analyzer and settings, a wildcard term may (most probably will)
		/// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
		/// <p>
		/// Overrides super class, by passing terms through analyzer.
		/// </remarks>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">
		/// Term that contains one or more wildcard
		/// characters (? or *), but is not simple prefix term
		/// </param>
		/// <returns>
		/// Resulting
		/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetWildcardQuery(string field, string termStr)
		{
			if (termStr == null)
			{
				//can't imagine this would ever happen
				throw new ParseException("Passed null value as term to getWildcardQuery");
			}
			if (!GetAllowLeadingWildcard() && (termStr.StartsWith("*") || termStr.StartsWith(
				"?")))
			{
				throw new ParseException("'*' or '?' not allowed as first character in WildcardQuery"
					 + " unless getAllowLeadingWildcard() returns true");
			}
			Matcher wildcardMatcher = wildcardPattern.Matcher(termStr);
			StringBuilder sb = new StringBuilder();
			int last = 0;
			while (wildcardMatcher.Find())
			{
				// continue if escaped char
				if (wildcardMatcher.Group(1) != null)
				{
					continue;
				}
				if (wildcardMatcher.Start() > 0)
				{
					string chunk = Sharpen.Runtime.Substring(termStr, last, wildcardMatcher.Start());
					string analyzed = AnalyzeSingleChunk(field, termStr, chunk);
					sb.Append(analyzed);
				}
				//append the wildcard character
				sb.Append(wildcardMatcher.Group(2));
				last = wildcardMatcher.End();
			}
			if (last < termStr.Length)
			{
				sb.Append(AnalyzeSingleChunk(field, termStr, Sharpen.Runtime.Substring(termStr, last
					)));
			}
			return base.GetWildcardQuery(field, sb.ToString());
		}

		/// <summary>
		/// Called when parser parses an input term
		/// that uses prefix notation; that is, contains a single '*' wildcard
		/// character as its last character.
		/// </summary>
		/// <remarks>
		/// Called when parser parses an input term
		/// that uses prefix notation; that is, contains a single '*' wildcard
		/// character as its last character. Since this is a special case
		/// of generic wildcard term, and such a query can be optimized easily,
		/// this usually results in a different query object.
		/// <p>
		/// Depending on analyzer and settings, a prefix term may (most probably will)
		/// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
		/// <p>
		/// Overrides super class, by passing terms through analyzer.
		/// </remarks>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">
		/// Term to use for building term for the query
		/// (<b>without</b> trailing '*' character!)
		/// </param>
		/// <returns>
		/// Resulting
		/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetPrefixQuery(string field, string termStr)
		{
			string analyzed = AnalyzeSingleChunk(field, termStr, termStr);
			return base.GetPrefixQuery(field, analyzed);
		}

		/// <summary>Called when parser parses an input term that has the fuzzy suffix (~) appended.
		/// 	</summary>
		/// <remarks>
		/// Called when parser parses an input term that has the fuzzy suffix (~) appended.
		/// <p>
		/// Depending on analyzer and settings, a fuzzy term may (most probably will)
		/// be lower-cased automatically. It <b>will</b> go through the default Analyzer.
		/// <p>
		/// Overrides super class, by passing terms through analyzer.
		/// </remarks>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">Term to use for building term for the query</param>
		/// <returns>
		/// Resulting
		/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetFuzzyQuery(string field, string termStr, float
			 minSimilarity)
		{
			string analyzed = AnalyzeSingleChunk(field, termStr, termStr);
			return base.GetFuzzyQuery(field, analyzed, minSimilarity);
		}

		/// <summary>
		/// Returns the analyzed form for the given chunk
		/// If the analyzer produces more than one output token from the given chunk,
		/// a ParseException is thrown.
		/// </summary>
		/// <remarks>
		/// Returns the analyzed form for the given chunk
		/// If the analyzer produces more than one output token from the given chunk,
		/// a ParseException is thrown.
		/// </remarks>
		/// <param name="field">The target field</param>
		/// <param name="termStr">The full term from which the given chunk is excerpted</param>
		/// <param name="chunk">The portion of the given termStr to be analyzed</param>
		/// <returns>The result of analyzing the given chunk</returns>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException">when analysis returns other than one output token
		/// 	</exception>
		protected internal virtual string AnalyzeSingleChunk(string field, string termStr
			, string chunk)
		{
			string analyzed = null;
			TokenStream stream = null;
			try
			{
				stream = GetAnalyzer().TokenStream(field, chunk);
				stream.Reset();
				CharTermAttribute termAtt = stream.GetAttribute<CharTermAttribute>();
				// get first and hopefully only output token
				if (stream.IncrementToken())
				{
					analyzed = termAtt.ToString();
					// try to increment again, there should only be one output token
					StringBuilder multipleOutputs = null;
					while (stream.IncrementToken())
					{
						if (null == multipleOutputs)
						{
							multipleOutputs = new StringBuilder();
							multipleOutputs.Append('"');
							multipleOutputs.Append(analyzed);
							multipleOutputs.Append('"');
						}
						multipleOutputs.Append(',');
						multipleOutputs.Append('"');
						multipleOutputs.Append(termAtt.ToString());
						multipleOutputs.Append('"');
					}
					stream.End();
					if (null != multipleOutputs)
					{
						throw new ParseException(string.Format(GetLocale(), "Analyzer created multiple terms for \"%s\": %s"
							, chunk, multipleOutputs.ToString()));
					}
				}
				else
				{
					// nothing returned by analyzer.  Was it a stop word and the user accidentally
					// used an analyzer with stop words?
					stream.End();
					throw new ParseException(string.Format(GetLocale(), "Analyzer returned nothing for \"%s\""
						, chunk));
				}
			}
			catch (IOException)
			{
				throw new ParseException(string.Format(GetLocale(), "IO error while trying to analyze single term: \"%s\""
					, termStr));
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(stream);
			}
			return analyzed;
		}
	}
}
