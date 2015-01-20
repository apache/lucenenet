/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>Encodes original text.</summary>
	/// <remarks>
	/// Encodes original text. The Encoder works with the
	/// <see cref="Formatter">Formatter</see>
	/// to generate output.
	/// </remarks>
	public interface Encoder
	{
		/// <param name="originalText">The section of text being output</param>
		string EncodeText(string originalText);
	}
}
