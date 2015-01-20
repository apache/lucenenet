/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search.Postingshighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Postingshighlight
{
	/// <summary>Creates a formatted snippet from the top passages.</summary>
	/// <remarks>Creates a formatted snippet from the top passages.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class PassageFormatter
	{
		/// <summary>
		/// Formats the top <code>passages</code> from <code>content</code>
		/// into a human-readable text snippet.
		/// </summary>
		/// <remarks>
		/// Formats the top <code>passages</code> from <code>content</code>
		/// into a human-readable text snippet.
		/// </remarks>
		/// <param name="passages">
		/// top-N passages for the field. Note these are sorted in
		/// the order that they appear in the document for convenience.
		/// </param>
		/// <param name="content">content for the field.</param>
		/// <returns>
		/// formatted highlight.  Note that for the
		/// non-expert APIs in
		/// <see cref="PostingsHighlighter">PostingsHighlighter</see>
		/// that
		/// return String, the toString method on the Object
		/// returned by this method is used to compute the string.
		/// </returns>
		public abstract object Format(Passage[] passages, string content);
	}
}
