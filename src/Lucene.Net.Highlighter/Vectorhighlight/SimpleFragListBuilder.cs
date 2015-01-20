/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// A simple implementation of
	/// <see cref="FragListBuilder">FragListBuilder</see>
	/// .
	/// </summary>
	public class SimpleFragListBuilder : BaseFragListBuilder
	{
		public SimpleFragListBuilder() : base()
		{
		}

		public SimpleFragListBuilder(int margin) : base(margin)
		{
		}

		public override FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList
			, int fragCharSize)
		{
			return CreateFieldFragList(fieldPhraseList, new SimpleFieldFragList(fragCharSize)
				, fragCharSize);
		}
	}
}
