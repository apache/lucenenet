using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public abstract class Fields : IEnumerable<string>
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected Fields()
        {
        }

        /// <summary>
        /// Returns an enumerator that will step through all field
        /// names.  This will not return <c>null</c>.
        /// </summary>
        public abstract IEnumerator<string> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the <see cref="Terms"/> for this field.  This will return
        /// <c>null</c> if the field does not exist.
        /// </summary>
        public abstract Terms GetTerms(string field);

        /// <summary>
        /// Gets the number of fields or -1 if the number of
        /// distinct field names is unknown. If &gt;= 0,
        /// <see cref="GetEnumerator"/> will return as many field names.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Returns the number of terms for all fields, or -1 if this
        /// measure isn't stored by the codec. Note that, just like
        /// other term measures, this measure does not take deleted
        /// documents into account. 
        /// </summary>
        /// <seealso cref="Index.Terms.Count"></seealso>
        [Obsolete("Iterate fields and add their Count instead. This method is only provided as a transition mechanism to access this statistic for 3.x indexes, which do not have this statistic per-field.")]
        public virtual long UniqueTermCount
        {
            get
            {
                long numTerms = 0;
                foreach (string field in this)
                {
                    Terms terms = GetTerms(field);
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
        /// Zero-length <see cref="Fields"/> array.
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly Fields[] EMPTY_ARRAY = Arrays.Empty<Fields>();
    }
}