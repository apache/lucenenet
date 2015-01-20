/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// <see cref="Fragmenter">Fragmenter</see>
	/// implementation which breaks text up into same-size
	/// fragments with no concerns over spotting sentence boundaries.
	/// </summary>
	public class SimpleFragmenter : Fragmenter
	{
		private const int DEFAULT_FRAGMENT_SIZE = 100;

		private int currentNumFrags;

		private int fragmentSize;

		private OffsetAttribute offsetAtt;

		public SimpleFragmenter() : this(DEFAULT_FRAGMENT_SIZE)
		{
		}

		/// <param name="fragmentSize">size in number of characters of each fragment</param>
		public SimpleFragmenter(int fragmentSize)
		{
			this.fragmentSize = fragmentSize;
		}

		public virtual void Start(string originalText, TokenStream stream)
		{
			offsetAtt = stream.AddAttribute<OffsetAttribute>();
			currentNumFrags = 1;
		}

		public virtual bool IsNewFragment()
		{
			bool isNewFrag = offsetAtt.EndOffset() >= (fragmentSize * currentNumFrags);
			if (isNewFrag)
			{
				currentNumFrags++;
			}
			return isNewFrag;
		}

		/// <returns>size in number of characters of each fragment</returns>
		public virtual int GetFragmentSize()
		{
			return fragmentSize;
		}

		/// <param name="size">size in characters of each fragment</param>
		public virtual void SetFragmentSize(int size)
		{
			fragmentSize = size;
		}
	}
}
