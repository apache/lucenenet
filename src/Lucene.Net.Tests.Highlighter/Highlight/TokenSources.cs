/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search.Highlight;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// Hides implementation issues associated with obtaining a TokenStream for use
	/// with the higlighter - can obtain from TermFreqVectors with offsets and
	/// (optionally) positions or from Analyzer class reparsing the stored content.
	/// </summary>
	/// <remarks>
	/// Hides implementation issues associated with obtaining a TokenStream for use
	/// with the higlighter - can obtain from TermFreqVectors with offsets and
	/// (optionally) positions or from Analyzer class reparsing the stored content.
	/// </remarks>
	public class TokenSources
	{
		/// <summary>
		/// A convenience method that tries to first get a TermPositionVector for the
		/// specified docId, then, falls back to using the passed in
		/// <see cref="Org.Apache.Lucene.Document.Document">Org.Apache.Lucene.Document.Document
		/// 	</see>
		/// to retrieve the TokenStream.
		/// This is useful when you already have the document, but would prefer to use
		/// the vector first.
		/// </summary>
		/// <param name="reader">
		/// The
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// to use to try
		/// and get the vector from
		/// </param>
		/// <param name="docId">The docId to retrieve.</param>
		/// <param name="field">The field to retrieve on the document</param>
		/// <param name="doc">The document to fall back on</param>
		/// <param name="analyzer">
		/// The analyzer to use for creating the TokenStream if the
		/// vector doesn't exist
		/// </param>
		/// <returns>
		/// The
		/// <see cref="Org.Apache.Lucene.Analysis.TokenStream">Org.Apache.Lucene.Analysis.TokenStream
		/// 	</see>
		/// for the
		/// <see cref="Org.Apache.Lucene.Index.IndexableField">Org.Apache.Lucene.Index.IndexableField
		/// 	</see>
		/// on the
		/// <see cref="Org.Apache.Lucene.Document.Document">Org.Apache.Lucene.Document.Document
		/// 	</see>
		/// </returns>
		/// <exception cref="System.IO.IOException">if there was an error loading</exception>
		public static TokenStream GetAnyTokenStream(IndexReader reader, int docId, string
			 field, Org.Apache.Lucene.Document.Document doc, Analyzer analyzer)
		{
			TokenStream ts = null;
			Fields vectors = reader.GetTermVectors(docId);
			if (vectors != null)
			{
				Terms vector = vectors.Terms(field);
				if (vector != null)
				{
					ts = GetTokenStream(vector);
				}
			}
			// No token info stored so fall back to analyzing raw content
			if (ts == null)
			{
				ts = GetTokenStream(doc, field, analyzer);
			}
			return ts;
		}

		/// <summary>
		/// A convenience method that tries a number of approaches to getting a token
		/// stream.
		/// </summary>
		/// <remarks>
		/// A convenience method that tries a number of approaches to getting a token
		/// stream. The cost of finding there are no termVectors in the index is
		/// minimal (1000 invocations still registers 0 ms). So this "lazy" (flexible?)
		/// approach to coding is probably acceptable
		/// </remarks>
		/// <returns>null if field not stored correctly</returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public static TokenStream GetAnyTokenStream(IndexReader reader, int docId, string
			 field, Analyzer analyzer)
		{
			TokenStream ts = null;
			Fields vectors = reader.GetTermVectors(docId);
			if (vectors != null)
			{
				Terms vector = vectors.Terms(field);
				if (vector != null)
				{
					ts = GetTokenStream(vector);
				}
			}
			// No token info stored so fall back to analyzing raw content
			if (ts == null)
			{
				ts = GetTokenStream(reader, docId, field, analyzer);
			}
			return ts;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static TokenStream GetTokenStream(Terms vector)
		{
			// assumes the worst and makes no assumptions about token position
			// sequences.
			return GetTokenStream(vector, false);
		}

		/// <summary>Low level api.</summary>
		/// <remarks>
		/// Low level api. Returns a token stream generated from a
		/// <see cref="Org.Apache.Lucene.Index.Terms">Org.Apache.Lucene.Index.Terms</see>
		/// . This
		/// can be used to feed the highlighter with a pre-parsed token
		/// stream.  The
		/// <see cref="Org.Apache.Lucene.Index.Terms">Org.Apache.Lucene.Index.Terms</see>
		/// must have offsets available.
		/// In my tests the speeds to recreate 1000 token streams using this method
		/// are: - with TermVector offset only data stored - 420 milliseconds - with
		/// TermVector offset AND position data stored - 271 milliseconds (nb timings
		/// for TermVector with position data are based on a tokenizer with contiguous
		/// positions - no overlaps or gaps) The cost of not using TermPositionVector
		/// to store pre-parsed content and using an analyzer to re-parse the original
		/// content: - reanalyzing the original content - 980 milliseconds
		/// The re-analyze timings will typically vary depending on - 1) The complexity
		/// of the analyzer code (timings above were using a
		/// stemmer/lowercaser/stopword combo) 2) The number of other fields (Lucene
		/// reads ALL fields off the disk when accessing just one document field - can
		/// cost dear!) 3) Use of compression on field storage - could be faster due to
		/// compression (less disk IO) or slower (more CPU burn) depending on the
		/// content.
		/// </remarks>
		/// <param name="tokenPositionsGuaranteedContiguous">
		/// true if the token position
		/// numbers have no overlaps or gaps. If looking to eek out the last
		/// drops of performance, set to true. If in doubt, set to false.
		/// </param>
		/// <exception cref="System.ArgumentException">if no offsets are available</exception>
		/// <exception cref="System.IO.IOException"></exception>
		public static TokenStream GetTokenStream(Terms tpv, bool tokenPositionsGuaranteedContiguous
			)
		{
			if (!tpv.HasOffsets())
			{
				throw new ArgumentException("Cannot create TokenStream from Terms without offsets"
					);
			}
			if (!tokenPositionsGuaranteedContiguous && tpv.HasPositions())
			{
				return new TokenStreamFromTermPositionVector(tpv);
			}
			// an object used to iterate across an array of tokens
			bool hasPayloads = tpv.HasPayloads();
			// code to reconstruct the original sequence of Tokens
			TermsEnum termsEnum = tpv.Iterator(null);
			int totalTokens = 0;
			while (termsEnum.Next() != null)
			{
				totalTokens += (int)termsEnum.TotalTermFreq();
			}
			Token[] tokensInOriginalOrder = new Token[totalTokens];
			AList<Token> unsortedTokens = null;
			termsEnum = tpv.Iterator(null);
			BytesRef text;
			DocsAndPositionsEnum dpEnum = null;
			while ((text = termsEnum.Next()) != null)
			{
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				if (dpEnum == null)
				{
					throw new ArgumentException("Required TermVector Offset information was not found"
						);
				}
				string term = text.Utf8ToString();
				dpEnum.NextDoc();
				int freq = dpEnum.Freq();
				for (int posUpto = 0; posUpto < freq; posUpto++)
				{
					int pos = dpEnum.NextPosition();
					if (dpEnum.StartOffset() < 0)
					{
						throw new ArgumentException("Required TermVector Offset information was not found"
							);
					}
					Token token = new Token(term, dpEnum.StartOffset(), dpEnum.EndOffset());
					if (hasPayloads)
					{
						// Must make a deep copy of the returned payload,
						// since D&PEnum API is allowed to re-use on every
						// call:
						token.SetPayload(BytesRef.DeepCopyOf(dpEnum.GetPayload()));
					}
					if (tokenPositionsGuaranteedContiguous && pos != -1)
					{
						// We have positions stored and a guarantee that the token position
						// information is contiguous
						// This may be fast BUT wont work if Tokenizers used which create >1
						// token in same position or
						// creates jumps in position numbers - this code would fail under those
						// circumstances
						// tokens stored with positions - can use this to index straight into
						// sorted array
						tokensInOriginalOrder[pos] = token;
					}
					else
					{
						// tokens NOT stored with positions or not guaranteed contiguous - must
						// add to list and sort later
						if (unsortedTokens == null)
						{
							unsortedTokens = new AList<Token>();
						}
						unsortedTokens.AddItem(token);
					}
				}
			}
			// If the field has been stored without position data we must perform a sort
			if (unsortedTokens != null)
			{
				tokensInOriginalOrder = Sharpen.Collections.ToArray(unsortedTokens, new Token[unsortedTokens
					.Count]);
				ArrayUtil.TimSort(tokensInOriginalOrder, new _IComparer_272());
			}
			return new _T1969884030(this, tokensInOriginalOrder);
		}

		internal sealed class _T1969884030 : TokenStream
		{
			internal Token tokens;

			internal int currentToken = 0;

			internal CharTermAttribute termAtt;

			internal OffsetAttribute offsetAtt;

			internal PositionIncrementAttribute posincAtt;

			internal PayloadAttribute payloadAtt;

			internal _T1969884030(TokenSources _enclosing, Token[] tokens)
			{
				this._enclosing = _enclosing;
				this.tokens = tokens;
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.offsetAtt = this.AddAttribute<OffsetAttribute>();
				this.posincAtt = this.AddAttribute<PositionIncrementAttribute>();
				this.payloadAtt = this.AddAttribute<PayloadAttribute>();
			}

			public override bool IncrementToken()
			{
				if (this.currentToken >= this.tokens.Length)
				{
					return false;
				}
				Token token = this.tokens[this.currentToken++];
				this.ClearAttributes();
				this.termAtt.SetEmpty().Append(token);
				this.offsetAtt.SetOffset(token.StartOffset(), token.EndOffset());
				BytesRef payload = token.GetPayload();
				if (payload != null)
				{
					this.payloadAtt.SetPayload(payload);
				}
				this.posincAtt.SetPositionIncrement(this.currentToken <= 1 || this.tokens[this.currentToken
					 - 1].StartOffset() > this.tokens[this.currentToken - 2].StartOffset() ? 1 : 0);
				return true;
			}

			private readonly TokenSources _enclosing;
		}

		private sealed class _IComparer_272 : IComparer<Token>
		{
			public _IComparer_272()
			{
			}

			public int Compare(Token t1, Token t2)
			{
				if (t1.StartOffset() == t2.StartOffset())
				{
					return t1.EndOffset() - t2.EndOffset();
				}
				else
				{
					return t1.StartOffset() - t2.StartOffset();
				}
			}
		}

		/// <summary>
		/// Returns a
		/// <see cref="Org.Apache.Lucene.Analysis.TokenStream">Org.Apache.Lucene.Analysis.TokenStream
		/// 	</see>
		/// with positions and offsets constructed from
		/// field termvectors.  If the field has no termvectors, or positions or offsets
		/// are not included in the termvector, return null.
		/// </summary>
		/// <param name="reader">
		/// the
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// to retrieve term vectors from
		/// </param>
		/// <param name="docId">the document to retrieve termvectors for</param>
		/// <param name="field">the field to retrieve termvectors for</param>
		/// <returns>
		/// a
		/// <see cref="Org.Apache.Lucene.Analysis.TokenStream">Org.Apache.Lucene.Analysis.TokenStream
		/// 	</see>
		/// , or null if positions and offsets are not available
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public static TokenStream GetTokenStreamWithOffsets(IndexReader reader, int docId
			, string field)
		{
			Fields vectors = reader.GetTermVectors(docId);
			if (vectors == null)
			{
				return null;
			}
			Terms vector = vectors.Terms(field);
			if (vector == null)
			{
				return null;
			}
			if (!vector.HasPositions() || !vector.HasOffsets())
			{
				return null;
			}
			return GetTokenStream(vector);
		}

		// convenience method
		/// <exception cref="System.IO.IOException"></exception>
		public static TokenStream GetTokenStream(IndexReader reader, int docId, string field
			, Analyzer analyzer)
		{
			Org.Apache.Lucene.Document.Document doc = reader.Document(docId);
			return GetTokenStream(doc, field, analyzer);
		}

		public static TokenStream GetTokenStream(Org.Apache.Lucene.Document.Document doc, 
			string field, Analyzer analyzer)
		{
			string contents = doc.Get(field);
			if (contents == null)
			{
				throw new ArgumentException("Field " + field + " in document is not stored and cannot be analyzed"
					);
			}
			return GetTokenStream(field, contents, analyzer);
		}

		// convenience method
		public static TokenStream GetTokenStream(string field, string contents, Analyzer 
			analyzer)
		{
			try
			{
				return analyzer.TokenStream(field, contents);
			}
			catch (IOException ex)
			{
				throw new RuntimeException(ex);
			}
		}
	}
}
