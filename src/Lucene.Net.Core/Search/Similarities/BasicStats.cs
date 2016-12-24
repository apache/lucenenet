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
    /// @lucene.experimental
    /// </summary>
    public class BasicStats : Similarity.SimWeight
    {
        // LUCENENET TODO: These fields should probably be made private/internal since they can be accessed through properties
        protected internal readonly string field;

        /// <summary>
        /// The number of documents. </summary>
        protected internal long NumberOfDocuments_Renamed;

        /// <summary>
        /// The total number of tokens in the field. </summary>
        protected internal long NumberOfFieldTokens_Renamed;

        /// <summary>
        /// The average field length. </summary>
        protected internal float AvgFieldLength_Renamed;

        /// <summary>
        /// The document frequency. </summary>
        protected internal long DocFreq_Renamed;

        /// <summary>
        /// The total number of occurrences of this term across all documents. </summary>
        protected internal long TotalTermFreq_Renamed;

        // -------------------------- Boost-related stuff --------------------------

        /// <summary>
        /// Query's inner boost. </summary>
        protected internal readonly float QueryBoost;

        /// <summary>
        /// Any outer query's boost. </summary>
        protected internal float TopLevelBoost;

        /// <summary>
        /// For most Similarities, the immediate and the top level query boosts are
        /// not handled differently. Hence, this field is just the product of the
        /// other two.
        /// </summary>
        protected internal float TotalBoost_Renamed;

        /// <summary>
        /// Constructor. Sets the query boost. </summary>
        public BasicStats(string field, float queryBoost)
        {
            this.field = field;
            this.QueryBoost = queryBoost;
            this.TotalBoost_Renamed = queryBoost;
        }

        // ------------------------- Getter/setter methods -------------------------

        /// <summary>
        /// Returns the number of documents. </summary>
        public virtual long NumberOfDocuments
        {
            get
            {
                return NumberOfDocuments_Renamed;
            }
            set
            {
                this.NumberOfDocuments_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the total number of tokens in the field. </summary>
        /// <seealso cref= Terms#getSumTotalTermFreq() </seealso>
        public virtual long NumberOfFieldTokens
        {
            get
            {
                return NumberOfFieldTokens_Renamed;
            }
            set
            {
                this.NumberOfFieldTokens_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the average field length. </summary>
        public virtual float AvgFieldLength
        {
            get
            {
                return AvgFieldLength_Renamed;
            }
            set
            {
                this.AvgFieldLength_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the document frequency. </summary>
        public virtual long DocFreq
        {
            get
            {
                return DocFreq_Renamed;
            }
            set
            {
                this.DocFreq_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the total number of occurrences of this term across all documents. </summary>
        public virtual long TotalTermFreq
        {
            get
            {
                return TotalTermFreq_Renamed;
            }
            set
            {
                this.TotalTermFreq_Renamed = value;
            }
        }

        public virtual string Field
        {
            get { return field; }
        }

        // -------------------------- Boost-related stuff --------------------------

        /// <summary>
        /// The square of the raw normalization value. </summary>
        /// <seealso cref= #rawNormalizationValue()  </seealso>
        public override float GetValueForNormalization()
        {
            float rawValue = RawNormalizationValue();
            return rawValue * rawValue;
        }

        /// <summary>
        /// Computes the raw normalization value. this basic implementation returns
        /// the query boost. Subclasses may override this method to include other
        /// factors (such as idf), or to save the value for inclusion in
        /// <seealso cref="#normalize(float, float)"/>, etc.
        /// </summary>
        protected internal virtual float RawNormalizationValue()
        {
            return QueryBoost;
        }

        /// <summary>
        /// No normalization is done. {@code topLevelBoost} is saved in the object,
        /// however.
        /// </summary>
        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            this.TopLevelBoost = topLevelBoost;
            TotalBoost_Renamed = QueryBoost * topLevelBoost;
        }

        /// <summary>
        /// Returns the total boost. </summary>
        public virtual float TotalBoost
        {
            get
            {
                return TotalBoost_Renamed;
            }
        }
    }
}