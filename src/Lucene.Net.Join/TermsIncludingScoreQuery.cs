// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    internal class TermsIncludingScoreQuery : Query
    {
        private readonly string _field;
        private readonly bool _multipleValuesPerDocument;
        private readonly BytesRefHash _terms;
        private readonly float[] _scores;
        private readonly int[] _ords;
        private readonly Query _originalQuery;
        private readonly Query _unwrittenOriginalQuery;

        internal TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash terms,
            float[] scores, Query originalQuery)
        {
            _field = field;
            _multipleValuesPerDocument = multipleValuesPerDocument;
            _terms = terms;
            _scores = scores;
            _originalQuery = originalQuery;
            _ords = terms.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            _unwrittenOriginalQuery = originalQuery;
        }

        private TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash terms,
            float[] scores, int[] ords, Query originalQuery, Query unwrittenOriginalQuery)
        {
            _field = field;
            _multipleValuesPerDocument = multipleValuesPerDocument;
            _terms = terms;
            _scores = scores;
            _originalQuery = originalQuery;
            _ords = ords;
            _unwrittenOriginalQuery = unwrittenOriginalQuery;
        }

        public override string ToString(string @string)
        {
            return string.Format("TermsIncludingScoreQuery{{field={0};originalQuery={1}}}", _field,
                _unwrittenOriginalQuery);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _originalQuery.ExtractTerms(terms);
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query originalQueryRewrite = _originalQuery.Rewrite(reader);
            if (originalQueryRewrite != _originalQuery)
            {
                Query rewritten = new TermsIncludingScoreQuery(_field, _multipleValuesPerDocument, _terms, _scores,
                    _ords, originalQueryRewrite, _originalQuery);
                rewritten.Boost = Boost;
                return rewritten;
            }

            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!base.Equals(obj)) return false;
            if (obj.GetType() != GetType()) return false;

            var other = (TermsIncludingScoreQuery)obj;
            if (!_field.Equals(other._field, StringComparison.Ordinal))
            {
                return false;
            }
            if (!_unwrittenOriginalQuery.Equals(other._unwrittenOriginalQuery))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ (_field != null ? _field.GetHashCode() : 0);
                hashCode = (hashCode*397) ^
                           (_unwrittenOriginalQuery != null ? _unwrittenOriginalQuery.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight originalWeight = _originalQuery.CreateWeight(searcher);
            return new WeightAnonymousClass(this, originalWeight);
        }

        private sealed class WeightAnonymousClass : Weight
        {
            private readonly TermsIncludingScoreQuery outerInstance;

            private readonly Weight originalWeight;

            public WeightAnonymousClass(TermsIncludingScoreQuery outerInstance, Weight originalWeight)
            {
                this.outerInstance = outerInstance;
                this.originalWeight = originalWeight;
            }


            private TermsEnum segmentTermsEnum;
            
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                SVInnerScorer scorer = (SVInnerScorer) GetBulkScorer(context, false, null);
                if (scorer != null)
                {
                    return scorer.Explain(doc);
                }
                return new ComplexExplanation(false, 0.0f, "Not a match");
            }

            public override bool ScoresDocsOutOfOrder =>
                // We have optimized impls below if we are allowed
                // to score out-of-order:
                true;

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                return originalWeight.GetValueForNormalization() * outerInstance.Boost*outerInstance.Boost;
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                originalWeight.Normalize(norm, topLevelBoost*outerInstance.Boost);
            }
            
            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                Terms terms = context.AtomicReader.GetTerms(outerInstance._field);
                if (terms is null)
                {
                    return null;
                }

                // what is the runtime...seems ok?
                long cost = context.AtomicReader.MaxDoc * terms.Count;

                segmentTermsEnum = terms.GetEnumerator(segmentTermsEnum);
                if (outerInstance._multipleValuesPerDocument)
                {
                    return new MVInOrderScorer(outerInstance, this, acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc, cost);
                }

                return new SVInOrderScorer(outerInstance, this, acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc, cost);
            }
            
            public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
            {
                if (scoreDocsInOrder)
                {
                    return base.GetBulkScorer(context, scoreDocsInOrder, acceptDocs);
                }

                Terms terms = context.AtomicReader.GetTerms(outerInstance._field);
                if (terms is null)
                {
                    return null;
                }
                // what is the runtime...seems ok?
                //long cost = context.AtomicReader.MaxDoc * terms.Count; // LUCENENET: IDE0059: Remove unnecessary value assignment

                segmentTermsEnum = terms.GetEnumerator(segmentTermsEnum);
                // Optimized impls that take advantage of docs
                // being allowed to be out of order:
                if (outerInstance._multipleValuesPerDocument)
                {
                    return new MVInnerScorer(outerInstance, /*this, // LUCENENET: Never read */
                        acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc /*, cost // LUCENENET: Never read */);
                }

                return new SVInnerScorer(outerInstance, /*this, // LUCENENET: Never read */
                    acceptDocs, segmentTermsEnum /*, cost // LUCENENET: Never read */);
            }
        }

        // This impl assumes that the 'join' values are used uniquely per doc per field. Used for one to many relations.
        internal class SVInnerScorer : BulkScorer
        {
            private readonly TermsIncludingScoreQuery outerInstance;

            private readonly BytesRef _spare = new BytesRef();
            private readonly IBits _acceptDocs;
            private readonly TermsEnum _termsEnum;
            //private readonly long _cost; // LUCENENET: Never read

            private int _upto;
            internal DocsEnum docsEnum;
            private DocsEnum _reuse;
            private int _scoreUpto;
            private int _doc;

            internal SVInnerScorer(TermsIncludingScoreQuery outerInstance, /* Weight weight, // LUCENENET: Never read */
                IBits acceptDocs, TermsEnum termsEnum /*, long cost // LUCENENET: Never read */)
            {
                this.outerInstance = outerInstance;
                _acceptDocs = acceptDocs;
                _termsEnum = termsEnum;
                //_cost = cost; // LUCENENET: Never read
                _doc = -1;
            }
            
            public override bool Score(ICollector collector, int max)
            {
                FakeScorer fakeScorer = new FakeScorer();
                collector.SetScorer(fakeScorer);
                if (_doc == -1)
                {
                    _doc = NextDocOutOfOrder();
                }
                while (_doc < max)
                {
                    fakeScorer.doc = _doc;
                    fakeScorer._score = outerInstance._scores[outerInstance._ords[_scoreUpto]];
                    collector.Collect(_doc);
                    _doc = NextDocOutOfOrder();
                }

                return _doc != DocIdSetIterator.NO_MORE_DOCS;
            }

            private int NextDocOutOfOrder()
            {
                while (true)
                {
                    if (docsEnum != null)
                    {
                        int docId = DocsEnumNextDoc();
                        if (docId == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            docsEnum = null;
                        }
                        else
                        {
                            return _doc = docId;
                        }
                    }

                    if (_upto == outerInstance._terms.Count)
                    {
                        return _doc = DocIdSetIterator.NO_MORE_DOCS;
                    }

                    _scoreUpto = _upto;
                    if (_termsEnum.SeekExact(outerInstance._terms.Get(outerInstance._ords[_upto++], _spare)))
                    {
                        docsEnum = _reuse = _termsEnum.Docs(_acceptDocs, _reuse, DocsFlags.NONE);
                    }
                }
            }
            
            protected virtual int DocsEnumNextDoc()
            {
                return docsEnum.NextDoc();
            }
            
            internal Explanation Explain(int target) // LUCENENET NOTE: changed accessibility from private to internal
            {
                int docId;
                do
                {
                    docId = NextDocOutOfOrder();
                    if (docId < target)
                    {
                        int tempDocId = docsEnum.Advance(target);
                        if (tempDocId == target)
                        {
                            //docId = tempDocId; // LUCENENET: IDE0059: Remove unnecessary value assignment
                            break;
                        }
                    }
                    else if (docId == target)
                    {
                        break;
                    }
                    docsEnum = null; // goto the next ord.
                } while (docId != DocIdSetIterator.NO_MORE_DOCS);

                return new ComplexExplanation(true, outerInstance._scores[outerInstance._ords[_scoreUpto]],
                    "Score based on join value " + _termsEnum.Term.Utf8ToString());
            }
        }

        // This impl that tracks whether a docid has already been emitted. This check makes sure that docs aren't emitted
        // twice for different join values. This means that the first encountered join value determines the score of a document
        // even if other join values yield a higher score.
        internal class MVInnerScorer : SVInnerScorer
        {
            internal readonly FixedBitSet alreadyEmittedDocs;

            internal MVInnerScorer(TermsIncludingScoreQuery outerInstance, /* Weight weight, // LUCENENET: Never read */
                IBits acceptDocs, TermsEnum termsEnum, int maxDoc /*, long cost // LUCENENET: Never read */) 
                : base(outerInstance, /*weight, // LUCENENET: Never read */
                      acceptDocs, termsEnum /*, cost // LUCENENET: Never read */)
            {
                alreadyEmittedDocs = new FixedBitSet(maxDoc);
            }
            
            protected override int DocsEnumNextDoc()
            {
                while (true)
                {
                    int docId = docsEnum.NextDoc();
                    if (docId == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        return docId;
                    }
                    if (!alreadyEmittedDocs.GetAndSet(docId))
                    {
                        return docId; //if it wasn't previously set, return it
                    }
                }
            }
        }

        internal class SVInOrderScorer : Scorer
        {
            protected readonly TermsIncludingScoreQuery m_outerInstance;


            internal readonly DocIdSetIterator matchingDocsIterator;
            internal readonly float[] scores;
            internal readonly long cost;

            internal int currentDoc = -1;
            
            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
            [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Internal class")]
            internal SVInOrderScorer(TermsIncludingScoreQuery outerInstance, Weight weight, IBits acceptDocs,
                TermsEnum termsEnum, int maxDoc, long cost) 
                : base(weight)
            {
                this.m_outerInstance = outerInstance;
                FixedBitSet matchingDocs = new FixedBitSet(maxDoc);
                scores = new float[maxDoc];
                FillDocsAndScores(matchingDocs, acceptDocs, termsEnum);
                matchingDocsIterator = matchingDocs.GetIterator();
                this.cost = cost;
            }
            
            protected virtual void FillDocsAndScores(FixedBitSet matchingDocs, IBits acceptDocs,
                TermsEnum termsEnum)
            {
                BytesRef spare = new BytesRef();
                DocsEnum docsEnum = null;
                for (int i = 0; i < m_outerInstance._terms.Count; i++)
                {
                    if (termsEnum.SeekExact(m_outerInstance._terms.Get(m_outerInstance._ords[i], spare)))
                    {
                        docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsFlags.NONE);
                        float score = m_outerInstance._scores[m_outerInstance._ords[i]];
                        for (int doc = docsEnum.NextDoc();
                            doc != NO_MORE_DOCS;
                            doc = docsEnum.NextDoc())
                        {
                            matchingDocs.Set(doc);
                            // In the case the same doc is also related to a another doc, a score might be overwritten. I think this
                            // can only happen in a many-to-many relation
                            scores[doc] = score;
                        }
                    }
                }
            }
            
            public override float GetScore()
            {
                return scores[currentDoc];
            }
            
            public override int Freq => 1;

            public override int DocID => currentDoc;

            public override int NextDoc()
            {
                return currentDoc = matchingDocsIterator.NextDoc();
            }
            
            public override int Advance(int target)
            {
                return currentDoc = matchingDocsIterator.Advance(target);
            }

            public override long GetCost()
            {
                return cost;
            }
        }

        // This scorer deals with the fact that a document can have more than one score from multiple related documents.
        internal class MVInOrderScorer : SVInOrderScorer
        {
            internal MVInOrderScorer(TermsIncludingScoreQuery outerInstance, Weight weight, IBits acceptDocs,
                TermsEnum termsEnum, int maxDoc, long cost)
                : base(outerInstance, weight, acceptDocs, termsEnum, maxDoc, cost)
            {
            }
            
            protected override void FillDocsAndScores(FixedBitSet matchingDocs, IBits acceptDocs,
                TermsEnum termsEnum)
            {
                BytesRef spare = new BytesRef();
                DocsEnum docsEnum = null;
                for (int i = 0; i < m_outerInstance._terms.Count; i++)
                {
                    if (termsEnum.SeekExact(m_outerInstance._terms.Get(m_outerInstance._ords[i], spare)))
                    {
                        docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsFlags.NONE);
                        float score = m_outerInstance._scores[m_outerInstance._ords[i]];
                        for (int doc = docsEnum.NextDoc();
                            doc != NO_MORE_DOCS;
                            doc = docsEnum.NextDoc())
                        {
                            // I prefer this:
                            /*if (scores[doc] < score) {
                              scores[doc] = score;
                              matchingDocs.set(doc);
                            }*/
                            // But this behaves the same as MVInnerScorer and only then the tests will pass:
                            if (!matchingDocs.Get(doc))
                            {
                                scores[doc] = score;
                                matchingDocs.Set(doc);
                            }
                        }
                    }
                }
            }
        }
    }
}