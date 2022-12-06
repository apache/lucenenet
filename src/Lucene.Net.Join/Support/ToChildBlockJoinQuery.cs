// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Just like <see cref="ToParentBlockJoinQuery"/>, except this
    /// query joins in reverse: you provide a <see cref="Query"/> matching
    /// parent documents and it joins down to child
    /// documents.
    ///
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Use Lucene.Net.Search.Join.ToChildBlockJoinQuery instead. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ToChildBlockJoinQuery : Query
    {
        /// <summary>
        /// Message thrown from <see cref="ToChildBlockJoinScorer.ValidateParentDoc"/>
        /// on mis-use, when the parent query incorrectly returns child docs.
        /// </summary>
        internal const string INVALID_QUERY_MESSAGE = "Parent query yields document which is not matched by parents filter, docID=";

        private readonly Filter _parentsFilter;
        private readonly Query _parentQuery;

        // If we are rewritten, this is the original parentQuery we
        // were passed; we use this for .equals() and
        // .hashCode().  This makes rewritten query equal the
        // original, so that user does not have to .rewrite() their
        // query before searching:
        private readonly Query _origParentQuery;
        private readonly bool _doScores;

        /// <summary>
        /// Create a <see cref="ToChildBlockJoinQuery"/>.
        /// </summary>
        /// <param name="parentQuery"><see cref="Query"/> that matches parent documents</param>
        /// <param name="parentsFilter"><see cref="Filter"/> (must produce FixedBitSet per-segment, like <see cref="FixedBitSetCachingWrapperFilter"/>)
        /// identifying the parent documents.</param>
        /// <param name="doScores">True if parent scores should be calculated.</param>
        public ToChildBlockJoinQuery(Query parentQuery, Filter parentsFilter, bool doScores)
            : base()
        {
            _origParentQuery = parentQuery;
            _parentQuery = parentQuery;
            _parentsFilter = parentsFilter;
            _doScores = doScores;
        }

        private ToChildBlockJoinQuery(Query origParentQuery, Query parentQuery, Filter parentsFilter, bool doScores)
            : base()
        {
            _origParentQuery = origParentQuery;
            _parentQuery = parentQuery;
            _parentsFilter = parentsFilter;
            _doScores = doScores;
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new ToChildBlockJoinWeight(this, _parentQuery.CreateWeight(searcher), _parentsFilter, _doScores);
        }

        private class ToChildBlockJoinWeight : Weight
        {
            private readonly Query _joinQuery;
            private readonly Weight _parentWeight;
            private readonly Filter _parentsFilter;
            private readonly bool _doScores;

            public ToChildBlockJoinWeight(Query joinQuery, Weight parentWeight, Filter parentsFilter, bool doScores)
                : base()
            {
                _joinQuery = joinQuery;
                _parentWeight = parentWeight;
                _parentsFilter = parentsFilter;
                _doScores = doScores;
            }

            public override Query Query => _joinQuery;

            public override float GetValueForNormalization()
            {
                return _parentWeight.GetValueForNormalization() * _joinQuery.Boost * _joinQuery.Boost;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                _parentWeight.Normalize(norm, topLevelBoost * _joinQuery.Boost);
            }

            // NOTE: acceptDocs applies (and is checked) only in the child document space
            public override Scorer GetScorer(AtomicReaderContext readerContext, IBits acceptDocs)
            {
                Scorer parentScorer = _parentWeight.GetScorer(readerContext, null);

                if (parentScorer is null)
                {
                    // No matches
                    return null;
                }

                // NOTE: we cannot pass acceptDocs here because this
                // will (most likely, justifiably) cause the filter to
                // not return a FixedBitSet but rather a
                // BitsFilteredDocIdSet.  Instead, we filter by
                // acceptDocs when we score:
                DocIdSet parents = _parentsFilter.GetDocIdSet(readerContext, null);

                if (parents is null)
                {
                    // No matches
                    return null;
                }
                if (!(parents is FixedBitSet))
                {
                    throw IllegalStateException.Create("parentFilter must return FixedBitSet; got " + parents);
                }

                return new ToChildBlockJoinScorer(this, parentScorer, (FixedBitSet)parents, _doScores, acceptDocs);
            }

            public override Explanation Explain(AtomicReaderContext reader, int doc)
            {
                // TODO
                throw UnsupportedOperationException.Create(GetType().Name + " cannot explain match on parent document");
            }

            public override bool ScoresDocsOutOfOrder => false;
        }

        private sealed class ToChildBlockJoinScorer : Scorer
        {
            private readonly Scorer _parentScorer;
            private readonly FixedBitSet _parentBits;
            private readonly bool _doScores;
            private readonly IBits _acceptDocs;

            private float _parentScore;
            private int _parentFreq = 1;

            private int _childDoc = -1;
            private int _parentDoc;

            public ToChildBlockJoinScorer(Weight weight, Scorer parentScorer, FixedBitSet parentBits, bool doScores, IBits acceptDocs)
                : base(weight)
            {
                _doScores = doScores;
                _parentBits = parentBits;
                _parentScorer = parentScorer;
                _acceptDocs = acceptDocs;
            }

            public override ICollection<ChildScorer> GetChildren()
            {
                return new JCG.List<ChildScorer> { new ChildScorer(_parentScorer, "BLOCK_JOIN") };
            }

            public override int NextDoc()
            {
                //System.out.println("Q.nextDoc() parentDoc=" + parentDoc + " childDoc=" + childDoc);

                // Loop until we hit a childDoc that's accepted
                while (true)
                {
                    if (_childDoc + 1 == _parentDoc)
                    {
                        // OK, we are done iterating through all children
                        // matching this one parent doc, so we now nextDoc()
                        // the parent.  Use a while loop because we may have
                        // to skip over some number of parents w/ no
                        // children:
                        while (true)
                        {
                            _parentDoc = _parentScorer.NextDoc();
                            ValidateParentDoc();

                            if (_parentDoc == 0)
                            {
                                // Degenerate but allowed: first parent doc has no children
                                // TODO: would be nice to pull initial parent
                                // into ctor so we can skip this if... but it's
                                // tricky because scorer must return -1 for
                                // .doc() on init...
                                _parentDoc = _parentScorer.NextDoc();
                                ValidateParentDoc();
                            }

                            if (_parentDoc == NO_MORE_DOCS)
                            {
                                _childDoc = NO_MORE_DOCS;
                                //System.out.println("  END");
                                return _childDoc;
                            }

                            // Go to first child for this next parentDoc:
                            _childDoc = 1 + _parentBits.PrevSetBit(_parentDoc - 1);

                            if (_childDoc == _parentDoc)
                            {
                                // This parent has no children; continue
                                // parent loop so we move to next parent
                                continue;
                            }

                            if (_acceptDocs != null && !_acceptDocs.Get(_childDoc))
                            {
                                goto nextChildDocContinue;
                            }

                            if (_childDoc < _parentDoc)
                            {
                                if (_doScores)
                                {
                                    _parentScore = _parentScorer.GetScore();
                                    _parentFreq = _parentScorer.Freq;
                                }
                                //System.out.println("  " + childDoc);
                                return _childDoc;
                            }
                            else
                            {
                                // Degenerate but allowed: parent has no children
                            }
                        }
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(_childDoc < _parentDoc, "childDoc={0} parentDoc={1}", _childDoc, _parentDoc);
                    _childDoc++;
                    if (_acceptDocs != null && !_acceptDocs.Get(_childDoc))
                    {
                        continue;
                    }
                    //System.out.println("  " + childDoc);
                    return _childDoc;
                nextChildDocContinue: {/* LUCENENET: intentionally blank */}
                }
            }

            /// <summary>
            /// Detect mis-use, where provided parent query in fact sometimes returns child documents.
            /// </summary>
            private void ValidateParentDoc()
            {
                if (_parentDoc != NO_MORE_DOCS && !_parentBits.Get(_parentDoc))
                {
                    throw IllegalStateException.Create(INVALID_QUERY_MESSAGE + _parentDoc);
                }
            }

            public override int DocID => _childDoc;

            public override float GetScore()
            {
                return _parentScore;
            }

            public override int Freq => _parentFreq;

            public override int Advance(int childTarget)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(childTarget >= _parentBits.Length || !_parentBits.Get(childTarget));

                //System.out.println("Q.advance childTarget=" + childTarget);
                if (childTarget == NO_MORE_DOCS)
                {
                    //System.out.println("  END");
                    return _childDoc = _parentDoc = NO_MORE_DOCS;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(_childDoc == -1 || childTarget != _parentDoc, "childTarget={0}", childTarget);
                if (_childDoc == -1 || childTarget > _parentDoc)
                {
                    // Advance to new parent:
                    _parentDoc = _parentScorer.Advance(childTarget);
                    ValidateParentDoc();
                    //System.out.println("  advance to parentDoc=" + parentDoc);
                    if (Debugging.AssertsEnabled) Debugging.Assert(_parentDoc > childTarget);
                    if (_parentDoc == NO_MORE_DOCS)
                    {
                        //System.out.println("  END");
                        return _childDoc = NO_MORE_DOCS;
                    }
                    if (_doScores)
                    {
                        _parentScore = _parentScorer.GetScore();
                        _parentFreq = _parentScorer.Freq;
                    }
                    int firstChild = _parentBits.PrevSetBit(_parentDoc - 1);
                    //System.out.println("  firstChild=" + firstChild);
                    childTarget = Math.Max(childTarget, firstChild);
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(childTarget < _parentDoc);

                // Advance within children of current parent:
                _childDoc = childTarget;
                //System.out.println("  " + childDoc);
                if (_acceptDocs != null && !_acceptDocs.Get(_childDoc))
                {
                    NextDoc();
                }
                return _childDoc;
            }

            public override long GetCost()
            {
                return _parentScorer.GetCost();
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _parentQuery.ExtractTerms(terms);
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query parentRewrite = _parentQuery.Rewrite(reader);
            if (parentRewrite != _parentQuery)
            {
                Query rewritten = new ToChildBlockJoinQuery(_parentQuery, parentRewrite, _parentsFilter, _doScores);
                rewritten.Boost = Boost;
                return rewritten;
            }

            return this;
        }

        public override string ToString(string field)
        {
            return "ToChildBlockJoinQuery (" + _parentQuery + ")";
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is ToChildBlockJoinQuery other)
            {
                return _origParentQuery.Equals(other._origParentQuery) &&
                    _parentsFilter.Equals(other._parentsFilter) &&
                    _doScores == other._doScores &&
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
                hashCode = (hashCode * 397) ^ (_origParentQuery != null ? _origParentQuery.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _doScores.GetHashCode();
                hashCode = (hashCode * 397) ^ (_parentsFilter != null ? _parentsFilter.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override object Clone()
        {
            return new ToChildBlockJoinQuery((Query)_origParentQuery.Clone(), _parentsFilter, _doScores);
        }
    }
}