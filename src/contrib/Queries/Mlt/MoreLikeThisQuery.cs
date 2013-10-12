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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using System.IO;
using Lucene.Net.Support;

namespace Lucene.Net.Search.Mlt
{
    /*<summary>
 * A simple wrapper for MoreLikeThis for use in scenarios where a Query object is required eg
 * in custom QueryParser extensions. At query.rewrite() time the reader is used to construct the
 * actual MoreLikeThis object and obtain the real Query object.
     * </summary>
 */
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


        /*<summary></summary>
         * <param name="moreLikeFields"></param>
         * <param name="likeText"></param>
         * <param name="analyzer"></param>
         */
        public MoreLikeThisQuery(string likeText, string[] moreLikeFields, Analyzer analyzer)
        {
            this.likeText = likeText;
            this.moreLikeFields = moreLikeFields;
            this.analyzer = analyzer;
        }

        public override Query Rewrite(IndexReader reader)
        {
            MoreLikeThis mlt = new MoreLikeThis(reader);

            mlt.FieldNames = moreLikeFields;
            mlt.Analyzer = analyzer;
            mlt.MinTermFreq = minTermFrequency;
            if (minDocFreq >= 0)
            {
                mlt.MinDocFreq = minDocFreq;
            }
            mlt.MaxQueryTerms = maxQueryTerms;
            mlt.StopWords = stopWords;
            BooleanQuery bq = (BooleanQuery)mlt.Like(new StringReader(likeText), fieldName);
            BooleanClause[] clauses = bq.Clauses;
            //make at least half the terms match
            bq.MinimumNumberShouldMatch = (int)(clauses.Length * percentTermsToMatch);
            return bq;
        }
        /* (non-Javadoc)
         * <see cref="org.apache.lucene.search.Query.toString(java.lang.String)"/>
         */
        public override String ToString(String field)
        {
            return "like:" + likeText;
        }

        public float PercentTermsToMatch
        {
            get { return percentTermsToMatch; }
            set { this.percentTermsToMatch = value; }
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
            set { this.analyzer = value; }
        }

        public string LikeText
        {
            get { return likeText; }
            set { this.likeText = value; }
        }

        public int MaxQueryTerms
        {
            get { return maxQueryTerms; }
            set { this.maxQueryTerms = value; }
        }

        public int MinTermFrequency
        {
            get { return minTermFrequency; }
            set { this.minTermFrequency = value; }
        }

        public String[] GetMoreLikeFields()
        {
            return moreLikeFields;
        }

        public void SetMoreLikeFields(String[] moreLikeFields)
        {
            this.moreLikeFields = moreLikeFields;
        }
        public ISet<string> GetStopWords()
        {
            return stopWords;
        }
        public void SetStopWords(ISet<string> stopWords)
        {
            this.stopWords = stopWords;
        }

        public int MinDocFreq
        {
            get { return minDocFreq; }
            set { this.minDocFreq = value; }
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
            result = prime * result + Arrays.HashCode(moreLikeFields);
            result = prime * result + Number.FloatToIntBits(percentTermsToMatch);
            result = prime * result + ((stopWords == null) ? 0 : stopWords.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!base.Equals(obj)) return false;
            if (GetType() != obj.GetType()) return false;
            MoreLikeThisQuery other = (MoreLikeThisQuery)obj;
            if (analyzer == null)
            {
                if (other.analyzer != null) return false;
            }
            else if (!analyzer.Equals(other.analyzer)) return false;
            if (fieldName == null)
            {
                if (other.fieldName != null) return false;
            }
            else if (!fieldName.Equals(other.fieldName)) return false;
            if (likeText == null)
            {
                if (other.likeText != null) return false;
            }
            else if (!likeText.Equals(other.likeText)) return false;
            if (maxQueryTerms != other.maxQueryTerms) return false;
            if (minDocFreq != other.minDocFreq) return false;
            if (minTermFrequency != other.minTermFrequency) return false;
            if (!Arrays.Equals(moreLikeFields, other.moreLikeFields)) return false;
            if (Number.FloatToIntBits(percentTermsToMatch) != Number
                .FloatToIntBits(other.percentTermsToMatch)) return false;
            if (stopWords == null)
            {
                if (other.stopWords != null) return false;
            }
            else if (!stopWords.Equals(other.stopWords)) return false;
            return true;
        }
    }
}
