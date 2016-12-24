/*
 * Created on 25-Jan-2006
 */
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;

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
    /// A simple wrapper for MoreLikeThis for use in scenarios where a Query object is required eg
    /// in custom QueryParser extensions. At query.rewrite() time the reader is used to construct the
    /// actual MoreLikeThis object and obtain the real Query object.
    /// </summary>
    public class MoreLikeThisQuery : Query
    {
        private readonly string fieldName;

        /// <param name="moreLikeFields"> fields used for similarity measure </param>
        public MoreLikeThisQuery(string likeText, string[] moreLikeFields, Analyzer analyzer, string fieldName)
        {
            this.LikeText = likeText;
            this.MoreLikeFields = moreLikeFields;
            this.Analyzer = analyzer;
            this.fieldName = fieldName;
            StopWords = null;

            PercentTermsToMatch = 0.3f;
            MinTermFrequency = 1;
            MaxQueryTerms = 5;
            MinDocFreq = -1;
        }

        public override Query Rewrite(IndexReader reader)
        {
            var mlt = new MoreLikeThis(reader) { FieldNames = MoreLikeFields, Analyzer = Analyzer, MinTermFreq = MinTermFrequency };

            if (MinDocFreq >= 0)
            {
                mlt.MinDocFreq = MinDocFreq;
            }
            mlt.MaxQueryTerms = MaxQueryTerms;
            mlt.StopWords = StopWords;
            var bq = (BooleanQuery)mlt.Like(new StringReader(LikeText), fieldName);
            var clauses = bq.GetClauses();
            //make at least half the terms match
            bq.MinimumNumberShouldMatch = (int)(clauses.Length * PercentTermsToMatch);
            return bq;
        }

        /* (non-Javadoc)
        * @see org.apache.lucene.search.Query#toString(java.lang.String)
        */
        public override string ToString(string field)
        {
            return "like:" + LikeText;
        }

        public float PercentTermsToMatch { get; set; }

        public Analyzer Analyzer { get; set; }

        public string LikeText { get; set; }

        public int MaxQueryTerms { get; set; }

        public int MinTermFrequency { get; set; }

        public string[] MoreLikeFields { get; set; }

        public HashSet<string> StopWords { get; set; } // LUCENENET TODO: Change to ISet

        public int MinDocFreq { get; set; }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((Analyzer == null) ? 0 : Analyzer.GetHashCode());
            result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
            result = prime * result + ((LikeText == null) ? 0 : LikeText.GetHashCode());
            result = prime * result + MaxQueryTerms;
            result = prime * result + MinDocFreq;
            result = prime * result + MinTermFrequency;
            result = prime * result + Arrays.GetHashCode(MoreLikeFields);
            result = prime * result + Number.FloatToIntBits(PercentTermsToMatch);
            result = prime * result + ((StopWords == null) ? 0 : StopWords.GetHashCode());
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
            if (Analyzer == null)
            {
                if (other.Analyzer != null)
                {
                    return false;
                }
            }
            else if (!Analyzer.Equals(other.Analyzer))
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
            if (LikeText == null)
            {
                if (other.LikeText != null)
                {
                    return false;
                }
            }
            else if (!LikeText.Equals(other.LikeText))
            {
                return false;
            }
            if (MaxQueryTerms != other.MaxQueryTerms)
            {
                return false;
            }
            if (MinDocFreq != other.MinDocFreq)
            {
                return false;
            }
            if (MinTermFrequency != other.MinTermFrequency)
            {
                return false;
            }
            if (!Arrays.Equals(MoreLikeFields, other.MoreLikeFields))
            {
                return false;
            }
            if (Number.FloatToIntBits(PercentTermsToMatch) != Number.FloatToIntBits(other.PercentTermsToMatch))
            {
                return false;
            }
            if (StopWords == null)
            {
                if (other.StopWords != null)
                {
                    return false;
                }
            }
            else if (!StopWords.Equals(other.StopWords))
            {
                return false;
            }
            return true;
        }
    }
}