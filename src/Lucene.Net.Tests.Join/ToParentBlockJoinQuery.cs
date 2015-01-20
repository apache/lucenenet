/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Join;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	/// <summary>
	/// This query requires that you index
	/// children and parent docs as a single block, using the
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter.AddDocuments(Sharpen.Iterable{T})"
	/// 	>IndexWriter.addDocuments()</see>
	/// or
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter.UpdateDocuments(Org.Apache.Lucene.Index.Term, Sharpen.Iterable{T})
	/// 	">IndexWriter.updateDocuments()</see>
	/// API.  In each block, the
	/// child documents must appear first, ending with the parent
	/// document.  At search time you provide a Filter
	/// identifying the parents, however this Filter must provide
	/// an
	/// <see cref="Org.Apache.Lucene.Util.FixedBitSet">Org.Apache.Lucene.Util.FixedBitSet
	/// 	</see>
	/// per sub-reader.
	/// <p>Once the block index is built, use this query to wrap
	/// any sub-query matching only child docs and join matches in that
	/// child document space up to the parent document space.
	/// You can then use this Query as a clause with
	/// other queries in the parent document space.</p>
	/// <p>See
	/// <see cref="ToChildBlockJoinQuery">ToChildBlockJoinQuery</see>
	/// if you need to join
	/// in the reverse order.
	/// <p>The child documents must be orthogonal to the parent
	/// documents: the wrapped child query must never
	/// return a parent document.</p>
	/// If you'd like to retrieve
	/// <see cref="Org.Apache.Lucene.Search.Grouping.TopGroups{GROUP_VALUE_TYPE}">Org.Apache.Lucene.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// for the
	/// resulting query, use the
	/// <see cref="ToParentBlockJoinCollector">ToParentBlockJoinCollector</see>
	/// .
	/// Note that this is not necessary, ie, if you simply want
	/// to collect the parent documents and don't need to see
	/// which child documents matched under that parent, then
	/// you can use any collector.
	/// <p><b>NOTE</b>: If the overall query contains parent-only
	/// matches, for example you OR a parent-only query with a
	/// joined child-only query, then the resulting collected documents
	/// will be correct, however the
	/// <see cref="Org.Apache.Lucene.Search.Grouping.TopGroups{GROUP_VALUE_TYPE}">Org.Apache.Lucene.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// you get
	/// from
	/// <see cref="ToParentBlockJoinCollector">ToParentBlockJoinCollector</see>
	/// will not contain every
	/// child for parents that had matched.
	/// <p>See
	/// <see cref="Org.Apache.Lucene.Search.Join">Org.Apache.Lucene.Search.Join</see>
	/// for an
	/// overview. </p>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ToParentBlockJoinQuery : Query
	{
		private readonly Filter parentsFilter;

		private readonly Query childQuery;

		private readonly Query origChildQuery;

		private readonly ScoreMode scoreMode;

		/// <summary>Create a ToParentBlockJoinQuery.</summary>
		/// <remarks>Create a ToParentBlockJoinQuery.</remarks>
		/// <param name="childQuery">Query matching child documents.</param>
		/// <param name="parentsFilter">
		/// Filter (must produce FixedBitSet
		/// per-segment, like
		/// <see cref="FixedBitSetCachingWrapperFilter">FixedBitSetCachingWrapperFilter</see>
		/// )
		/// identifying the parent documents.
		/// </param>
		/// <param name="scoreMode">
		/// How to aggregate multiple child scores
		/// into a single parent score.
		/// </param>
		public ToParentBlockJoinQuery(Query childQuery, Filter parentsFilter, ScoreMode scoreMode
			) : base()
		{
			// If we are rewritten, this is the original childQuery we
			// were passed; we use this for .equals() and
			// .hashCode().  This makes rewritten query equal the
			// original, so that user does not have to .rewrite() their
			// query before searching:
			this.origChildQuery = childQuery;
			this.childQuery = childQuery;
			this.parentsFilter = parentsFilter;
			this.scoreMode = scoreMode;
		}

		private ToParentBlockJoinQuery(Query origChildQuery, Query childQuery, Filter parentsFilter
			, ScoreMode scoreMode) : base()
		{
			this.origChildQuery = origChildQuery;
			this.childQuery = childQuery;
			this.parentsFilter = parentsFilter;
			this.scoreMode = scoreMode;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new ToParentBlockJoinQuery.BlockJoinWeight(this, childQuery.CreateWeight(searcher
				), parentsFilter, scoreMode);
		}

		private class BlockJoinWeight : Weight
		{
			private readonly Query joinQuery;

			private readonly Weight childWeight;

			private readonly Filter parentsFilter;

			private readonly ScoreMode scoreMode;

			public BlockJoinWeight(Query joinQuery, Weight childWeight, Filter parentsFilter, 
				ScoreMode scoreMode) : base()
			{
				this.joinQuery = joinQuery;
				this.childWeight = childWeight;
				this.parentsFilter = parentsFilter;
				this.scoreMode = scoreMode;
			}

			public override Query GetQuery()
			{
				return joinQuery;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				return childWeight.GetValueForNormalization() * joinQuery.GetBoost() * joinQuery.
					GetBoost();
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				childWeight.Normalize(norm, topLevelBoost * joinQuery.GetBoost());
			}

			// NOTE: acceptDocs applies (and is checked) only in the
			// parent document space
			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Search.Scorer Scorer(AtomicReaderContext readerContext
				, Bits acceptDocs)
			{
				Org.Apache.Lucene.Search.Scorer childScorer = childWeight.Scorer(readerContext, (
					(AtomicReader)readerContext.Reader()).GetLiveDocs());
				if (childScorer == null)
				{
					// No matches
					return null;
				}
				int firstChildDoc = childScorer.NextDoc();
				if (firstChildDoc == DocIdSetIterator.NO_MORE_DOCS)
				{
					// No matches
					return null;
				}
				// NOTE: we cannot pass acceptDocs here because this
				// will (most likely, justifiably) cause the filter to
				// not return a FixedBitSet but rather a
				// BitsFilteredDocIdSet.  Instead, we filter by
				// acceptDocs when we score:
				DocIdSet parents = parentsFilter.GetDocIdSet(readerContext, null);
				if (parents == null)
				{
					// No matches
					return null;
				}
				if (!(parents is FixedBitSet))
				{
					throw new InvalidOperationException("parentFilter must return FixedBitSet; got " 
						+ parents);
				}
				return new ToParentBlockJoinQuery.BlockJoinScorer(this, childScorer, (FixedBitSet
					)parents, firstChildDoc, scoreMode, acceptDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				ToParentBlockJoinQuery.BlockJoinScorer scorer = (ToParentBlockJoinQuery.BlockJoinScorer
					)Scorer(context, ((AtomicReader)context.Reader()).GetLiveDocs());
				if (scorer != null && scorer.Advance(doc) == doc)
				{
					return scorer.Explain(context.docBase);
				}
				return new ComplexExplanation(false, 0.0f, "Not a match");
			}

			public override bool ScoresDocsOutOfOrder()
			{
				return false;
			}
		}

		internal class BlockJoinScorer : Scorer
		{
			private readonly Scorer childScorer;

			private readonly FixedBitSet parentBits;

			private readonly ScoreMode scoreMode;

			private readonly Bits acceptDocs;

			private int parentDoc = -1;

			private int prevParentDoc;

			private float parentScore;

			private int parentFreq;

			private int nextChildDoc;

			private int[] pendingChildDocs;

			private float[] pendingChildScores;

			private int childDocUpto;

			public BlockJoinScorer(Weight weight, Scorer childScorer, FixedBitSet parentBits, 
				int firstChildDoc, ScoreMode scoreMode, Bits acceptDocs) : base(weight)
			{
				//System.out.println("Q.init firstChildDoc=" + firstChildDoc);
				this.parentBits = parentBits;
				this.childScorer = childScorer;
				this.scoreMode = scoreMode;
				this.acceptDocs = acceptDocs;
				nextChildDoc = firstChildDoc;
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				return Sharpen.Collections.Singleton(new Scorer.ChildScorer(childScorer, "BLOCK_JOIN"
					));
			}

			internal virtual int GetChildCount()
			{
				return childDocUpto;
			}

			internal virtual int GetParentDoc()
			{
				return parentDoc;
			}

			internal virtual int[] SwapChildDocs(int[] other)
			{
				int[] ret = pendingChildDocs;
				if (other == null)
				{
					pendingChildDocs = new int[5];
				}
				else
				{
					pendingChildDocs = other;
				}
				return ret;
			}

			internal virtual float[] SwapChildScores(float[] other)
			{
				if (scoreMode == ScoreMode.None)
				{
					throw new InvalidOperationException("ScoreMode is None; you must pass trackScores=false to ToParentBlockJoinCollector"
						);
				}
				float[] ret = pendingChildScores;
				if (other == null)
				{
					pendingChildScores = new float[5];
				}
				else
				{
					pendingChildScores = other;
				}
				return ret;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				//System.out.println("Q.nextDoc() nextChildDoc=" + nextChildDoc);
				// Loop until we hit a parentDoc that's accepted
				while (true)
				{
					if (nextChildDoc == NO_MORE_DOCS)
					{
						//System.out.println("  end");
						return parentDoc = NO_MORE_DOCS;
					}
					// Gather all children sharing the same parent as
					// nextChildDoc
					parentDoc = parentBits.NextSetBit(nextChildDoc);
					// Parent & child docs are supposed to be
					// orthogonal:
					if (nextChildDoc == parentDoc)
					{
						throw new InvalidOperationException("child query must only match non-parent docs, but parent docID="
							 + nextChildDoc + " matched childScorer=" + childScorer.GetType());
					}
					//System.out.println("  parentDoc=" + parentDoc);
					//System.out.println("  nextChildDoc=" + nextChildDoc);
					if (parentDoc != -1 != null && !acceptDocs.Get(parentDoc))
					{
						do
						{
							// Parent doc not accepted; skip child docs until
							// we hit a new parent doc:
							nextChildDoc = childScorer.NextDoc();
						}
						while (nextChildDoc < parentDoc);
						// Parent & child docs are supposed to be
						// orthogonal:
						if (nextChildDoc == parentDoc)
						{
							throw new InvalidOperationException("child query must only match non-parent docs, but parent docID="
								 + nextChildDoc + " matched childScorer=" + childScorer.GetType());
						}
						continue;
					}
					float totalScore = 0;
					float maxScore = float.NegativeInfinity;
					childDocUpto = 0;
					parentFreq = 0;
					do
					{
						//System.out.println("  c=" + nextChildDoc);
						if (pendingChildDocs != null && pendingChildDocs.Length == childDocUpto)
						{
							pendingChildDocs = ArrayUtil.Grow(pendingChildDocs);
						}
						if (pendingChildScores != null && scoreMode != ScoreMode.None && pendingChildScores
							.Length == childDocUpto)
						{
							pendingChildScores = ArrayUtil.Grow(pendingChildScores);
						}
						if (pendingChildDocs != null)
						{
							pendingChildDocs[childDocUpto] = nextChildDoc;
						}
						if (scoreMode != ScoreMode.None)
						{
							// TODO: specialize this into dedicated classes per-scoreMode
							float childScore = childScorer.Score();
							int childFreq = childScorer.Freq();
							if (pendingChildScores != null)
							{
								pendingChildScores[childDocUpto] = childScore;
							}
							maxScore = Math.Max(childScore, maxScore);
							totalScore += childScore;
							parentFreq += childFreq;
						}
						childDocUpto++;
						nextChildDoc = childScorer.NextDoc();
					}
					while (nextChildDoc < parentDoc);
					// Parent & child docs are supposed to be
					// orthogonal:
					if (nextChildDoc == parentDoc)
					{
						throw new InvalidOperationException("child query must only match non-parent docs, but parent docID="
							 + nextChildDoc + " matched childScorer=" + childScorer.GetType());
					}
					switch (scoreMode)
					{
						case ScoreMode.Avg:
						{
							parentScore = totalScore / childDocUpto;
							break;
						}

						case ScoreMode.Max:
						{
							parentScore = maxScore;
							break;
						}

						case ScoreMode.Total:
						{
							parentScore = totalScore;
							break;
						}

						case ScoreMode.None:
						{
							break;
						}
					}
					//System.out.println("  return parentDoc=" + parentDoc + " childDocUpto=" + childDocUpto);
					return parentDoc;
				}
			}

			public override int DocID()
			{
				return parentDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				return parentScore;
			}

			public override int Freq()
			{
				return parentFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int parentTarget)
			{
				//System.out.println("Q.advance parentTarget=" + parentTarget);
				if (parentTarget == NO_MORE_DOCS)
				{
					return parentDoc = NO_MORE_DOCS;
				}
				if (parentTarget == 0)
				{
					// Callers should only be passing in a docID from
					// the parent space, so this means this parent
					// has no children (it got docID 0), so it cannot
					// possibly match.  We must handle this case
					// separately otherwise we pass invalid -1 to
					// prevSetBit below:
					return NextDoc();
				}
				prevParentDoc = parentBits.PrevSetBit(parentTarget - 1);
				//System.out.println("  rolled back to prevParentDoc=" + prevParentDoc + " vs parentDoc=" + parentDoc);
				if (prevParentDoc >= parentDoc > nextChildDoc)
				{
					nextChildDoc = childScorer.Advance(prevParentDoc);
				}
				// System.out.println("  childScorer advanced to child docID=" + nextChildDoc);
				//} else {
				//System.out.println("  skip childScorer advance");
				// Parent & child docs are supposed to be orthogonal:
				if (nextChildDoc == prevParentDoc)
				{
					throw new InvalidOperationException("child query must only match non-parent docs, but parent docID="
						 + nextChildDoc + " matched childScorer=" + childScorer.GetType());
				}
				int nd = NextDoc();
				//System.out.println("  return nextParentDoc=" + nd);
				return nd;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Explanation Explain(int docBase)
			{
				int start = docBase + prevParentDoc + 1;
				// +1 b/c prevParentDoc is previous parent doc
				int end = docBase + parentDoc - 1;
				// -1 b/c parentDoc is parent doc
				return new ComplexExplanation(true, Score(), string.Format(CultureInfo.ROOT, "Score based on child doc range from %d to %d"
					, start, end));
			}

			public override long Cost()
			{
				return childScorer.Cost();
			}

			/// <summary>Instructs this scorer to keep track of the child docIds and score ids for retrieval purposes.
			/// 	</summary>
			/// <remarks>Instructs this scorer to keep track of the child docIds and score ids for retrieval purposes.
			/// 	</remarks>
			public virtual void TrackPendingChildHits()
			{
				pendingChildDocs = new int[5];
				if (scoreMode != ScoreMode.None)
				{
					pendingChildScores = new float[5];
				}
			}
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			childQuery.ExtractTerms(terms);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query childRewrite = childQuery.Rewrite(reader);
			if (childRewrite != childQuery)
			{
				Query rewritten = new ToParentBlockJoinQuery(origChildQuery, childRewrite, parentsFilter
					, scoreMode);
				rewritten.SetBoost(GetBoost());
				return rewritten;
			}
			else
			{
				return this;
			}
		}

		public override string ToString(string field)
		{
			return "ToParentBlockJoinQuery (" + childQuery.ToString() + ")";
		}

		public override bool Equals(object _other)
		{
			if (_other is ToParentBlockJoinQuery)
			{
				ToParentBlockJoinQuery other = (ToParentBlockJoinQuery)_other;
				return origChildQuery.Equals(other.origChildQuery) && parentsFilter.Equals(other.
					parentsFilter) && scoreMode == other.scoreMode && base.Equals(other);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int hash = base.GetHashCode();
			hash = prime * hash + origChildQuery.GetHashCode();
			hash = prime * hash + scoreMode.GetHashCode();
			hash = prime * hash + parentsFilter.GetHashCode();
			return hash;
		}
	}
}
