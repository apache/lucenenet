/*
 * Created on 25-Jan-2006
 */
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Lucene.Net.Queries.Mlt
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
    /// A simple wrapper for <see cref="MoreLikeThis"/> for use in scenarios where a <see cref="Query"/> object is required eg
    /// in custom QueryParser extensions. At query.Rewrite() time the reader is used to construct the
    /// actual <see cref="MoreLikeThis"/> object and obtain the real <see cref="Query"/> object.
    /// </summary>
    public class MoreLikeThisQuery : Query
    {
        private string likeText;
        private string[] moreLikeFields;
        private Analyzer analyzer;
        private readonly string fieldName;
        private float percentTermsToMatch = 0.3f;
        private int minTermFrequency = 1;
        private int maxQueryTerms = 5;
        private ISet<string> stopWords = null;
        private int minDocFreq = -1;

        /// <param name="moreLikeFields"> fields used for similarity measure </param>
        public MoreLikeThisQuery(string likeText, string[] moreLikeFields, Analyzer analyzer, string fieldName)
        {
            this.LikeText = likeText;
            this.MoreLikeFields = moreLikeFields;
            this.Analyzer = analyzer;
            this.fieldName = fieldName;
        }

        public override Query Rewrite(IndexReader reader)
        {
            var mlt = new MoreLikeThis(reader)
            {
                FieldNames = moreLikeFields,
                Analyzer = analyzer,
                MinTermFreq = minTermFrequency
            };

            if (MinDocFreq >= 0)
            {
                mlt.MinDocFreq = minDocFreq;
            }
            mlt.MaxQueryTerms = maxQueryTerms;
            mlt.StopWords = stopWords;
            var bq = (BooleanQuery)mlt.Like(new StringReader(likeText), fieldName);
            var clauses = bq.GetClauses();
            //make at least half the terms match
            bq.MinimumNumberShouldMatch = (int)(clauses.Length * percentTermsToMatch);
            return bq;
        }

        /// <summary>
        /// <see cref="Query.ToString(string)"/>
        /// </summary>
        public override string ToString(string field)
        {
            return "like:" + LikeText;
        }

        public virtual float PercentTermsToMatch
        {
            get { return percentTermsToMatch; }
            set { percentTermsToMatch = value; }
        }

        public virtual Analyzer Analyzer
        {
            get { return analyzer; }
            set { analyzer = value; }
        }

        public virtual string LikeText
        {
            get { return likeText; }
            set { likeText = value; }
        }

        public virtual int MaxQueryTerms
        {
            get { return maxQueryTerms; }
            set { maxQueryTerms = value; }
        }

        public virtual int MinTermFrequency
        {
            get { return minTermFrequency; }
            set { minTermFrequency = value; }
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual string[] MoreLikeFields
        {
            get { return moreLikeFields; }
            set { moreLikeFields = value; }
        }

        public virtual ISet<string> StopWords
        {
            get { return stopWords; }
            set { stopWords = value; }
        }

        public virtual int MinDocFreq
        {
            get { return minDocFreq; }
            set { minDocFreq = value; }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((analyzer == null) ? 0 : analyzer.GetHashCode());
            result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
            result = prime * result + ((likeText == null) ? 0 : likeText.GetHashCode());
            result = prime * result + maxQueryTerms;
            result = prime * result + minDocFreq;
            result = prime * result + minTermFrequency;
            result = prime * result + Arrays.GetHashCode(moreLikeFields);
            result = prime * result + Number.SingleToInt32Bits(percentTermsToMatch);
            // LUCENENET: wrap in Equatable to compare set contents
            result = prime * result + ((stopWords == null) ? 0 : Equatable.Wrap(stopWords).GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            var other = (MoreLikeThisQuery)obj;
            if (analyzer == null)
            {
                if (other.analyzer != null)
                {
                    return false;
                }
            }
            else if (!analyzer.Equals(other.analyzer))
            {
                return false;
            }
            if (fieldName == null)
            {
                if (other.fieldName != null)
                {
                    return false;
                }
            }
            else if (!fieldName.Equals(other.fieldName))
            {
                return false;
            }
            if (likeText == null)
            {
                if (other.likeText != null)
                {
                    return false;
                }
            }
            else if (!likeText.Equals(other.likeText))
            {
                return false;
            }
            if (maxQueryTerms != other.maxQueryTerms)
            {
                return false;
            }
            if (minDocFreq != other.minDocFreq)
            {
                return false;
            }
            if (minTermFrequency != other.minTermFrequency)
            {
                return false;
            }
            if (!Arrays.Equals(moreLikeFields, other.moreLikeFields))
            {
                return false;
            }
            if (Number.SingleToInt32Bits(percentTermsToMatch) != Number.SingleToInt32Bits(other.percentTermsToMatch))
            {
                return false;
            }
            if (stopWords == null)
            {
                if (other.stopWords != null)
                {
                    return false;
                }
            }
            // LUCENENET: wrap in Equatable to compare set contents
            else if (!Equatable.Wrap(stopWords).Equals(other.stopWords))
            {
                return false;
            }
            return true;
        }
    }
}