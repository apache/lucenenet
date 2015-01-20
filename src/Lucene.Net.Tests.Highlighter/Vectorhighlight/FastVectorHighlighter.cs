/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Highlight;
using Org.Apache.Lucene.Search.Vectorhighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Vectorhighlight
{
	/// <summary>Another highlighter implementation.</summary>
	/// <remarks>Another highlighter implementation.</remarks>
	public class FastVectorHighlighter
	{
		public const bool DEFAULT_PHRASE_HIGHLIGHT = true;

		public const bool DEFAULT_FIELD_MATCH = true;

		private readonly bool phraseHighlight;

		private readonly bool fieldMatch;

		private readonly FragListBuilder fragListBuilder;

		private readonly FragmentsBuilder fragmentsBuilder;

		private int phraseLimit = int.MaxValue;

		/// <summary>the default constructor.</summary>
		/// <remarks>the default constructor.</remarks>
		public FastVectorHighlighter() : this(DEFAULT_PHRASE_HIGHLIGHT, DEFAULT_FIELD_MATCH
			)
		{
		}

		/// <summary>a constructor.</summary>
		/// <remarks>
		/// a constructor. Using
		/// <see cref="SimpleFragListBuilder">SimpleFragListBuilder</see>
		/// and
		/// <see cref="ScoreOrderFragmentsBuilder">ScoreOrderFragmentsBuilder</see>
		/// .
		/// </remarks>
		/// <param name="phraseHighlight">true or false for phrase highlighting</param>
		/// <param name="fieldMatch">true of false for field matching</param>
		public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch) : this(phraseHighlight
			, fieldMatch, new SimpleFragListBuilder(), new ScoreOrderFragmentsBuilder())
		{
		}

		/// <summary>a constructor.</summary>
		/// <remarks>
		/// a constructor. A
		/// <see cref="FragListBuilder">FragListBuilder</see>
		/// and a
		/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
		/// can be specified (plugins).
		/// </remarks>
		/// <param name="phraseHighlight">true of false for phrase highlighting</param>
		/// <param name="fieldMatch">true of false for field matching</param>
		/// <param name="fragListBuilder">
		/// an instance of
		/// <see cref="FragListBuilder">FragListBuilder</see>
		/// </param>
		/// <param name="fragmentsBuilder">
		/// an instance of
		/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
		/// </param>
		public FastVectorHighlighter(bool phraseHighlight, bool fieldMatch, FragListBuilder
			 fragListBuilder, FragmentsBuilder fragmentsBuilder)
		{
			this.phraseHighlight = phraseHighlight;
			this.fieldMatch = fieldMatch;
			this.fragListBuilder = fragListBuilder;
			this.fragmentsBuilder = fragmentsBuilder;
		}

		/// <summary>
		/// create a
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object.
		/// </summary>
		/// <param name="query">a query</param>
		/// <returns>
		/// the created
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </returns>
		public virtual FieldQuery GetFieldQuery(Query query)
		{
			// TODO: should we deprecate this? 
			// because if there is no reader, then we cannot rewrite MTQ.
			try
			{
				return new FieldQuery(query, null, phraseHighlight, fieldMatch);
			}
			catch (IOException e)
			{
				// should never be thrown when reader is null
				throw new RuntimeException(e);
			}
		}

		/// <summary>
		/// create a
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object.
		/// </summary>
		/// <param name="query">a query</param>
		/// <returns>
		/// the created
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual FieldQuery GetFieldQuery(Query query, IndexReader reader)
		{
			return new FieldQuery(query, reader, phraseHighlight, fieldMatch);
		}

		/// <summary>return the best fragment.</summary>
		/// <remarks>return the best fragment.</remarks>
		/// <param name="fieldQuery">
		/// 
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// of the index
		/// </param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <returns>the best fragment (snippet) string</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId
			, string fieldName, int fragCharSize)
		{
			FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader
				, docId, fieldName, fragCharSize);
			return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList);
		}

		/// <summary>return the best fragments.</summary>
		/// <remarks>return the best fragments.</remarks>
		/// <param name="fieldQuery">
		/// 
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// of the index
		/// </param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <param name="maxNumFragments">maximum number of fragments</param>
		/// <returns>
		/// created fragments or null when no fragments created.
		/// size of the array can be less than maxNumFragments
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId
			, string fieldName, int fragCharSize, int maxNumFragments)
		{
			FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader
				, docId, fieldName, fragCharSize);
			return fragmentsBuilder.CreateFragments(reader, docId, fieldName, fieldFragList, 
				maxNumFragments);
		}

		/// <summary>return the best fragment.</summary>
		/// <remarks>return the best fragment.</remarks>
		/// <param name="fieldQuery">
		/// 
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// of the index
		/// </param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <param name="fragListBuilder">
		/// 
		/// <see cref="FragListBuilder">FragListBuilder</see>
		/// object
		/// </param>
		/// <param name="fragmentsBuilder">
		/// 
		/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
		/// object
		/// </param>
		/// <param name="preTags">pre-tags to be used to highlight terms</param>
		/// <param name="postTags">post-tags to be used to highlight terms</param>
		/// <param name="encoder">an encoder that generates encoded text</param>
		/// <returns>the best fragment (snippet) string</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public string GetBestFragment(FieldQuery fieldQuery, IndexReader reader, int docId
			, string fieldName, int fragCharSize, FragListBuilder fragListBuilder, FragmentsBuilder
			 fragmentsBuilder, string[] preTags, string[] postTags, Encoder encoder)
		{
			FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader
				, docId, fieldName, fragCharSize);
			return fragmentsBuilder.CreateFragment(reader, docId, fieldName, fieldFragList, preTags
				, postTags, encoder);
		}

		/// <summary>return the best fragments.</summary>
		/// <remarks>return the best fragments.</remarks>
		/// <param name="fieldQuery">
		/// 
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// of the index
		/// </param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="fieldName">field of the document to be highlighted</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <param name="maxNumFragments">maximum number of fragments</param>
		/// <param name="fragListBuilder">
		/// 
		/// <see cref="FragListBuilder">FragListBuilder</see>
		/// object
		/// </param>
		/// <param name="fragmentsBuilder">
		/// 
		/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
		/// object
		/// </param>
		/// <param name="preTags">pre-tags to be used to highlight terms</param>
		/// <param name="postTags">post-tags to be used to highlight terms</param>
		/// <param name="encoder">an encoder that generates encoded text</param>
		/// <returns>
		/// created fragments or null when no fragments created.
		/// size of the array can be less than maxNumFragments
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId
			, string fieldName, int fragCharSize, int maxNumFragments, FragListBuilder fragListBuilder
			, FragmentsBuilder fragmentsBuilder, string[] preTags, string[] postTags, Encoder
			 encoder)
		{
			FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader
				, docId, fieldName, fragCharSize);
			return fragmentsBuilder.CreateFragments(reader, docId, fieldName, fieldFragList, 
				maxNumFragments, preTags, postTags, encoder);
		}

		/// <summary>Return the best fragments.</summary>
		/// <remarks>
		/// Return the best fragments.  Matches are scanned from matchedFields and turned into fragments against
		/// storedField.  The highlighting may not make sense if matchedFields has matches with offsets that don't
		/// correspond features in storedField.  It will outright throw a
		/// <code>StringIndexOutOfBoundsException</code>
		/// if matchedFields produces offsets outside of storedField.  As such it is advisable that all
		/// matchedFields share the same source as storedField or are at least a prefix of it.
		/// </remarks>
		/// <param name="fieldQuery">
		/// 
		/// <see cref="FieldQuery">FieldQuery</see>
		/// object
		/// </param>
		/// <param name="reader">
		/// 
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// of the index
		/// </param>
		/// <param name="docId">document id to be highlighted</param>
		/// <param name="storedField">field of the document that stores the text</param>
		/// <param name="matchedFields">fields of the document to scan for matches</param>
		/// <param name="fragCharSize">the length (number of chars) of a fragment</param>
		/// <param name="maxNumFragments">maximum number of fragments</param>
		/// <param name="fragListBuilder">
		/// 
		/// <see cref="FragListBuilder">FragListBuilder</see>
		/// object
		/// </param>
		/// <param name="fragmentsBuilder">
		/// 
		/// <see cref="FragmentsBuilder">FragmentsBuilder</see>
		/// object
		/// </param>
		/// <param name="preTags">pre-tags to be used to highlight terms</param>
		/// <param name="postTags">post-tags to be used to highlight terms</param>
		/// <param name="encoder">an encoder that generates encoded text</param>
		/// <returns>
		/// created fragments or null when no fragments created.
		/// size of the array can be less than maxNumFragments
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public string[] GetBestFragments(FieldQuery fieldQuery, IndexReader reader, int docId
			, string storedField, ICollection<string> matchedFields, int fragCharSize, int maxNumFragments
			, FragListBuilder fragListBuilder, FragmentsBuilder fragmentsBuilder, string[] preTags
			, string[] postTags, Encoder encoder)
		{
			FieldFragList fieldFragList = GetFieldFragList(fragListBuilder, fieldQuery, reader
				, docId, matchedFields, fragCharSize);
			return fragmentsBuilder.CreateFragments(reader, docId, storedField, fieldFragList
				, maxNumFragments, preTags, postTags, encoder);
		}

		/// <summary>Build a FieldFragList for one field.</summary>
		/// <remarks>Build a FieldFragList for one field.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private FieldFragList GetFieldFragList(FragListBuilder fragListBuilder, FieldQuery
			 fieldQuery, IndexReader reader, int docId, string matchedField, int fragCharSize
			)
		{
			FieldTermStack fieldTermStack = new FieldTermStack(reader, docId, matchedField, fieldQuery
				);
			FieldPhraseList fieldPhraseList = new FieldPhraseList(fieldTermStack, fieldQuery, 
				phraseLimit);
			return fragListBuilder.CreateFieldFragList(fieldPhraseList, fragCharSize);
		}

		/// <summary>Build a FieldFragList for more than one field.</summary>
		/// <remarks>Build a FieldFragList for more than one field.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private FieldFragList GetFieldFragList(FragListBuilder fragListBuilder, FieldQuery
			 fieldQuery, IndexReader reader, int docId, ICollection<string> matchedFields, int
			 fragCharSize)
		{
			Iterator<string> matchedFieldsItr = matchedFields.Iterator();
			if (!matchedFieldsItr.HasNext())
			{
				throw new ArgumentException("matchedFields must contain at least on field name.");
			}
			FieldPhraseList[] toMerge = new FieldPhraseList[matchedFields.Count];
			int i = 0;
			while (matchedFieldsItr.HasNext())
			{
				FieldTermStack stack = new FieldTermStack(reader, docId, matchedFieldsItr.Next(), 
					fieldQuery);
				toMerge[i++] = new FieldPhraseList(stack, fieldQuery, phraseLimit);
			}
			return fragListBuilder.CreateFieldFragList(new FieldPhraseList(toMerge), fragCharSize
				);
		}

		/// <summary>return whether phraseHighlight or not.</summary>
		/// <remarks>return whether phraseHighlight or not.</remarks>
		/// <returns>whether phraseHighlight or not</returns>
		public virtual bool IsPhraseHighlight()
		{
			return phraseHighlight;
		}

		/// <summary>return whether fieldMatch or not.</summary>
		/// <remarks>return whether fieldMatch or not.</remarks>
		/// <returns>whether fieldMatch or not</returns>
		public virtual bool IsFieldMatch()
		{
			return fieldMatch;
		}

		/// <returns>the maximum number of phrases to analyze when searching for the highest-scoring phrase.
		/// 	</returns>
		public virtual int GetPhraseLimit()
		{
			return phraseLimit;
		}

		/// <summary>set the maximum number of phrases to analyze when searching for the highest-scoring phrase.
		/// 	</summary>
		/// <remarks>
		/// set the maximum number of phrases to analyze when searching for the highest-scoring phrase.
		/// The default is unlimited (Integer.MAX_VALUE).
		/// </remarks>
		public virtual void SetPhraseLimit(int phraseLimit)
		{
			this.phraseLimit = phraseLimit;
		}
	}
}
