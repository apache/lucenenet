using J2N.Collections.Generic.Extensions;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Benchmarks.Quality
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
    /// A QualityQuery has an ID and some name-value pairs.
    /// <para/>
    /// The ID allows to map the quality query with its judgements.
    /// <para/>
    /// The name-value pairs are used by a 
    /// <see cref="IQualityQueryParser"/>
    /// to create a Lucene <see cref="Search.Query"/>.
    /// <para/>
    /// It is very likely that name-value-pairs would be mapped into fields in a Lucene query,
    /// but it is up to the QualityQueryParser how to map - e.g. all values in a single field,
    /// or each pair as its own field, etc., - and this of course must match the way the 
    /// searched index was constructed.
    /// </summary>
    public class QualityQuery : IComparable<QualityQuery>
    {
        private readonly string queryID; // LUCENENET: marked readonly
        private readonly IDictionary<string, string> nameValPairs; // LUCENENET: marked readonly

        /// <summary>
        /// Create a <see cref="QualityQuery"/> with given ID and name-value pairs.
        /// </summary>
        /// <param name="queryID">ID of this quality query.</param>
        /// <param name="nameValPairs">The contents of this quality query.</param>
        public QualityQuery(string queryID, IDictionary<string, string> nameValPairs)
        {
            this.queryID = queryID;
            this.nameValPairs = nameValPairs;
        }

        /// <summary>
        /// Return all the names of name-value-pairs in this <see cref="QualityQuery"/>.
        /// </summary>
        public virtual string[] GetNames()
        {
            return nameValPairs.Keys.ToArray();
        }

        /// <summary>
        /// Return the value of a certain name-value pair.
        /// </summary>
        /// <param name="name">The name whose value should be returned.</param>
        /// <returns></returns>
        public virtual string GetValue(string name)
        {
            nameValPairs.TryGetValue(name, out string result);
            return result;
        }

        /// <summary>
        /// Gets the ID of this query.
        /// The ID allows to map the quality query with its judgements.
        /// </summary>
        public virtual string QueryID => queryID;

        /// <summary>
        /// For a nicer sort of input queries before running them.
        /// Try first as ints, fall back to string if not int.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual int CompareTo(QualityQuery other)
        {
            try
            {
                // compare as ints when ids ints
                int n = int.Parse(queryID, CultureInfo.InvariantCulture);
                int nOther = int.Parse(other.queryID, CultureInfo.InvariantCulture);
                return n - nOther;
            }
            catch (Exception e) when (e.IsNumberFormatException())
            {
                // fall back to string comparison
                return queryID.CompareToOrdinal(other.queryID);
            }
        }
    }
}
