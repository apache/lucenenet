using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Index
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
    /// Flex API for access to fields and terms
    ///  @lucene.experimental
    /// </summary>

    public abstract class Fields : IEnumerable<string>
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected Fields()
        {
        }

        /// <summary>
        /// Returns an iterator that will step through all fields
        ///  names.  this will not return null.
        /// </summary>
        public abstract IEnumerator<string> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the <seealso cref="Terms"/> for this field.  this will return
        ///  null if the field does not exist.
        /// </summary>
        public abstract Terms Terms(string field);

        /// <summary>
        /// Gets the number of fields or -1 if the number of
        /// distinct field names is unknown. If &gt;= 0,
        /// <see cref="GetEnumerator"/> will return as many field names.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Returns the number of terms for all fields, or -1 if this
        ///  measure isn't stored by the codec. Note that, just like
        ///  other term measures, this measure does not take deleted
        ///  documents into account. </summary>
        ///  @deprecated iterate fields and add their Count instead.
        ///   this method is only provided as a transition mechanism
        ///   to access this statistic for 3.x indexes, which do not
        ///   have this statistic per-field.
        ///  <seealso cref="Index.Terms.Count"></seealso>
        [Obsolete("iterate fields and add their Count instead.")]
        public virtual long UniqueTermCount
        {
            get
            {
                long numTerms = 0;
                foreach (string field in this)
                {
                    Terms terms = Terms(field);
                    if (terms != null)
                    {
                        long termCount = terms.Count;
                        if (termCount == -1)
                        {
                            return -1;
                        }

                        numTerms += termCount;
                    }
                }
                return numTerms;
            }
        }

        /// <summary>
        /// Zero-length {@code Fields} array.
        /// </summary>
        public static readonly Fields[] EMPTY_ARRAY = new Fields[0];
    }
}