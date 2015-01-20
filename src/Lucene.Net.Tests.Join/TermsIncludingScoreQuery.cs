/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Globalization;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Join;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	internal class TermsIncludingScoreQuery : Query
	{
		internal readonly string field;

		internal readonly bool multipleValuesPerDocument;

		internal readonly BytesRefHash terms;

		internal readonly float[] scores;

		internal readonly int[] ords;

		internal readonly Query originalQuery;

		internal readonly Query unwrittenOriginalQuery;

		internal TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash
			 terms, float[] scores, Query originalQuery)
		{
			this.field = field;
			this.multipleValuesPerDocument = multipleValuesPerDocument;
			this.terms = terms;
			this.scores = scores;
			this.originalQuery = originalQuery;
			this.ords = terms.Sort(BytesRef.GetUTF8SortedAsUnicodeComparator());
			this.unwrittenOriginalQuery = originalQuery;
		}

		private TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash
			 terms, float[] scores, int[] ords, Query originalQuery, Query unwrittenOriginalQuery
			)
		{
			this.field = field;
			this.multipleValuesPerDocument = multipleValuesPerDocument;
			this.terms = terms;
			this.scores = scores;
			this.originalQuery = originalQuery;
			this.ords = ords;
			this.unwrittenOriginalQuery = unwrittenOriginalQuery;
		}

		public override string ToString(string @string)
		{
			return string.Format(CultureInfo.ROOT, "TermsIncludingScoreQuery{field=%s;originalQuery=%s}"
				, field, unwrittenOriginalQuery);
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			originalQuery.ExtractTerms(terms);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query originalQueryRewrite = originalQuery.Rewrite(reader);
			if (originalQueryRewrite != originalQuery)
			{
				Query rewritten = new Org.Apache.Lucene.Search.Join.TermsIncludingScoreQuery(field
					, multipleValuesPerDocument, terms, scores, ords, originalQueryRewrite, originalQuery
					);
				rewritten.SetBoost(GetBoost());
				return rewritten;
			}
			else
			{
				return this;
			}
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Search.Join.TermsIncludingScoreQuery other = (Org.Apache.Lucene.Search.Join.TermsIncludingScoreQuery
				)obj;
			if (!field.Equals(other.field))
			{
				return false;
			}
			if (!unwrittenOriginalQuery.Equals(other.unwrittenOriginalQuery))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result += prime * field.GetHashCode();
			result += prime * unwrittenOriginalQuery.GetHashCode();
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			Weight originalWeight = originalQuery.CreateWeight(searcher);
			return new _Weight_129(this, originalWeight);
		}

		private sealed class _Weight_129 : Weight
		{
			public _Weight_129(TermsIncludingScoreQuery _enclosing, Weight originalWeight)
			{
				this._enclosing = _enclosing;
				this.originalWeight = originalWeight;
			}

			private TermsEnum segmentTermsEnum;

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				TermsIncludingScoreQuery.SVInnerScorer scorer = (TermsIncludingScoreQuery.SVInnerScorer
					)this.BulkScorer(context, false, null);
				if (scorer != null)
				{
					return scorer.Explain(doc);
				}
				return new ComplexExplanation(false, 0.0f, "Not a match");
			}

			public override bool ScoresDocsOutOfOrder()
			{
				// We have optimized impls below if we are allowed
				// to score out-of-order:
				return true;
			}

			public override Query GetQuery()
			{
				return this._enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				return originalWeight.GetValueForNormalization() * this._enclosing.GetBoost() * this
					._enclosing.GetBoost();
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				originalWeight.Normalize(norm, topLevelBoost * this._enclosing.GetBoost());
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
			{
				Terms terms = ((AtomicReader)context.Reader()).Terms(this._enclosing.field);
				if (terms == null)
				{
					return null;
				}
				// what is the runtime...seems ok?
				long cost = ((AtomicReader)context.Reader()).MaxDoc() * terms.Size();
				this.segmentTermsEnum = terms.Iterator(this.segmentTermsEnum);
				if (this._enclosing.multipleValuesPerDocument)
				{
					return new TermsIncludingScoreQuery.MVInOrderScorer(this, this, acceptDocs, this.
						segmentTermsEnum, ((AtomicReader)context.Reader()).MaxDoc(), cost);
				}
				else
				{
					return new TermsIncludingScoreQuery.SVInOrderScorer(this, this, acceptDocs, this.
						segmentTermsEnum, ((AtomicReader)context.Reader()).MaxDoc(), cost);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder
				, Bits acceptDocs)
			{
				if (scoreDocsInOrder)
				{
					return base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
				}
				else
				{
					Terms terms = ((AtomicReader)context.Reader()).Terms(this._enclosing.field);
					if (terms == null)
					{
						return null;
					}
					// what is the runtime...seems ok?
					long cost = ((AtomicReader)context.Reader()).MaxDoc() * terms.Size();
					this.segmentTermsEnum = terms.Iterator(this.segmentTermsEnum);
					// Optimized impls that take advantage of docs
					// being allowed to be out of order:
					if (this._enclosing.multipleValuesPerDocument)
					{
						return new TermsIncludingScoreQuery.MVInnerScorer(this, this, acceptDocs, this.segmentTermsEnum
							, ((AtomicReader)context.Reader()).MaxDoc(), cost);
					}
					else
					{
						return new TermsIncludingScoreQuery.SVInnerScorer(this, this, acceptDocs, this.segmentTermsEnum
							, cost);
					}
				}
			}

			private readonly TermsIncludingScoreQuery _enclosing;

			private readonly Weight originalWeight;
		}

		internal class SVInnerScorer : BulkScorer
		{
			internal readonly BytesRef spare = new BytesRef();

			internal readonly Bits acceptDocs;

			internal readonly TermsEnum termsEnum;

			internal readonly long cost;

			internal int upto;

			internal DocsEnum docsEnum;

			internal DocsEnum reuse;

			internal int scoreUpto;

			internal int doc;

			internal SVInnerScorer(TermsIncludingScoreQuery _enclosing, Weight weight, Bits acceptDocs
				, TermsEnum termsEnum, long cost)
			{
				this._enclosing = _enclosing;
				// This impl assumes that the 'join' values are used uniquely per doc per field. Used for one to many relations.
				this.acceptDocs = acceptDocs;
				this.termsEnum = termsEnum;
				this.cost = cost;
				this.doc = -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool Score(Collector collector, int max)
			{
				FakeScorer fakeScorer = new FakeScorer();
				collector.SetScorer(fakeScorer);
				if (this.doc == -1)
				{
					this.doc = this.NextDocOutOfOrder();
				}
				while (this.doc < max)
				{
					fakeScorer.doc = this.doc;
					fakeScorer.score = this._enclosing.scores[this._enclosing.ords[this.scoreUpto]];
					collector.Collect(this.doc);
					this.doc = this.NextDocOutOfOrder();
				}
				return this.doc != DocsEnum.NO_MORE_DOCS;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual int NextDocOutOfOrder()
			{
				while (true)
				{
					if (this.docsEnum != null)
					{
						int docId = this.DocsEnumNextDoc();
						if (docId == DocIdSetIterator.NO_MORE_DOCS)
						{
							this.docsEnum = null;
						}
						else
						{
							return this.doc = docId;
						}
					}
					if (this.upto == this._enclosing.terms.Size())
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
					this.scoreUpto = this.upto;
					if (this.termsEnum.SeekExact(this._enclosing.terms.Get(this._enclosing.ords[this.
						upto++], this.spare)))
					{
						this.docsEnum = this.reuse = this.termsEnum.Docs(this.acceptDocs, this.reuse, DocsEnum
							.FLAG_NONE);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual int DocsEnumNextDoc()
			{
				return this.docsEnum.NextDoc();
			}

			/// <exception cref="System.IO.IOException"></exception>
			private Explanation Explain(int target)
			{
				int docId;
				do
				{
					docId = this.NextDocOutOfOrder();
					if (docId < target)
					{
						int tempDocId = this.docsEnum.Advance(target);
						if (tempDocId == target)
						{
							docId = tempDocId;
							break;
						}
					}
					else
					{
						if (docId == target)
						{
							break;
						}
					}
					this.docsEnum = null;
				}
				while (docId != DocIdSetIterator.NO_MORE_DOCS);
				// goto the next ord.
				return new ComplexExplanation(true, this._enclosing.scores[this._enclosing.ords[this
					.scoreUpto]], "Score based on join value " + this.termsEnum.Term().Utf8ToString(
					));
			}

			private readonly TermsIncludingScoreQuery _enclosing;
		}

		internal class MVInnerScorer : TermsIncludingScoreQuery.SVInnerScorer
		{
			internal readonly FixedBitSet alreadyEmittedDocs;

			internal MVInnerScorer(TermsIncludingScoreQuery _enclosing, Weight weight, Bits acceptDocs
				, TermsEnum termsEnum, int maxDoc, long cost) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				// This impl that tracks whether a docid has already been emitted. This check makes sure that docs aren't emitted
				// twice for different join values. This means that the first encountered join value determines the score of a document
				// even if other join values yield a higher score.
				this.alreadyEmittedDocs = new FixedBitSet(maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override int DocsEnumNextDoc()
			{
				while (true)
				{
					int docId = this.docsEnum.NextDoc();
					if (docId == DocIdSetIterator.NO_MORE_DOCS)
					{
						return docId;
					}
					if (!this.alreadyEmittedDocs.GetAndSet(docId))
					{
						return docId;
					}
				}
			}

			private readonly TermsIncludingScoreQuery _enclosing;
			//if it wasn't previously set, return it
		}

		internal class SVInOrderScorer : Scorer
		{
			internal readonly DocIdSetIterator matchingDocsIterator;

			internal readonly float[] scores;

			internal readonly long cost;

			internal int currentDoc = -1;

			/// <exception cref="System.IO.IOException"></exception>
			internal SVInOrderScorer(TermsIncludingScoreQuery _enclosing, Weight weight, Bits
				 acceptDocs, TermsEnum termsEnum, int maxDoc, long cost) : base(weight)
			{
				this._enclosing = _enclosing;
				FixedBitSet matchingDocs = new FixedBitSet(maxDoc);
				this.scores = new float[maxDoc];
				this.FillDocsAndScores(matchingDocs, acceptDocs, termsEnum);
				this.matchingDocsIterator = matchingDocs.Iterator();
				this.cost = cost;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void FillDocsAndScores(FixedBitSet matchingDocs, Bits 
				acceptDocs, TermsEnum termsEnum)
			{
				BytesRef spare = new BytesRef();
				DocsEnum docsEnum = null;
				for (int i = 0; i < this._enclosing.terms.Size(); i++)
				{
					if (termsEnum.SeekExact(this._enclosing.terms.Get(this._enclosing.ords[i], spare)
						))
					{
						docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
						float score = this._enclosing.scores[this._enclosing.ords[i]];
						for (int doc = docsEnum.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = docsEnum
							.NextDoc())
						{
							matchingDocs.Set(doc);
							// In the case the same doc is also related to a another doc, a score might be overwritten. I think this
							// can only happen in a many-to-many relation
							this.scores[doc] = score;
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				return this.scores[this.currentDoc];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return 1;
			}

			public override int DocID()
			{
				return this.currentDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				return this.currentDoc = this.matchingDocsIterator.NextDoc();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return this.currentDoc = this.matchingDocsIterator.Advance(target);
			}

			public override long Cost()
			{
				return this.cost;
			}

			private readonly TermsIncludingScoreQuery _enclosing;
		}

		internal class MVInOrderScorer : TermsIncludingScoreQuery.SVInOrderScorer
		{
			/// <exception cref="System.IO.IOException"></exception>
			internal MVInOrderScorer(TermsIncludingScoreQuery _enclosing, Weight weight, Bits
				 acceptDocs, TermsEnum termsEnum, int maxDoc, long cost) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			// This scorer deals with the fact that a document can have more than one score from multiple related documents.
			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void FillDocsAndScores(FixedBitSet matchingDocs, Bits
				 acceptDocs, TermsEnum termsEnum)
			{
				BytesRef spare = new BytesRef();
				DocsEnum docsEnum = null;
				for (int i = 0; i < this._enclosing.terms.Size(); i++)
				{
					if (termsEnum.SeekExact(this._enclosing.terms.Get(this._enclosing.ords[i], spare)
						))
					{
						docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
						float score = this._enclosing.scores[this._enclosing.ords[i]];
						for (int doc = docsEnum.NextDoc(); doc != DocIdSetIterator.NO_MORE_DOCS; doc = docsEnum
							.NextDoc())
						{
							// I prefer this:
							// But this behaves the same as MVInnerScorer and only then the tests will pass:
							if (!matchingDocs.Get(doc))
							{
								this.scores[doc] = score;
								matchingDocs.Set(doc);
							}
						}
					}
				}
			}

			private readonly TermsIncludingScoreQuery _enclosing;
		}
	}
}
