using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Join
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// This query requires that you index
    /// children and parent docs as a single block, using the
    /// <see cref="IndexWriter#addDocuments IndexWriter.addDocuments()"/> or {@link
    /// IndexWriter#updateDocuments IndexWriter.updateDocuments()} API.  In each block, the
    /// child documents must appear first, ending with the parent
    /// document.  At search time you provide a Filter
    /// identifying the parents, however this Filter must provide
    /// an <see cref="FixedBitSet"/> per sub-reader.
    /// 
    /// <p>Once the block index is built, use this query to wrap
    /// any sub-query matching only child docs and join matches in that
    /// child document space up to the parent document space.
    /// You can then use this Query as a clause with
    /// other queries in the parent document space.</p>
    /// 
    /// <p>See <see cref="ToChildBlockJoinQuery"/> if you need to join
    /// in the reverse order.
    /// 
    /// <p>The child documents must be orthogonal to the parent
    /// documents: the wrapped child query must never
    /// return a parent document.</p>
    /// 
    /// If you'd like to retrieve <see cref="TopGroups"/> for the
    /// resulting query, use the <see cref="ToParentBlockJoinCollector"/>.
    /// Note that this is not necessary, ie, if you simply want
    /// to collect the parent documents and don't need to see
    /// which child documents matched under that parent, then
    /// you can use any collector.
    /// 
    /// <p><b>NOTE</b>: If the overall query contains parent-only
    /// matches, for example you OR a parent-only query with a
    /// joined child-only query, then the resulting collected documents
    /// will be correct, however the <see cref="TopGroups"/> you get
    /// from <see cref="ToParentBlockJoinCollector"/> will not contain every
    /// child for parents that had matched.
    /// 
    /// <p>See <see cref="org.apache.lucene.search.join"/> for an
    /// overview. </p>
    /// 
    /// @lucene.experimental
    /// </summary>
    public class ToParentBlockJoinQuery : Query
    {

        private readonly Filter _parentsFilter;
        private readonly Query _childQuery;

        // If we are rewritten, this is the original childQuery we
        // were passed; we use this for .equals() and
        // .hashCode().  This makes rewritten query equal the
        // original, so that user does not have to .rewrite() their
        // query before searching:
        private readonly Query _origChildQuery;
        private readonly ScoreMode _scoreMode;

        /// <summary>
        /// Create a ToParentBlockJoinQuery.
        /// </summary>
        /// <param name="childQuery"> Query matching child documents. </param>
        /// <param name="parentsFilter"> Filter (must produce FixedBitSet
        /// per-segment, like <see cref="FixedBitSetCachingWrapperFilter"/>)
        /// identifying the parent documents. </param>
        /// <param name="scoreMode"> How to aggregate multiple child scores
        /// into a single parent score.
        ///  </param>
        public ToParentBlockJoinQuery(Query childQuery, Filter parentsFilter, ScoreMode scoreMode)
        {
            _origChildQuery = childQuery;
            _childQuery = childQuery;
            _parentsFilter = parentsFilter;
            _scoreMode = scoreMode;
        }

        private ToParentBlockJoinQuery(Query origChildQuery, Query childQuery, Filter parentsFilter, ScoreMode scoreMode) : base()
        {
            _origChildQuery = origChildQuery;
            _childQuery = childQuery;
            _parentsFilter = parentsFilter;
            _scoreMode = scoreMode;
        }
        
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new BlockJoinWeight(this, _childQuery.CreateWeight(searcher), _parentsFilter, _scoreMode);
        }

        private class BlockJoinWeight : Weight
        {
            internal readonly Query JoinQuery;
            internal readonly Weight ChildWeight;
            internal readonly Filter ParentsFilter;
            internal readonly ScoreMode ScoreMode;

            public BlockJoinWeight(Query joinQuery, Weight childWeight, Filter parentsFilter, ScoreMode scoreMode) : base()
            {
                JoinQuery = joinQuery;
                ChildWeight = childWeight;
                ParentsFilter = parentsFilter;
                ScoreMode = scoreMode;
            }

            public override Query Query
            {
                get { return JoinQuery; }
            }
            
            public override float GetValueForNormalization()
            {
                return ChildWeight.GetValueForNormalization() * JoinQuery.Boost*JoinQuery.Boost;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                ChildWeight.Normalize(norm, topLevelBoost * JoinQuery.Boost);
            }

            // NOTE: acceptDocs applies (and is checked) only in the parent document space
            public override Scorer GetScorer(AtomicReaderContext readerContext, IBits acceptDocs)
            {

                Scorer childScorer = ChildWeight.GetScorer(readerContext, readerContext.AtomicReader.LiveDocs);
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
                DocIdSet parents = ParentsFilter.GetDocIdSet(readerContext, null);

                if (parents == null)
                {
                    // No matches
                    return null;
                }
                if (!(parents is FixedBitSet))
                {
                    throw new InvalidOperationException("parentFilter must return FixedBitSet; got " + parents);
                }

                return new BlockJoinScorer(this, childScorer, (FixedBitSet)parents, firstChildDoc, ScoreMode, acceptDocs);
            }
            
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                BlockJoinScorer scorer = (BlockJoinScorer)GetScorer(context, context.AtomicReader.LiveDocs);
                if (scorer != null && scorer.Advance(doc) == doc)
                {
                    return scorer.Explain(context.DocBase);
                }
                return new ComplexExplanation(false, 0.0f, "Not a match");
            }

            public override bool ScoresDocsOutOfOrder
            {
                get { return false; }
            }
        }

        internal class BlockJoinScorer : Scorer
        {
            private readonly Scorer _childScorer;
            private readonly FixedBitSet _parentBits;
            private readonly ScoreMode _scoreMode;
            private readonly IBits _acceptDocs;
            private int _parentDocRenamed = -1;
            private int _prevParentDoc;
            private float _parentScore;
            private int _parentFreq;
            private int _nextChildDoc;
            private int[] _pendingChildDocs;
            private float[] _pendingChildScores;
            private int _childDocUpto;

            public BlockJoinScorer(Weight weight, Scorer childScorer, FixedBitSet parentBits, int firstChildDoc, ScoreMode scoreMode, IBits acceptDocs) : base(weight)
            {
                //System.out.println("Q.init firstChildDoc=" + firstChildDoc);
                _parentBits = parentBits;
                _childScorer = childScorer;
                _scoreMode = scoreMode;
                _acceptDocs = acceptDocs;
                _nextChildDoc = firstChildDoc;
            }

            public override ICollection<ChildScorer> GetChildren()
            {
                return Collections.Singleton(new ChildScorer(_childScorer, "BLOCK_JOIN"));
            }

            internal virtual int ChildCount
            {
                get { return _childDocUpto; }
            }

            internal virtual int ParentDoc
            {
                get { return _parentDocRenamed; }
            }

            internal virtual int[] SwapChildDocs(int[] other)
            {
                int[] ret = _pendingChildDocs;
                if (other == null)
                {
                    _pendingChildDocs = new int[5];
                }
                else
                {
                    _pendingChildDocs = other;
                }
                return ret;
            }

            internal virtual float[] SwapChildScores(float[] other)
            {
                if (_scoreMode == ScoreMode.None)
                {
                    throw new InvalidOperationException("ScoreMode is None; you must pass trackScores=false to ToParentBlockJoinCollector");
                }
                float[] ret = _pendingChildScores;
                if (other == null)
                {
                    _pendingChildScores = new float[5];
                }
                else
                {
                    _pendingChildScores = other;
                }
                return ret;
            }
            
            public override int NextDoc()
            {
                //System.out.println("Q.nextDoc() nextChildDoc=" + nextChildDoc);
                // Loop until we hit a parentDoc that's accepted
                while (true)
                {
                    if (_nextChildDoc == NO_MORE_DOCS)
                    {
                        //System.out.println("  end");
                        return _parentDocRenamed = NO_MORE_DOCS;
                    }

                    // Gather all children sharing the same parent as
                    // nextChildDoc

                    _parentDocRenamed = _parentBits.NextSetBit(_nextChildDoc);

                    // Parent & child docs are supposed to be
                    // orthogonal:
                    if (_nextChildDoc == _parentDocRenamed)
                    {
                        throw new InvalidOperationException("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                    }

                    //System.out.println("  parentDoc=" + parentDoc);
                    Debug.Assert(_parentDocRenamed != -1);

                    //System.out.println("  nextChildDoc=" + nextChildDoc);
                    if (_acceptDocs != null && !_acceptDocs.Get(_parentDocRenamed))
                    {
                        // Parent doc not accepted; skip child docs until
                        // we hit a new parent doc:
                        do
                        {
                            _nextChildDoc = _childScorer.NextDoc();
                        } while (_nextChildDoc < _parentDocRenamed);

                        // Parent & child docs are supposed to be
                        // orthogonal:
                        if (_nextChildDoc == _parentDocRenamed)
                        {
                            throw new InvalidOperationException("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                        }

                        continue;
                    }

                    float totalScore = 0;
                    float maxScore = float.NegativeInfinity;

                    _childDocUpto = 0;
                    _parentFreq = 0;
                    do
                    {
                        //System.out.println("  c=" + nextChildDoc);
                        if (_pendingChildDocs != null && _pendingChildDocs.Length == _childDocUpto)
                        {
                            _pendingChildDocs = ArrayUtil.Grow(_pendingChildDocs);
                        }
                        if (_pendingChildScores != null && _scoreMode != ScoreMode.None && _pendingChildScores.Length == _childDocUpto)
                        {
                            _pendingChildScores = ArrayUtil.Grow(_pendingChildScores);
                        }
                        if (_pendingChildDocs != null)
                        {
                            _pendingChildDocs[_childDocUpto] = _nextChildDoc;
                        }
                        if (_scoreMode != ScoreMode.None)
                        {
                            // TODO: specialize this into dedicated classes per-scoreMode
                            float childScore = _childScorer.GetScore();
                            int childFreq = _childScorer.Freq;
                            if (_pendingChildScores != null)
                            {
                                _pendingChildScores[_childDocUpto] = childScore;
                            }
                            maxScore = Math.Max(childScore, maxScore);
                            totalScore += childScore;
                            _parentFreq += childFreq;
                        }
                        _childDocUpto++;
                        _nextChildDoc = _childScorer.NextDoc();
                    } while (_nextChildDoc < _parentDocRenamed);

                    // Parent & child docs are supposed to be
                    // orthogonal:
                    if (_nextChildDoc == _parentDocRenamed)
                    {
                        throw new InvalidOperationException("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                    }

                    switch (_scoreMode)
                    {
                        case ScoreMode.Avg:
                            _parentScore = totalScore / _childDocUpto;
                            break;
                        case ScoreMode.Max:
                            _parentScore = maxScore;
                            break;
                        case ScoreMode.Total:
                            _parentScore = totalScore;
                            break;
                        case ScoreMode.None:
                            break;
                    }

                    //System.out.println("  return parentDoc=" + parentDoc + " childDocUpto=" + childDocUpto);
                    return _parentDocRenamed;
                }
            }

            public override int DocID
            {
                get { return _parentDocRenamed; }
            }

            public override float GetScore()
            {
                return _parentScore;
            }

            public override int Freq
            {
                get { return _parentFreq; }
            }
            
            public override int Advance(int parentTarget)
            {

                //System.out.println("Q.advance parentTarget=" + parentTarget);
                if (parentTarget == NO_MORE_DOCS)
                {
                    return _parentDocRenamed = NO_MORE_DOCS;
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

                _prevParentDoc = _parentBits.PrevSetBit(parentTarget - 1);

                //System.out.println("  rolled back to prevParentDoc=" + prevParentDoc + " vs parentDoc=" + parentDoc);
                Debug.Assert(_prevParentDoc >= _parentDocRenamed);
                if (_prevParentDoc > _nextChildDoc)
                {
                    _nextChildDoc = _childScorer.Advance(_prevParentDoc);
                    // System.out.println("  childScorer advanced to child docID=" + nextChildDoc);
                    //} else {
                    //System.out.println("  skip childScorer advance");
                }

                // Parent & child docs are supposed to be orthogonal:
                if (_nextChildDoc == _prevParentDoc)
                {
                    throw new InvalidOperationException("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                }

                int nd = NextDoc();
                //System.out.println("  return nextParentDoc=" + nd);
                return nd;
            }
            
            public virtual Explanation Explain(int docBase)
            {
                int start = docBase + _prevParentDoc + 1; // +1 b/c prevParentDoc is previous parent doc
                int end = docBase + _parentDocRenamed - 1; // -1 b/c parentDoc is parent doc
                return new ComplexExplanation(true, GetScore(), string.Format("Score based on child doc range from {0} to {1}", start, end));
            }

            public override long GetCost()
            {
                return _childScorer.GetCost();
            }

            /// <summary>
            /// Instructs this scorer to keep track of the child docIds and score ids for retrieval purposes.
            /// </summary>
            public virtual void TrackPendingChildHits()
            {
                _pendingChildDocs = new int[5];
                if (_scoreMode != ScoreMode.None)
                {
                    _pendingChildScores = new float[5];
                }
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _childQuery.ExtractTerms(terms);
        }
        
        public override Query Rewrite(IndexReader reader)
        {
            Query childRewrite = _childQuery.Rewrite(reader);
            if (childRewrite != _childQuery)
            {
                Query rewritten = new ToParentBlockJoinQuery(_origChildQuery, childRewrite, _parentsFilter, _scoreMode);
                rewritten.Boost = Boost;
                return rewritten;
            }
            return this;
        }

        public override string ToString(string field)
        {
            return "ToParentBlockJoinQuery (" + _childQuery + ")";
        }

        protected bool Equals(ToParentBlockJoinQuery other)
        {
            return base.Equals(other) && 
                Equals(_parentsFilter, other._parentsFilter) && 
                _scoreMode == other._scoreMode && 
                Equals(_origChildQuery, other._origChildQuery);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ToParentBlockJoinQuery) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ (_parentsFilter != null ? _parentsFilter.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) _scoreMode;
                hashCode = (hashCode*397) ^ (_origChildQuery != null ? _origChildQuery.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}