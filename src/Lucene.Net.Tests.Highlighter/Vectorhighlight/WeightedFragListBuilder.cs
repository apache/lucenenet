/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>
	/// A weighted implementation of
	/// <see cref="FragListBuilder">FragListBuilder</see>
	/// .
	/// </summary>
	public class WeightedFragListBuilder : BaseFragListBuilder
	{
		public WeightedFragListBuilder() : base()
		{
		}

		public WeightedFragListBuilder(int margin) : base(margin)
		{
		}

		public override FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList
			, int fragCharSize)
		{
			return CreateFieldFragList(fieldPhraseList, new WeightedFieldFragList(fragCharSize
				), fragCharSize);
		}
	}
}
