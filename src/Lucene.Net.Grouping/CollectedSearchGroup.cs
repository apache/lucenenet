/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search.Grouping;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>
	/// Expert: representation of a group in
	/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}">AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// ,
	/// tracking the top doc and
	/// <see cref="Lucene.Net.Search.FieldComparator{T}">Lucene.Net.Search.FieldComparator&lt;T&gt;
	/// 	</see>
	/// slot.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class CollectedSearchGroup<T> : SearchGroup<T>
	{
		internal int topDoc;

		internal int comparatorSlot;
		// javadocs
	}
}
