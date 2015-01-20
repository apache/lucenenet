/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Surround.Query;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>
	/// Interface for queries that can be nested as subqueries
	/// into a span near.
	/// </summary>
	/// <remarks>
	/// Interface for queries that can be nested as subqueries
	/// into a span near.
	/// </remarks>
	public interface DistanceSubQuery
	{
		/// <summary>
		/// When distanceSubQueryNotAllowed() returns non null, the reason why the subquery
		/// is not allowed as a distance subquery is returned.
		/// </summary>
		/// <remarks>
		/// When distanceSubQueryNotAllowed() returns non null, the reason why the subquery
		/// is not allowed as a distance subquery is returned.
		/// <br />When distanceSubQueryNotAllowed() returns null addSpanNearQueries() can be used
		/// in the creation of the span near clause for the subquery.
		/// </remarks>
		string DistanceSubQueryNotAllowed();

		/// <exception cref="System.IO.IOException"></exception>
		void AddSpanQueries(SpanNearClauseFactory sncf);
	}
}
