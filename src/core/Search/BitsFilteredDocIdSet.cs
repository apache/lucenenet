using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	public sealed class BitsFilteredDocIdSet : FilteredDocIdSet
	{
		private IBits acceptDocs;

		///**
		// * Convenience wrapper method: If {@code acceptDocs == null} it returns the original set without wrapping.
		// * @param set Underlying DocIdSet. If {@code null}, this method returns {@code null}
		// * @param acceptDocs Allowed docs, all docids not in this set will not be returned by this DocIdSet.
		// * If {@code null}, this method returns the original set without wrapping.
		// */
		public static DocIdSet Wrap(DocIdSet set, IBits acceptDocs)
		{
			return (set == null || acceptDocs == null) ? set : new BitsFilteredDocIdSet(set, acceptDocs);
		}

		///**
		// * Constructor.
		// * @param innerSet Underlying DocIdSet
		// * @param acceptDocs Allowed docs, all docids not in this set will not be returned by this DocIdSet
		// */
		public BitsFilteredDocIdSet(DocIdSet innerSet, IBits acceptDocs)
            : base(innerSet)
		{
			if (acceptDocs == null)
				throw new NullReferenceException("acceptDocs is null");
			this.acceptDocs = acceptDocs;
		}
        
		public override bool Match(int docid)
		{
			return acceptDocs[docid];
		}

	}
}
