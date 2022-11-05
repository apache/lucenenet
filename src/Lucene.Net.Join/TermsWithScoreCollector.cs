// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
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

    internal abstract class TermsWithScoreCollector : ICollector
    {
        private const int INITIAL_ARRAY_SIZE = 256;

        private readonly string _field;
        private readonly BytesRefHash _collectedTerms = new BytesRefHash();
        private readonly ScoreMode _scoreMode;

        private Scorer _scorer;
        private float[] _scoreSums = new float[INITIAL_ARRAY_SIZE];

        private protected TermsWithScoreCollector(string field, ScoreMode scoreMode) // LUCENENET: Changed from internal to private protected
        {
            this._field = field;
            this._scoreMode = scoreMode;
        }

        public BytesRefHash CollectedTerms => _collectedTerms;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual float[] ScoresPerTerm => _scoreSums;

        public virtual void SetScorer(Scorer scorer)
        {
            _scorer = scorer;
        }

        // LUCENENET specific - we need to implement these here, since our abstract base class
        // is now an interface.
        /// <summary>
        /// Called once for every document matching a query, with the unbased document
        /// number.
        /// <para/>Note: The collection of the current segment can be terminated by throwing
        /// a <see cref="CollectionTerminatedException"/>. In this case, the last docs of the
        /// current <see cref="AtomicReaderContext"/> will be skipped and <see cref="IndexSearcher"/>
        /// will swallow the exception and continue collection with the next leaf.
        /// <para/>
        /// Note: this is called in an inner search loop. For good search performance,
        /// implementations of this method should not call <see cref="IndexSearcher.Doc(int)"/> or
        /// <see cref="Lucene.Net.Index.IndexReader.Document(int)"/> on every hit.
        /// Doing so can slow searches by an order of magnitude or more.
        /// </summary>
        public abstract void Collect(int doc);

        /// <summary>
        /// Called before collecting from each <see cref="AtomicReaderContext"/>. All doc ids in
        /// <see cref="Collect(int)"/> will correspond to <see cref="Index.IndexReaderContext.Reader"/>.
        ///
        /// Add <see cref="AtomicReaderContext.DocBase"/> to the current <see cref="Index.IndexReaderContext.Reader"/>'s
        /// internal document id to re-base ids in <see cref="Collect(int)"/>.
        /// </summary>
        /// <param name="context">next atomic reader context </param>
        public abstract void SetNextReader(AtomicReaderContext context);


        public virtual bool AcceptsDocsOutOfOrder => true;

        /// <summary>
        /// Chooses the right <see cref="TermsWithScoreCollector"/> implementation.
        /// </summary>
        /// <param name="field">The field to collect terms for.</param>
        /// <param name="multipleValuesPerDocument">Whether the field to collect terms for has multiple values per document.</param>
        /// <param name="scoreMode">See <see cref="ScoreMode"/></param>
        /// <returns>A <see cref="TermsWithScoreCollector"/> instance</returns>
        internal static TermsWithScoreCollector Create(string field, bool multipleValuesPerDocument, ScoreMode scoreMode)
        {
            if (multipleValuesPerDocument)
            {
                switch (scoreMode)
                {
                    case ScoreMode.Avg:
                        return new MV.Avg(field);
                    default:
                        return new MV(field, scoreMode);
                }
            }

            switch (scoreMode)
            {
                case ScoreMode.Avg:
                    return new SV.Avg(field);
                default:
                    return new SV(field, scoreMode);
            }
        }

        // impl that works with single value per document
        internal class SV : TermsWithScoreCollector
        {
            private readonly BytesRef _spare = new BytesRef();
            private BinaryDocValues _fromDocTerms;

            internal SV(string field, ScoreMode scoreMode) 
                : base(field, scoreMode)
            {
            }
            
            public override void Collect(int doc)
            {
                _fromDocTerms.Get(doc, _spare);
                int ord = _collectedTerms.Add(_spare);
                if (ord < 0)
                {
                    ord = -ord - 1;
                }
                else
                {
                    if (ord >= _scoreSums.Length)
                    {
                        _scoreSums = ArrayUtil.Grow(_scoreSums);
                    }
                }

                float current = _scorer.GetScore();
                float existing = _scoreSums[ord];
                if (existing.CompareTo(0.0f) == 0)
                {
                    _scoreSums[ord] = current;
                }
                else
                {
                    switch (_scoreMode)
                    {
                        case ScoreMode.Total:
                            _scoreSums[ord] = _scoreSums[ord] + current;
                            break;
                        case ScoreMode.Max:
                            if (current > existing)
                            {
                                _scoreSums[ord] = current;
                            }
                            break;
                    }
                }
            }
            
            public override void SetNextReader(AtomicReaderContext context)
            {
                _fromDocTerms = FieldCache.DEFAULT.GetTerms(context.AtomicReader, _field, false);
            }

            internal class Avg : SV
            {
                private int[] _scoreCounts = new int[INITIAL_ARRAY_SIZE];

                internal Avg(string field) 
                    : base(field, ScoreMode.Avg)
                {
                }
                
                public override void Collect(int doc)
                {
                    _fromDocTerms.Get(doc, _spare);
                    int ord = _collectedTerms.Add(_spare);
                    if (ord < 0)
                    {
                        ord = -ord - 1;
                    }
                    else
                    {
                        if (ord >= _scoreSums.Length)
                        {
                            _scoreSums = ArrayUtil.Grow(_scoreSums);
                            _scoreCounts = ArrayUtil.Grow(_scoreCounts);
                        }
                    }

                    float current = _scorer.GetScore();
                    float existing = _scoreSums[ord];
                    if (existing.CompareTo(0.0f) == 0)
                    {
                        _scoreSums[ord] = current;
                        _scoreCounts[ord] = 1;
                    }
                    else
                    {
                        _scoreSums[ord] = _scoreSums[ord] + current;
                        _scoreCounts[ord]++;
                    }
                }

                public override float[] ScoresPerTerm
                {
                    get
                    {
                        if (_scoreCounts != null)
                        {
                            for (int i = 0; i < _scoreCounts.Length; i++)
                            {
                                _scoreSums[i] = _scoreSums[i] / _scoreCounts[i];
                            }
                            _scoreCounts = null;
                        }
                        return _scoreSums;
                    }
                }
            }
        }

        // impl that works with multiple values per document
        internal class MV : TermsWithScoreCollector
        {
            private SortedSetDocValues _fromDocTermOrds;
            private readonly BytesRef _scratch = new BytesRef();

            internal MV(string field, ScoreMode scoreMode) 
                : base(field, scoreMode)
            {
            }
            
            public override void Collect(int doc)
            {
                _fromDocTermOrds.SetDocument(doc);
                long ord;
                while ((ord = _fromDocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    _fromDocTermOrds.LookupOrd(ord, _scratch);

                    int termId = _collectedTerms.Add(_scratch);
                    if (termId < 0)
                    {
                        termId = -termId - 1;
                    }
                    else
                    {
                        if (termId >= _scoreSums.Length)
                        {
                            _scoreSums = ArrayUtil.Grow(_scoreSums);
                        }
                    }

                    switch (_scoreMode)
                    {
                        case ScoreMode.Total:
                            _scoreSums[termId] += _scorer.GetScore();
                            break;
                        case ScoreMode.Max:
                            _scoreSums[termId] = Math.Max(_scoreSums[termId], _scorer.GetScore());
                            break;
                    }
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                _fromDocTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, _field);
            }

            internal class Avg : MV
            {
                private int[] _scoreCounts = new int[INITIAL_ARRAY_SIZE];

                internal Avg(string field) 
                    : base(field, ScoreMode.Avg)
                {
                }
                
                public override void Collect(int doc)
                {
                    _fromDocTermOrds.SetDocument(doc);
                    long ord;
                    while ((ord = _fromDocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        _fromDocTermOrds.LookupOrd(ord, _scratch);

                        int termId = _collectedTerms.Add(_scratch);
                        if (termId < 0)
                        {
                            termId = -termId - 1;
                        }
                        else
                        {
                            if (termId >= _scoreSums.Length)
                            {
                                _scoreSums = ArrayUtil.Grow(_scoreSums);
                                _scoreCounts = ArrayUtil.Grow(_scoreCounts);
                            }
                        }

                        _scoreSums[termId] += _scorer.GetScore();
                        _scoreCounts[termId]++;
                    }
                }

                public override float[] ScoresPerTerm
                {
                    get
                    {
                        if (_scoreCounts != null)
                        {
                            for (int i = 0; i < _scoreCounts.Length; i++)
                            {
                                _scoreSums[i] = _scoreSums[i] / _scoreCounts[i];
                            }
                            _scoreCounts = null;
                        }
                        return _scoreSums;
                    }
                }
            }
        }
    }
}