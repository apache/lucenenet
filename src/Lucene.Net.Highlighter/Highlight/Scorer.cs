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
	/// <summary>A Scorer is responsible for scoring a stream of tokens.</summary>
	/// <remarks>
	/// A Scorer is responsible for scoring a stream of tokens. These token scores
	/// can then be used to compute
	/// <see cref="TextFragment">TextFragment</see>
	/// scores.
	/// </remarks>
	public interface Scorer
	{
		/// <summary>
		/// Called to init the Scorer with a
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// . You can grab references to
		/// the attributes you are interested in here and access them from
		/// <see cref="GetTokenScore()">GetTokenScore()</see>
		/// .
		/// </summary>
		/// <param name="tokenStream">
		/// the
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// that will be scored.
		/// </param>
		/// <returns>
		/// either a
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// that the Highlighter should continue using (eg
		/// if you read the tokenSream in this method) or null to continue
		/// using the same
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// that was passed in.
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		TokenStream Init(TokenStream tokenStream);

		/// <summary>Called when a new fragment is started for consideration.</summary>
		/// <remarks>Called when a new fragment is started for consideration.</remarks>
		/// <param name="newFragment">the fragment that will be scored next</param>
		void StartFragment(TextFragment newFragment);

		/// <summary>Called for each token in the current fragment.</summary>
		/// <remarks>
		/// Called for each token in the current fragment. The
		/// <see cref="Highlighter">Highlighter</see>
		/// will
		/// increment the
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// passed to init on every call.
		/// </remarks>
		/// <returns>
		/// a score which is passed to the
		/// <see cref="Highlighter">Highlighter</see>
		/// class to influence the
		/// mark-up of the text (this return value is NOT used to score the
		/// fragment)
		/// </returns>
		float GetTokenScore();

		/// <summary>
		/// Called when the
		/// <see cref="Highlighter">Highlighter</see>
		/// has no more tokens for the current fragment -
		/// the Scorer returns the weighting it has derived for the most recent
		/// fragment, typically based on the results of
		/// <see cref="GetTokenScore()">GetTokenScore()</see>
		/// .
		/// </summary>
		float GetFragmentScore();
	}
}
