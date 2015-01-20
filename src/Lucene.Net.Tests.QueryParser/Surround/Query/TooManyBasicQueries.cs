/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	/// <summary>
	/// Exception thrown when
	/// <see cref="BasicQueryFactory">BasicQueryFactory</see>
	/// would exceed the limit
	/// of query clauses.
	/// </summary>
	[System.Serializable]
	public class TooManyBasicQueries : IOException
	{
		public TooManyBasicQueries(int maxBasicQueries) : base("Exceeded maximum of " + maxBasicQueries
			 + " basic queries.")
		{
		}
	}
}
