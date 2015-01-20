/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
	/// is an interface for fragments (snippets) builder classes.
	/// A
	/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
	/// class can be plugged in to
	/// <see cref="FastVectorHighlighter">FastVectorHighlighter</see>
	/// .
	/// </summary>
	public interface FragmentsBuilder
	{
		/// <summary>create a fragment.</summary>
		/// <remarks>create a fragment.</remarks>
		/// <param name="reader">IndexReader of the index</param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fieldFragList">FieldFragList object</param>
		/// <returns>a created fragment or null when no fragment created</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		string CreateFragment(IndexReader reader, int docId, string fieldName, FieldFragList
			 fieldFragList);

		/// <summary>create multiple fragments.</summary>
		/// <remarks>create multiple fragments.</remarks>
		/// <param name="reader">IndexReader of the index</param>
		/// <param name="docId">document id to be highlighter</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fieldFragList">FieldFragList object</param>
		/// <param name="maxNumFragments">maximum number of fragments</param>
		/// <returns>
		/// created fragments or null when no fragments created.
		/// size of the array can be less than maxNumFragments
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		string[] CreateFragments(IndexReader reader, int docId, string fieldName, FieldFragList
			 fieldFragList, int maxNumFragments);

		/// <summary>create a fragment.</summary>
		/// <remarks>create a fragment.</remarks>
		/// <param name="reader">IndexReader of the index</param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fieldFragList">FieldFragList object</param>
		/// <param name="preTags">pre-tags to be used to highlight terms</param>
		/// <param name="postTags">post-tags to be used to highlight terms</param>
		/// <param name="encoder">an encoder that generates encoded text</param>
		/// <returns>a created fragment or null when no fragment created</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		string CreateFragment(IndexReader reader, int docId, string fieldName, FieldFragList
			 fieldFragList, string[] preTags, string[] postTags, Encoder encoder);

		/// <summary>create multiple fragments.</summary>
		/// <remarks>create multiple fragments.</remarks>
		/// <param name="reader">IndexReader of the index</param>
		/// <param name="docId">document id to be highlighter</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fieldFragList">FieldFragList object</param>
		/// <param name="maxNumFragments">maximum number of fragments</param>
		/// <param name="preTags">pre-tags to be used to highlight terms</param>
		/// <param name="postTags">post-tags to be used to highlight terms</param>
		/// <param name="encoder">an encoder that generates encoded text</param>
		/// <returns>
		/// created fragments or null when no fragments created.
		/// size of the array can be less than maxNumFragments
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		string[] CreateFragments(IndexReader reader, int docId, string fieldName, FieldFragList
			 fieldFragList, int maxNumFragments, string[] preTags, string[] postTags, Encoder
			 encoder);
	}
}
