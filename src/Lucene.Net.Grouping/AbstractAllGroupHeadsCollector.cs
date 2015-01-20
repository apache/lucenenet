using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Grouping
{
	/// <summary>This collector specializes in collecting the most relevant document (group head) for each group that match the query.
	/// 	</summary>
	/// <remarks>This collector specializes in collecting the most relevant document (group head) for each group that match the query.
	/// 	</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractAllGroupHeadsCollector<GH> : Collector where GH:AbstractAllGroupHeadsCollector.GroupHead
	{
		protected internal readonly int[] reversed;

		protected internal readonly int compIDXEnd;

		protected internal readonly AbstractAllGroupHeadsCollector.TemporalResult temporalResult;

		protected internal AbstractAllGroupHeadsCollector(int numberOfSorts)
		{
			this.reversed = new int[numberOfSorts];
			this.compIDXEnd = numberOfSorts - 1;
			temporalResult = new AbstractAllGroupHeadsCollector.TemporalResult(this);
		}

		/// <param name="maxDoc">
		/// The maxDoc of the top level
		/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
		/// 	</see>
		/// .
		/// </param>
		/// <returns>
		/// a
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// containing all group heads.
		/// </returns>
		public virtual FixedBitSet RetrieveGroupHeads(int maxDoc)
		{
			FixedBitSet bitSet = new FixedBitSet(maxDoc);
			ICollection<GH> groupHeads = GetCollectedGroupHeads();
			foreach (AbstractAllGroupHeadsCollector.GroupHead groupHead in groupHeads)
			{
				bitSet.Set(groupHead.doc);
			}
			return bitSet;
		}

		/// <returns>an int array containing all group heads. The size of the array is equal to number of collected unique groups.
		/// 	</returns>
		public virtual int[] RetrieveGroupHeads()
		{
			ICollection<GH> groupHeads = GetCollectedGroupHeads();
			int[] docHeads = new int[groupHeads.Count];
			int i = 0;
			foreach (AbstractAllGroupHeadsCollector.GroupHead groupHead in groupHeads)
			{
				docHeads[i++] = groupHead.doc;
			}
			return docHeads;
		}

		/// <returns>the number of group heads found for a query.</returns>
		public virtual int GroupHeadsSize()
		{
			return GetCollectedGroupHeads().Count;
		}

		/// <summary>
		/// Returns the group head and puts it into
		/// <see cref="AbstractAllGroupHeadsCollector{GH}.temporalResult">AbstractAllGroupHeadsCollector&lt;GH&gt;.temporalResult
		/// 	</see>
		/// .
		/// If the group head wasn't encountered before then it will be added to the collected group heads.
		/// <p/>
		/// The
		/// <see cref="TemporalResult.stop">TemporalResult.stop</see>
		/// property will be <code>true</code> if the group head wasn't encountered before
		/// otherwise <code>false</code>.
		/// </summary>
		/// <param name="doc">The document to retrieve the group head for.</param>
		/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
		protected internal abstract void RetrieveGroupHeadAndAddIfNotExist(int doc);

		/// <summary>Returns the collected group heads.</summary>
		/// <remarks>
		/// Returns the collected group heads.
		/// Subsequent calls should return the same group heads.
		/// </remarks>
		/// <returns>the collected group heads</returns>
		protected internal abstract ICollection<GH> GetCollectedGroupHeads();

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			RetrieveGroupHeadAndAddIfNotExist(doc);
			if (temporalResult.stop)
			{
				return;
			}
			GH groupHead = temporalResult.groupHead;
			// Ok now we need to check if the current doc is more relevant then current doc for this group
			for (int compIDX = 0; ; compIDX++)
			{
				int c = reversed[compIDX] * groupHead.Compare(compIDX, doc);
				if (c < 0)
				{
					// Definitely not competitive. So don't even bother to continue
					return;
				}
				else
				{
					if (c > 0)
					{
						// Definitely competitive.
						break;
					}
					else
					{
						if (compIDX == compIDXEnd)
						{
							// Here c=0. If we're at the last comparator, this doc is not
							// competitive, since docs are visited in doc Id order, which means
							// this doc cannot compete with any other document in the queue.
							return;
						}
					}
				}
			}
			groupHead.UpdateDocHead(doc);
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return false;
		}

		/// <summary>Contains the result of group head retrieval.</summary>
		/// <remarks>
		/// Contains the result of group head retrieval.
		/// To prevent new object creations of this class for every collect.
		/// </remarks>
		protected internal class TemporalResult
		{
			public GH groupHead;

			public bool stop;

			internal TemporalResult(AbstractAllGroupHeadsCollector<GH> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly AbstractAllGroupHeadsCollector<GH> _enclosing;
		}

		/// <summary>Represents a group head.</summary>
		/// <remarks>
		/// Represents a group head. A group head is the most relevant document for a particular group.
		/// The relevancy is based is usually based on the sort.
		/// The group head contains a group value with its associated most relevant document id.
		/// </remarks>
		public abstract class GroupHead<GROUP_VALUE_TYPE>
		{
			public readonly GROUP_VALUE_TYPE groupValue;

			public int doc;

			protected internal GroupHead(GROUP_VALUE_TYPE groupValue, int doc)
			{
				this.groupValue = groupValue;
				this.doc = doc;
			}

			/// <summary>Compares the specified document for a specified comparator against the current most relevant document.
			/// 	</summary>
			/// <remarks>Compares the specified document for a specified comparator against the current most relevant document.
			/// 	</remarks>
			/// <param name="compIDX">The comparator index of the specified comparator.</param>
			/// <param name="doc">The specified document.</param>
			/// <returns>
			/// -1 if the specified document wasn't competitive against the current most relevant document, 1 if the
			/// specified document was competitive against the current most relevant document. Otherwise 0.
			/// </returns>
			/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
			protected internal abstract int Compare(int compIDX, int doc);

			/// <summary>Updates the current most relevant document with the specified document.</summary>
			/// <remarks>Updates the current most relevant document with the specified document.</remarks>
			/// <param name="doc">The specified document</param>
			/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
			protected internal abstract void UpdateDocHead(int doc);
		}
	}
}
