// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Join
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
    /// <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/> 
    /// or <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)"/>
    /// API.  In each block, the
    /// child documents must appear first, ending with the parent
    /// document.  At search time you provide a <see cref="Filter"/>
    /// identifying the parents, however this <see cref="Filter"/> must provide
    /// an <see cref="FixedBitSet"/> per sub-reader.
    /// 
    /// <para>Once the block index is built, use this query to wrap
    /// any sub-query matching only child docs and join matches in that
    /// child document space up to the parent document space.
    /// You can then use this <see cref="Query"/> as a clause with
    /// other queries in the parent document space.</para>
    /// 
    /// <para>See <see cref="ToChildBlockJoinQuery"/> if you need to join
    /// in the reverse order.</para>
    /// 
    /// <para>The child documents must be orthogonal to the parent
    /// documents: the wrapped child query must never
    /// return a parent document.</para>
    /// 
    /// <para>If you'd like to retrieve <see cref="Lucene.Net.Search.Grouping.ITopGroups{T}"/> for the
    /// resulting query, use the <see cref="ToParentBlockJoinCollector"/>.
    /// Note that this is not necessary, ie, if you simply want
    /// to collect the parent documents and don't need to see
    /// which child documents matched under that parent, then
    /// you can use any collector.</para>
    /// 
    /// <para><b>NOTE</b>: If the overall query contains parent-only
    /// matches, for example you OR a parent-only query with a
    /// joined child-only query, then the resulting collected documents
    /// will be correct, however the <see cref="Lucene.Net.Search.Grouping.ITopGroups{T}"/> you get
    /// from <see cref="ToParentBlockJoinCollector"/> will not contain every
    /// child for parents that had matched.</para>
    /// 
    /// <para>See <a href="http://lucene.apache.org/core/4_8_0/join/">http://lucene.apache.org/core/4_8_0/join/</a> for an
    /// overview. </para>
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
        /// Create a <see cref="ToParentBlockJoinQuery"/>.
        /// </summary>
        /// <param name="childQuery"> <see cref="Query"/> matching child documents. </param>
        /// <param name="parentsFilter"> <see cref="Filter"/> (must produce <see cref="FixedBitSet"/>
        /// per-segment, like <see cref="FixedBitSetCachingWrapperFilter"/>)
        /// identifying the parent documents. </param>
        /// <param name="scoreMode"> How to aggregate multiple child scores
        /// into a single parent score.
        ///  </param>
        public ToParentBlockJoinQuery(Query childQuery, Filter parentsFilter, ScoreMode scoreMode)
            : base()
        {
            _origChildQuery = childQuery;
            _childQuery = childQuery;
            _parentsFilter = parentsFilter;
            _scoreMode = scoreMode;
        }

        private ToParentBlockJoinQuery(Query origChildQuery, Query childQuery, Filter parentsFilter, ScoreMode scoreMode) 
            : base()
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
            private readonly Query joinQuery;
            private readonly Weight childWeight;
            private readonly Filter parentsFilter;
            private readonly ScoreMode scoreMode;

            public BlockJoinWeight(Query joinQuery, Weight childWeight, Filter parentsFilter, ScoreMode scoreMode) 
                : base()
            {
                this.joinQuery = joinQuery;
                this.childWeight = childWeight;
                this.parentsFilter = parentsFilter;
                this.scoreMode = scoreMode;
            }

            public override Query Query => joinQuery;

            public override float GetValueForNormalization()
            {
                return childWeight.GetValueForNormalization() * joinQuery.Boost*joinQuery.Boost;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                childWeight.Normalize(norm, topLevelBoost * joinQuery.Boost);
            }

            // NOTE: acceptDocs applies (and is checked) only in the parent document space
            public override Scorer GetScorer(AtomicReaderContext readerContext, IBits acceptDocs)
            {

                Scorer childScorer = childWeight.GetScorer(readerContext, readerContext.AtomicReader.LiveDocs);
                if (childScorer is null)
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

                if (parents is null)
                {
                    // No matches
                    return null;
                }
                if (!(parents is FixedBitSet))
                {
                    throw IllegalStateException.Create("parentFilter must return FixedBitSet; got " + parents);
                }

                return new BlockJoinScorer(this, childScorer, (FixedBitSet)parents, firstChildDoc, scoreMode, acceptDocs);
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

            public override bool ScoresDocsOutOfOrder => false;
        }

        internal class BlockJoinScorer : Scorer
        {
            private readonly Scorer _childScorer;
            private readonly FixedBitSet _parentBits;
            private readonly ScoreMode _scoreMode;
            private readonly IBits _acceptDocs;
            private int _parentDoc = -1;
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
                return new JCG.List<ChildScorer> { new ChildScorer(_childScorer, "BLOCK_JOIN") };
            }

            internal virtual int ChildCount => _childDocUpto;

            internal virtual int ParentDoc => _parentDoc;

            internal virtual int[] SwapChildDocs(int[] other)
            {
                int[] ret = _pendingChildDocs;
                if (other is null)
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
                    throw IllegalStateException.Create("ScoreMode is None; you must pass trackScores=false to ToParentBlockJoinCollector");
                }
                float[] ret = _pendingChildScores;
                if (other is null)
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
                        return _parentDoc = NO_MORE_DOCS;
                    }

                    // Gather all children sharing the same parent as
                    // nextChildDoc

                    _parentDoc = _parentBits.NextSetBit(_nextChildDoc);

                    // Parent & child docs are supposed to be
                    // orthogonal:
                    if (_nextChildDoc == _parentDoc)
                    {
                        throw IllegalStateException.Create("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                    }

                    //System.out.println("  parentDoc=" + parentDoc);
                    if (Debugging.AssertsEnabled) Debugging.Assert(_parentDoc != -1);

                    //System.out.println("  nextChildDoc=" + nextChildDoc);
                    if (_acceptDocs != null && !_acceptDocs.Get(_parentDoc))
                    {
                        // Parent doc not accepted; skip child docs until
                        // we hit a new parent doc:
                        do
                        {
                            _nextChildDoc = _childScorer.NextDoc();
                        } while (_nextChildDoc < _parentDoc);

                        // Parent & child docs are supposed to be
                        // orthogonal:
                        if (_nextChildDoc == _parentDoc)
                        {
                            throw IllegalStateException.Create("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
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
                    } while (_nextChildDoc < _parentDoc);

                    // Parent & child docs are supposed to be
                    // orthogonal:
                    if (_nextChildDoc == _parentDoc)
                    {
                        throw IllegalStateException.Create("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
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
                    return _parentDoc;
                }
            }

            public override int DocID => _parentDoc;

            public override float GetScore()
            {
                return _parentScore;
            }

            public override int Freq => _parentFreq;

            public override int Advance(int parentTarget)
            {

                //System.out.println("Q.advance parentTarget=" + parentTarget);
                if (parentTarget == NO_MORE_DOCS)
                {
                    return _parentDoc = NO_MORE_DOCS;
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
                if (Debugging.AssertsEnabled) Debugging.Assert(_prevParentDoc >= _parentDoc);
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
                    throw IllegalStateException.Create("child query must only match non-parent docs, but parent docID=" + _nextChildDoc + " matched childScorer=" + _childScorer.GetType());
                }

                int nd = NextDoc();
                //System.out.println("  return nextParentDoc=" + nd);
                return nd;
            }
            
            public virtual Explanation Explain(int docBase)
            {
                int start = docBase + _prevParentDoc + 1; // +1 b/c prevParentDoc is previous parent doc
                int end = docBase + _parentDoc - 1; // -1 b/c parentDoc is parent doc
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

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is ToParentBlockJoinQuery other)
            {
                return _origChildQuery.Equals(other._origChildQuery) &&
                    _parentsFilter.Equals(other._parentsFilter) &&
                    _scoreMode == other._scoreMode &&
                    base.Equals(other);
            }
            else
            {
                return false;
            }
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