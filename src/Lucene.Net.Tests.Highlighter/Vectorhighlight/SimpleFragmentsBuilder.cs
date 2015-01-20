/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>A simple implementation of FragmentsBuilder.</summary>
	/// <remarks>A simple implementation of FragmentsBuilder.</remarks>
	public class SimpleFragmentsBuilder : BaseFragmentsBuilder
	{
		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		public SimpleFragmentsBuilder() : base()
		{
		}

		/// <summary>a constructor.</summary>
		/// <remarks>a constructor.</remarks>
		/// <param name="preTags">array of pre-tags for markup terms.</param>
		/// <param name="postTags">array of post-tags for markup terms.</param>
		protected internal SimpleFragmentsBuilder(string[] preTags, string[] postTags) : 
			base(preTags, postTags)
		{
		}

		protected internal SimpleFragmentsBuilder(BoundaryScanner bs) : base(bs)
		{
		}

		protected internal SimpleFragmentsBuilder(string[] preTags, string[] postTags, BoundaryScanner
			 bs) : base(preTags, postTags, bs)
		{
		}

		/// <summary>do nothing.</summary>
		/// <remarks>do nothing. return the source list.</remarks>
		public override IList<FieldFragList.WeightedFragInfo> GetWeightedFragInfoList(IList
			<FieldFragList.WeightedFragInfo> src)
		{
			return src;
		}
	}
}
