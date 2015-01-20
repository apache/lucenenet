/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Similarities;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Function that returns
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.Tf(float)">Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity.Tf(float)
	/// 	</see>
	/// for every document.
	/// <p>
	/// Note that the configured Similarity for the field must be
	/// a subclass of
	/// <see cref="Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity">Org.Apache.Lucene.Search.Similarities.TFIDFSimilarity
	/// 	</see>
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class TFValueSource : TermFreqValueSource
	{
		public TFValueSource(string field, string val, string indexedField, BytesRef indexedBytes
			) : base(field, val, indexedField, indexedBytes)
		{
		}

		public override string Name()
		{
			return "tf";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			Fields fields = ((AtomicReader)readerContext.Reader()).Fields();
			Terms terms = fields.Terms(indexedField);
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.GetSimilarity(), indexedField
				);
			if (similarity == null)
			{
				throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)"
					);
			}
			return new _FloatDocValues_58(this, terms, similarity, this);
		}

		private sealed class _FloatDocValues_58 : FloatDocValues
		{
			public _FloatDocValues_58(TFValueSource _enclosing, Terms terms, TFIDFSimilarity 
				similarity, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.terms = terms;
				this.similarity = similarity;
				{
					this.Reset();
				}
				this.lastDocRequested = -1;
			}

			internal DocsEnum docs;

			internal int atDoc;

			internal int lastDocRequested;

			/// <exception cref="System.IO.IOException"></exception>
			public void Reset()
			{
				// no one should call us for deleted docs?
				if (terms != null)
				{
					TermsEnum termsEnum = terms.Iterator(null);
					if (termsEnum.SeekExact(this._enclosing.indexedBytes))
					{
						this.docs = termsEnum.Docs(null, null);
					}
					else
					{
						this.docs = null;
					}
				}
				else
				{
					this.docs = null;
				}
				if (this.docs == null)
				{
					this.docs = new _DocsEnum_80();
				}
				this.atDoc = -1;
			}

			private sealed class _DocsEnum_80 : DocsEnum
			{
				public _DocsEnum_80()
				{
				}

				public override int Freq()
				{
					return 0;
				}

				public override int DocID()
				{
					return DocIdSetIterator.NO_MORE_DOCS;
				}

				public override int NextDoc()
				{
					return DocIdSetIterator.NO_MORE_DOCS;
				}

				public override int Advance(int target)
				{
					return DocIdSetIterator.NO_MORE_DOCS;
				}

				public override long Cost()
				{
					return 0;
				}
			}

			public override float FloatVal(int doc)
			{
				try
				{
					if (doc < this.lastDocRequested)
					{
						// out-of-order access.... reset
						this.Reset();
					}
					this.lastDocRequested = doc;
					if (this.atDoc < doc)
					{
						this.atDoc = this.docs.Advance(doc);
					}
					if (this.atDoc > doc)
					{
						// term doesn't match this document... either because we hit the
						// end, or because the next doc is after this doc.
						return similarity.Tf(0);
					}
					// a match!
					return similarity.Tf(this.docs.Freq());
				}
				catch (IOException e)
				{
					throw new RuntimeException("caught exception in function " + this._enclosing.Description
						() + " : doc=" + doc, e);
				}
			}

			private readonly TFValueSource _enclosing;

			private readonly Terms terms;

			private readonly TFIDFSimilarity similarity;
		}
	}
}
