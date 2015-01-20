/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>FragListBuilder is an interface for FieldFragList builder classes.</summary>
	/// <remarks>
	/// FragListBuilder is an interface for FieldFragList builder classes.
	/// A FragListBuilder class can be plugged in to Highlighter.
	/// </remarks>
	public interface FragListBuilder
	{
		/// <summary>create a FieldFragList.</summary>
		/// <remarks>create a FieldFragList.</remarks>
		/// <param name="fieldPhraseList">FieldPhraseList object</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <returns>the created FieldFragList object</returns>
		FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, int fragCharSize
			);
	}
}
