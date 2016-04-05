using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Facet;

namespace Lucene.Net.Facet.Taxonomy
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


    using MatchingDocs = FacetsCollector.MatchingDocs;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;
    using DoubleDocValues = Lucene.Net.Queries.Function.DocValues.DoubleDocValues;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Scorer = Lucene.Net.Search.Scorer;
    using Weight = Lucene.Net.Search.Weight;
    using IntsRef = Lucene.Net.Util.IntsRef;

    /// <summary>
    /// Aggregates sum of values from {@link
    ///  FunctionValues#doubleVal}, for each facet label.
    /// 
    ///  @lucene.experimental 
    /// </summary>
    public class TaxonomyFacetSumValueSource : FloatTaxonomyFacets
    {
        private readonly OrdinalsReader ordinalsReader;

        /// <summary>
        /// Aggreggates float facet values from the provided
        ///  <seealso cref="ValueSource"/>, pulling ordinals using {@link
        ///  DocValuesOrdinalsReader} against the default indexed
        ///  facet field {@link
        ///  FacetsConfig#DEFAULT_INDEX_FIELD_NAME}. 
        /// </summary>
        public TaxonomyFacetSumValueSource(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc, ValueSource valueSource)
            : this(new DocValuesOrdinalsReader(FacetsConfig.DEFAULT_INDEX_FIELD_NAME), taxoReader, config, fc, valueSource)
        {
        }

        /// <summary>
        /// Aggreggates float facet values from the provided
        ///  <seealso cref="ValueSource"/>, and pulls ordinals from the
        ///  provided <seealso cref="OrdinalsReader"/>. 
        /// </summary>
        public TaxonomyFacetSumValueSource(OrdinalsReader ordinalsReader, TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc, ValueSource valueSource)
            : base(ordinalsReader.IndexFieldName, taxoReader, config)
        {
            this.ordinalsReader = ordinalsReader;
            SumValues(fc.GetMatchingDocs, fc.KeepScores, valueSource);
        }

        private sealed class FakeScorer : Scorer
        {
            internal float score_Renamed;
            internal int docID_Renamed;
            internal FakeScorer()
                : base(null)
            {
            }
            public override float Score()
            {
                return score_Renamed;
            }
            public override int Freq()
            {
                throw new System.NotSupportedException();
            }
            public override int DocID()
            {
                return docID_Renamed;
            }
            public override int NextDoc()
            {
                throw new System.NotSupportedException();
            }
            public override int Advance(int target)
            {
                throw new System.NotSupportedException();
            }
            public override long Cost()
            {
                return 0;
            }
            public override Weight Weight
            {
                get
                {
                    throw new System.NotSupportedException();
                }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    throw new System.NotSupportedException();
                }
            }
        }

        private void SumValues(IList<MatchingDocs> matchingDocs, bool keepScores, ValueSource valueSource)
        {
            FakeScorer scorer = new FakeScorer();
            IDictionary context = new Dictionary<string, Scorer>();
            if (keepScores)
            {
                context["scorer"] = scorer;
            }
            IntsRef scratch = new IntsRef();
            foreach (MatchingDocs hits in matchingDocs)
            {
                OrdinalsReader.OrdinalsSegmentReader ords = ordinalsReader.GetReader(hits.context);

                int scoresIdx = 0;
                float[] scores = hits.scores;

                FunctionValues functionValues = valueSource.GetValues(context, hits.context);
                DocIdSetIterator docs = hits.bits.GetIterator();

                int doc;
                while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    ords.Get(doc, scratch);
                    if (keepScores)
                    {
                        scorer.docID_Renamed = doc;
                        scorer.score_Renamed = scores[scoresIdx++];
                    }
                    float value = (float)functionValues.DoubleVal(doc);
                    for (int i = 0; i < scratch.Length; i++)
                    {
                        values[scratch.Ints[i]] += value;
                    }
                }
            }

            Rollup();
        }

        /// <summary>
        /// <seealso cref="ValueSource"/> that returns the score for each
        ///  hit; use this to aggregate the sum of all hit scores
        ///  for each facet label.  
        /// </summary>
        public class ScoreValueSource : ValueSource
        {

            /// <summary>
            /// Sole constructor. </summary>
            public ScoreValueSource()
            {
            }

            public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
            {
                Scorer scorer = (Scorer)context["scorer"];
                if (scorer == null)
                {
                    throw new ThreadStateException("scores are missing; be sure to pass keepScores=true to FacetsCollector");
                }
                return new DoubleDocValuesAnonymousInnerClassHelper(this, scorer);
            }

            private class DoubleDocValuesAnonymousInnerClassHelper : DoubleDocValues
            {
                private readonly ScoreValueSource outerInstance;

                private Scorer scorer;

                public DoubleDocValuesAnonymousInnerClassHelper(ScoreValueSource outerInstance, Scorer scorer)
                    : base(outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.scorer = scorer;

                }

                public override double DoubleVal(int document)
                {
                    try
                    {
                        return scorer.Score();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            public override bool Equals(object o)
            {
                if (ReferenceEquals(null, o)) return false;
                if (ReferenceEquals(this, o)) return true;
                if (o.GetType() != this.GetType()) return false;
                return Equals((ScoreValueSource) o);
            }

            protected bool Equals(ScoreValueSource other)
            {
                return Equals(this, other);
            }

            public override int GetHashCode()
            {
                return hcode;
            }

            private static readonly int hcode = typeof(DoubleDocValuesAnonymousInnerClassHelper).GetHashCode();

            


            public override string Description
            {
                get
                {
                    return "score()";
                }

            }
        }

    }

}