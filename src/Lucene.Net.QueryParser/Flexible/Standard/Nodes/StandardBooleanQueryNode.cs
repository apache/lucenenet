/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="StandardBooleanQueryNode">StandardBooleanQueryNode</see>
	/// has the same behavior as
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// . It only indicates if the coord should be enabled or
	/// not for this boolean query. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Search.Similarities.Similarity.Coord(int, int)">Lucene.Net.Search.Similarities.Similarity.Coord(int, int)
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Search.BooleanQuery">Lucene.Net.Search.BooleanQuery
	/// 	</seealso>
	public class StandardBooleanQueryNode : BooleanQueryNode
	{
		private bool disableCoord;

		public StandardBooleanQueryNode(IList<QueryNode> clauses, bool disableCoord) : base
			(clauses)
		{
			this.disableCoord = disableCoord;
		}

		public virtual bool IsDisableCoord()
		{
			return this.disableCoord;
		}
	}
}
