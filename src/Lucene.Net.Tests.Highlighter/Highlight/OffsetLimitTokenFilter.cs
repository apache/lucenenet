/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// This TokenFilter limits the number of tokens while indexing by adding up the
	/// current offset.
	/// </summary>
	/// <remarks>
	/// This TokenFilter limits the number of tokens while indexing by adding up the
	/// current offset.
	/// </remarks>
	public sealed class OffsetLimitTokenFilter : TokenFilter
	{
		private int offsetCount;

		private OffsetAttribute offsetAttrib = GetAttribute<OffsetAttribute>();

		private int offsetLimit;

		public OffsetLimitTokenFilter(TokenStream input, int offsetLimit) : base(input)
		{
			this.offsetLimit = offsetLimit;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (offsetCount < offsetLimit && input.IncrementToken())
			{
				int offsetLength = offsetAttrib.EndOffset() - offsetAttrib.StartOffset();
				offsetCount += offsetLength;
				return true;
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			offsetCount = 0;
		}
	}
}
