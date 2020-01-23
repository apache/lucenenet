using J2N.Collections;

namespace Lucene.Net.Search
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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// This is a <see cref="PhraseQuery"/> which is optimized for n-gram phrase query.
    /// For example, when you query "ABCD" on a 2-gram field, you may want to use
    /// <see cref="NGramPhraseQuery"/> rather than <see cref="PhraseQuery"/>, because <see cref="NGramPhraseQuery"/>
    /// will <see cref="Rewrite(IndexReader)"/> the query to "AB/0 CD/2", while <see cref="PhraseQuery"/>
    /// will query "AB/0 BC/1 CD/2" (where term/position).
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="PhraseQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var phraseQuery = new NGramPhraseQuery(2) {
    ///     new Term("field", "ABCD"), 
    ///     new Term("field", "EFGH")
    /// };
    /// </code>
    /// Note that as long as you specify all of the parameters, you can use either
    /// <see cref="PhraseQuery.Add(Term)"/> or <see cref="PhraseQuery.Add(Term, int)"/>
    /// as the method to use to initialize. If there are multiple parameters, each parameter set
    /// must be surrounded by curly braces.
    /// </summary>
    public class NGramPhraseQuery : PhraseQuery
    {
        private readonly int n;

        /// <summary>
        /// Constructor that takes gram size. </summary>
        /// <param name="n"> n-gram size </param>
        public NGramPhraseQuery(int n)
            : base()
        {
            this.n = n;
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (Slop != 0)
            {
                return base.Rewrite(reader);
            }

            // check whether optimizable or not
            if (n < 2 || GetTerms().Length < 3) // too short to optimize -  non-overlap n-gram cannot be optimized
            {
                return base.Rewrite(reader);
            }

            // check all posIncrement is 1
            // if not, cannot optimize
            int[] positions = GetPositions();
            Term[] terms = GetTerms();
            int prevPosition = positions[0];
            for (int i = 1; i < positions.Length; i++)
            {
                int pos = positions[i];
                if (prevPosition + 1 != pos)
                {
                    return base.Rewrite(reader);
                }
                prevPosition = pos;
            }

            // now create the new optimized phrase query for n-gram
            PhraseQuery optimized = new PhraseQuery();
            optimized.Boost = Boost;
            int pos_ = 0;
            int lastPos = terms.Length - 1;
            for (int i = 0; i < terms.Length; i++)
            {
                if (pos_ % n == 0 || pos_ >= lastPos)
                {
                    optimized.Add(terms[i], positions[i]);
                }
                pos_++;
            }

            return optimized;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is NGramPhraseQuery))
            {
                return false;
            }
            NGramPhraseQuery other = (NGramPhraseQuery)o;
            if (this.n != other.n)
            {
                return false;
            }
            return base.Equals(other);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(Boost) 
                ^ Slop 
                ^ ArrayEqualityComparer<Term>.OneDimensional.GetHashCode(GetTerms()) 
                ^ ArrayEqualityComparer<int>.OneDimensional.GetHashCode(GetPositions()) 
                ^ n;
        }
    }
}