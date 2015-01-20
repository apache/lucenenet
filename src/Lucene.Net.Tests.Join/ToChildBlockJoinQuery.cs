/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Join;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Join
{
	/// <summary>
	/// Just like
	/// <see cref="ToParentBlockJoinQuery">ToParentBlockJoinQuery</see>
	/// , except this
	/// query joins in reverse: you provide a Query matching
	/// parent documents and it joins down to child
	/// documents.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ToChildBlockJoinQuery : Query
	{
		/// <summary>
		/// Message thrown from
		/// <see cref="ToChildBlockJoinScorer.ValidateParentDoc()">ToChildBlockJoinScorer.ValidateParentDoc()
		/// 	</see>
		/// on mis-use,
		/// when the parent query incorrectly returns child docs.
		/// </summary>
		internal static readonly string INVALID_QUERY_MESSAGE = "Parent query yields document which is not matched by parents filter, docID=";

		private readonly Filter parentsFilter;

		private readonly Query parentQuery;

		private readonly Query origParentQuery;

		private readonly bool doScores;

		/// <summary>Create a ToChildBlockJoinQuery.</summary>
		/// <remarks>Create a ToChildBlockJoinQuery.</remarks>
		/// <param name="parentQuery">Query that matches parent documents</param>
		/// <param name="parentsFilter">
		/// Filter (must produce FixedBitSet
		/// per-segment, like
		/// <see cref="FixedBitSetCachingWrapperFilter">FixedBitSetCachingWrapperFilter</see>
		/// )
		/// identifying the parent documents.
		/// </param>
		/// <param name="doScores">true if parent scores should be calculated</param>
		public ToChildBlockJoinQuery(Query parentQuery, Filter parentsFilter, bool doScores
			) : base()
		{
			// If we are rewritten, this is the original parentQuery we
			// were passed; we use this for .equals() and
			// .hashCode().  This makes rewritten query equal the
			// original, so that user does not have to .rewrite() their
			// query before searching:
			this.origParentQuery = parentQuery;
			this.parentQuery = parentQuery;
			this.parentsFilter = parentsFilter;
			this.doScores = doScores;
		}

		private ToChildBlockJoinQuery(Query origParentQuery, Query parentQuery, Filter parentsFilter
			, bool doScores) : base()
		{
			this.origParentQuery = origParentQuery;
			this.parentQuery = parentQuery;
			this.parentsFilter = parentsFilter;
			this.doScores = doScores;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new ToChildBlockJoinQuery.ToChildBlockJoinWeight(this, parentQuery.CreateWeight
				(searcher), parentsFilter, doScores);
		}

		private class ToChildBlockJoinWeight : Weight
		{
			private readonly Query joinQuery;

			private readonly Weight parentWeight;

			private readonly Filter parentsFilter;

			private readonly bool doScores;

			public ToChildBlockJoinWeight(Query joinQuery, Weight parentWeight, Filter parentsFilter
				, bool doScores) : base()
			{
				this.joinQuery = joinQuery;
				this.parentWeight = parentWeight;
				this.parentsFilter = parentsFilter;
				this.doScores = doScores;
			}

			public override Query GetQuery()
			{
				return joinQuery;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				return parentWeight.GetValueForNormalization() * joinQuery.GetBoost() * joinQuery
					.GetBoost();
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				parentWeight.Normalize(norm, topLevelBoost * joinQuery.GetBoost());
			}

			// NOTE: acceptDocs applies (and is checked) only in the
			// child document space
			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Search.Scorer Scorer(AtomicReaderContext readerContext
				, Bits acceptDocs)
			{
				Org.Apache.Lucene.Search.Scorer parentScorer = parentWeight.Scorer(readerContext, 
					null);
				if (parentScorer == null)
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
				return new ToChildBlockJoinQuery.ToChildBlockJoinScorer(this, parentScorer, (FixedBitSet
					)parents, doScores, acceptDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext reader, int doc)
			{
				// TODO
				throw new NotSupportedException(GetType().FullName + " cannot explain match on parent document"
					);
			}

			public override bool ScoresDocsOutOfOrder()
			{
				return false;
			}
		}

		internal class ToChildBlockJoinScorer : Scorer
		{
			private readonly Scorer parentScorer;

			private readonly FixedBitSet parentBits;

			private readonly bool doScores;

			private readonly Bits acceptDocs;

			private float parentScore;

			private int parentFreq = 1;

			private int childDoc = -1;

			private int parentDoc;

			public ToChildBlockJoinScorer(Weight weight, Scorer parentScorer, FixedBitSet parentBits
				, bool doScores, Bits acceptDocs) : base(weight)
			{
				this.doScores = doScores;
				this.parentBits = parentBits;
				this.parentScorer = parentScorer;
				this.acceptDocs = acceptDocs;
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				return Sharpen.Collections.Singleton(new Scorer.ChildScorer(parentScorer, "BLOCK_JOIN"
					));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				//System.out.println("Q.nextDoc() parentDoc=" + parentDoc + " childDoc=" + childDoc);
				// Loop until we hit a childDoc that's accepted
				while (true)
				{
					if (childDoc + 1 == parentDoc)
					{
						// OK, we are done iterating through all children
						// matching this one parent doc, so we now nextDoc()
						// the parent.  Use a while loop because we may have
						// to skip over some number of parents w/ no
						// children:
						while (true)
						{
							parentDoc = parentScorer.NextDoc();
							ValidateParentDoc();
							if (parentDoc == 0)
							{
								// Degenerate but allowed: first parent doc has no children
								// TODO: would be nice to pull initial parent
								// into ctor so we can skip this if... but it's
								// tricky because scorer must return -1 for
								// .doc() on init...
								parentDoc = parentScorer.NextDoc();
								ValidateParentDoc();
							}
							if (parentDoc == NO_MORE_DOCS)
							{
								childDoc = NO_MORE_DOCS;
								//System.out.println("  END");
								return childDoc;
							}
							// Go to first child for this next parentDoc:
							childDoc = 1 + parentBits.PrevSetBit(parentDoc - 1);
							if (childDoc == parentDoc)
							{
								// This parent has no children; continue
								// parent loop so we move to next parent
								continue;
							}
							if (acceptDocs != null && !acceptDocs.Get(childDoc))
							{
								goto nextChildDoc_continue;
							}
							if (childDoc < parentDoc)
							{
								if (doScores)
								{
									parentScore = parentScorer.Score();
									parentFreq = parentScorer.Freq();
								}
								//System.out.println("  " + childDoc);
								return childDoc;
							}
						}
					}
					else
					{
						// Degenerate but allowed: parent has no children
						//HM:assert
						//assert childDoc < parentDoc: "childDoc=" + childDoc + " parentDoc=" + parentDoc;
						childDoc++;
						if (acceptDocs != null && !acceptDocs.Get(childDoc))
						{
							continue;
						}
						//System.out.println("  " + childDoc);
						return childDoc;
					}
nextChildDoc_continue: ;
				}
nextChildDoc_break: ;
			}

			/// <summary>
			/// Detect mis-use, where provided parent query in fact
			/// sometimes returns child documents.
			/// </summary>
			/// <remarks>
			/// Detect mis-use, where provided parent query in fact
			/// sometimes returns child documents.
			/// </remarks>
			private void ValidateParentDoc()
			{
				if (parentDoc != NO_MORE_DOCS && !parentBits.Get(parentDoc))
				{
					throw new InvalidOperationException(INVALID_QUERY_MESSAGE + parentDoc);
				}
			}

			public override int DocID()
			{
				return childDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				return parentScore;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return parentFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int childTarget)
			{
				//System.out.println("Q.advance childTarget=" + childTarget);
				if (childTarget >= parentBits.Length() || !parentBits.Get(childTarget) == NO_MORE_DOCS)
				{
					//System.out.println("  END");
					return childDoc = parentDoc = NO_MORE_DOCS;
				}
				//HM:revisit
				//assert childDoc == -1 || childTarget != parentDoc: "childTarget=" + childTarget;
				if (childDoc == -1 || childTarget > parentDoc)
				{
					// Advance to new parent:
					parentDoc = parentScorer.Advance(childTarget);
					ValidateParentDoc();
					//System.out.println("  advance to parentDoc=" + parentDoc);
					if (parentDoc > childTarget == NO_MORE_DOCS)
					{
						//System.out.println("  END");
						return childDoc = NO_MORE_DOCS;
					}
					if (doScores)
					{
						parentScore = parentScorer.Score();
						parentFreq = parentScorer.Freq();
					}
					int firstChild = parentBits.PrevSetBit(parentDoc - 1);
					//System.out.println("  firstChild=" + firstChild);
					childTarget = Math.Max(childTarget, firstChild);
				}
				// Advance within children of current parent:
				childTarget < parentDoc = childTarget;
				//System.out.println("  " + childDoc);
				if (acceptDocs != null && !acceptDocs.Get(childDoc))
				{
					NextDoc();
				}
				return childDoc;
			}

			public override long Cost()
			{
				return parentScorer.Cost();
			}
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			parentQuery.ExtractTerms(terms);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query parentRewrite = parentQuery.Rewrite(reader);
			if (parentRewrite != parentQuery)
			{
				Query rewritten = new ToChildBlockJoinQuery(parentQuery, parentRewrite, parentsFilter
					, doScores);
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
			return "ToChildBlockJoinQuery (" + parentQuery.ToString() + ")";
		}

		public override bool Equals(object _other)
		{
			if (_other is ToChildBlockJoinQuery)
			{
				ToChildBlockJoinQuery other = (ToChildBlockJoinQuery)_other;
				return origParentQuery.Equals(other.origParentQuery) && parentsFilter.Equals(other
					.parentsFilter) && doScores == other.doScores && base.Equals(other);
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
			hash = prime * hash + origParentQuery.GetHashCode();
			hash = prime * hash + doScores.GetHashCode();
			hash = prime * hash + parentsFilter.GetHashCode();
			return hash;
		}

		public override Query Clone()
		{
			return new ToChildBlockJoinQuery(origParentQuery.Clone(), parentsFilter, doScores
				);
		}
	}
}
