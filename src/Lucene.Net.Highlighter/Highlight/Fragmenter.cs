/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Search.Highlight;
using Sharpen;

namespace Lucene.Net.Search.Highlight
{
	/// <summary>
	/// Implements the policy for breaking text into multiple fragments for
	/// consideration by the
	/// <see cref="Highlighter">Highlighter</see>
	/// class. A sophisticated
	/// implementation may do this on the basis of detecting end of sentences in the
	/// text.
	/// </summary>
	public interface Fragmenter
	{
		/// <summary>Initializes the Fragmenter.</summary>
		/// <remarks>
		/// Initializes the Fragmenter. You can grab references to the Attributes you are
		/// interested in from tokenStream and then access the values in
		/// <see cref="IsNewFragment()">IsNewFragment()</see>
		/// .
		/// </remarks>
		/// <param name="originalText">the original source text</param>
		/// <param name="tokenStream">
		/// the
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// to be fragmented
		/// </param>
		void Start(string originalText, TokenStream tokenStream);

		/// <summary>
		/// Test to see if this token from the stream should be held in a new
		/// TextFragment.
		/// </summary>
		/// <remarks>
		/// Test to see if this token from the stream should be held in a new
		/// TextFragment. Every time this is called, the TokenStream
		/// passed to start(String, TokenStream) will have been incremented.
		/// </remarks>
		bool IsNewFragment();
	}
}
