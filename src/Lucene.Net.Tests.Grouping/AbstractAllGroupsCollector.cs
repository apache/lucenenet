/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping
{
	/// <summary>
	/// A collector that collects all groups that match the
	/// query.
	/// </summary>
	/// <remarks>
	/// A collector that collects all groups that match the
	/// query. Only the group value is collected, and the order
	/// is undefined.  This collector does not determine
	/// the most relevant document of a group.
	/// <p/>
	/// This is an abstract version. Concrete implementations define
	/// what a group actually is and how it is internally collected.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractAllGroupsCollector<GROUP_VALUE_TYPE> : Collector
	{
		/// <summary>Returns the total number of groups for the executed search.</summary>
		/// <remarks>
		/// Returns the total number of groups for the executed search.
		/// This is a convenience method. The following code snippet has the same effect: <pre>getGroups().size()</pre>
		/// </remarks>
		/// <returns>The total number of groups for the executed search</returns>
		public virtual int GetGroupCount()
		{
			return GetGroups().Count;
		}

		/// <summary>
		/// Returns the group values
		/// <p/>
		/// This is an unordered collections of group values.
		/// </summary>
		/// <remarks>
		/// Returns the group values
		/// <p/>
		/// This is an unordered collections of group values. For each group that matched the query there is a
		/// <see cref="Org.Apache.Lucene.Util.BytesRef">Org.Apache.Lucene.Util.BytesRef</see>
		/// representing a group value.
		/// </remarks>
		/// <returns>the group values</returns>
		public abstract ICollection<GROUP_VALUE_TYPE> GetGroups();

		// Empty not necessary
		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}
	}
}
