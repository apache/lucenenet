using System;
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


    internal abstract class TermsWithScoreCollector : Collector
    {
        private const int InitialArraySize = 256;

        private readonly string _field;
        private readonly BytesRefHash _collectedTerms = new BytesRefHash();
        private readonly ScoreMode _scoreMode;

        private Scorer _scorer;
        private float[] _scoreSums = new float[InitialArraySize];

        internal TermsWithScoreCollector(string field, ScoreMode scoreMode)
        {
            this._field = field;
            this._scoreMode = scoreMode;
        }

        public BytesRefHash CollectedTerms
        {
            get
            {
                return _collectedTerms;
            }
        }

        public virtual float[] ScoresPerTerm // LUCENENET TODO: Make GetScoresPerTerm() (array)
        {
            get
            {
                return _scoreSums;
            }
        }

        public override void SetScorer(Scorer scorer)
        {
            _scorer = scorer;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        /// <summary>
        /// Chooses the right <seealso cref="TermsWithScoreCollector"/> implementation.
        /// </summary>
        /// <param name="field">The field to collect terms for.</param>
        /// <param name="multipleValuesPerDocument">Whether the field to collect terms for has multiple values per document.</param>
        /// <returns>A <see cref="TermsWithScoreCollector"/> instance</returns>
        internal static TermsWithScoreCollector Create(string field, bool multipleValuesPerDocument, ScoreMode scoreMode)
        {
            if (multipleValuesPerDocument)
            {
                switch (scoreMode)
                {
                    case ScoreMode.Avg:
                        return new Mv.Avg(field);
                    default:
                        return new Mv(field, scoreMode);
                }
            }

            switch (scoreMode)
            {
                case ScoreMode.Avg:
                    return new Sv.Avg(field);
                default:
                    return new Sv(field, scoreMode);
            }
        }

        // impl that works with single value per document
        internal class Sv : TermsWithScoreCollector
        {
            private readonly BytesRef _spare = new BytesRef();
            private BinaryDocValues _fromDocTerms;

            internal Sv(string field, ScoreMode scoreMode) : base(field, scoreMode)
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

                float current = _scorer.Score();
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

            public override bool AcceptsDocsOutOfOrder
            {
                get { return base.AcceptsDocsOutOfOrder; }
            }

            internal class Avg : Sv
            {
                private int[] _scoreCounts = new int[InitialArraySize];

                internal Avg(string field) : base(field, ScoreMode.Avg)
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

                    float current = _scorer.Score();
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
        internal class Mv : TermsWithScoreCollector
        {
            private SortedSetDocValues _fromDocTermOrds;
            private readonly BytesRef _scratch = new BytesRef();

            internal Mv(string field, ScoreMode scoreMode) : base(field, scoreMode)
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
                            _scoreSums[termId] += _scorer.Score();
                            break;
                        case ScoreMode.Max:
                            _scoreSums[termId] = Math.Max(_scoreSums[termId], _scorer.Score());
                            break;
                    }
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                _fromDocTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, _field);
            }

            internal class Avg : Mv
            {
                private int[] _scoreCounts = new int[InitialArraySize];

                internal Avg(string field) : base(field, ScoreMode.Avg)
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

                        _scoreSums[termId] += _scorer.Score();
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