namespace Lucene.Net.Search.Similarities
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
    /// Stores all statistics commonly used ranking methods.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BasicStats : Similarity.SimWeight
    {
        private readonly string field;

        /// <summary>
        /// The number of documents. </summary>
        protected long m_numberOfDocuments;

        /// <summary>
        /// The total number of tokens in the field. </summary>
        protected long m_numberOfFieldTokens;

        /// <summary>
        /// The average field length. </summary>
        protected internal float m_avgFieldLength;

        /// <summary>
        /// The document frequency. </summary>
        protected long m_docFreq;

        /// <summary>
        /// The total number of occurrences of this term across all documents. </summary>
        protected long m_totalTermFreq;

        // -------------------------- Boost-related stuff --------------------------

        /// <summary>
        /// Query's inner boost. </summary>
        protected readonly float m_queryBoost;

        /// <summary>
        /// Any outer query's boost. </summary>
        protected float m_topLevelBoost;

        /// <summary>
        /// For most Similarities, the immediate and the top level query boosts are
        /// not handled differently. Hence, this field is just the product of the
        /// other two.
        /// </summary>
        protected float m_totalBoost;

        /// <summary>
        /// Constructor. Sets the query boost. </summary>
        public BasicStats(string field, float queryBoost)
        {
            this.field = field;
            this.m_queryBoost = queryBoost;
            this.m_totalBoost = queryBoost;
        }

        // ------------------------- Getter/setter methods -------------------------

        /// <summary>
        /// Gets or Sets the number of documents. </summary>
        public virtual long NumberOfDocuments
        {
            get => m_numberOfDocuments;
            set => this.m_numberOfDocuments = value;
        }

        /// <summary>
        /// Returns the total number of tokens in the field. </summary>
        /// <seealso cref="Index.Terms.SumTotalTermFreq"/>
        public virtual long NumberOfFieldTokens
        {
            get => m_numberOfFieldTokens;
            set => this.m_numberOfFieldTokens = value;
        }

        /// <summary>
        /// Returns the average field length. </summary>
        public virtual float AvgFieldLength
        {
            get => m_avgFieldLength;
            set => this.m_avgFieldLength = value;
        }

        /// <summary>
        /// Returns the document frequency. </summary>
        public virtual long DocFreq
        {
            get => m_docFreq;
            set => this.m_docFreq = value;
        }

        /// <summary>
        /// Returns the total number of occurrences of this term across all documents. </summary>
        public virtual long TotalTermFreq
        {
            get => m_totalTermFreq;
            set => this.m_totalTermFreq = value;
        }

        /// <summary>
        /// The field.
        /// </summary>
        // LUCENENET specific
        public string Field => field;

        // -------------------------- Boost-related stuff --------------------------

        /// <summary>
        /// The square of the raw normalization value. </summary>
        /// <seealso cref="RawNormalizationValue()"/>
        public override float GetValueForNormalization()
        {
            float rawValue = RawNormalizationValue();
            return rawValue * rawValue;
        }

        /// <summary>
        /// Computes the raw normalization value. This basic implementation returns
        /// the query boost. Subclasses may override this method to include other
        /// factors (such as idf), or to save the value for inclusion in
        /// <seealso cref="Normalize(float, float)"/>, etc.
        /// </summary>
        protected internal virtual float RawNormalizationValue()
        {
            return m_queryBoost;
        }

        /// <summary>
        /// No normalization is done. <paramref name="topLevelBoost"/> is saved in the object,
        /// however.
        /// </summary>
        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            this.m_topLevelBoost = topLevelBoost;
            m_totalBoost = m_queryBoost * topLevelBoost;
        }

        /// <summary>
        /// Returns the total boost. </summary>
        public virtual float TotalBoost => m_totalBoost;
    }
}