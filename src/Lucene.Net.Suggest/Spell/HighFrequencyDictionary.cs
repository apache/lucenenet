using Lucene.Net.Index;
using Lucene.Net.Search.Suggest;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Search.Spell
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
    /// HighFrequencyDictionary: terms taken from the given field
    /// of a Lucene index, which appear in a number of documents
    /// above a given threshold.
    /// 
    /// Threshold is a value in [0..1] representing the minimum
    /// number of documents (of the total) where a term should appear.
    /// 
    /// Based on <see cref="LuceneDictionary"/>.
    /// </summary>
    public class HighFrequencyDictionary : IDictionary
    {
        private IndexReader reader;
        private string field;
        private float thresh;

        /// <summary>
        /// Creates a new Dictionary, pulling source terms from
        /// the specified <code>field</code> in the provided <code>reader</code>.
        /// <para>
        /// Terms appearing in less than <code>thresh</code> percentage of documents
        /// will be excluded.
        /// </para>
        /// </summary>
        public HighFrequencyDictionary(IndexReader reader, string field, float thresh)
        {
            this.reader = reader;
            this.field = field;
            this.thresh = thresh;
        }

        public IInputIterator EntryIterator
        {
            get
            {
                return new HighFrequencyIterator(this);
            }
        }

        internal sealed class HighFrequencyIterator : IInputIterator
        {
            private readonly HighFrequencyDictionary outerInstance;

            internal readonly BytesRef spare = new BytesRef();
            internal readonly TermsEnum termsEnum;
            internal int minNumDocs;
            internal long freq;

            internal HighFrequencyIterator(HighFrequencyDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
                Terms terms = MultiFields.GetTerms(outerInstance.reader, outerInstance.field);
                if (terms != null)
                {
                    termsEnum = terms.Iterator(null);
                }
                else
                {
                    termsEnum = null;
                }
                minNumDocs = (int)(outerInstance.thresh * (float)outerInstance.reader.NumDocs);
            }

            internal bool IsFrequent(int freq)
            {
                return freq >= minNumDocs;
            }

            public long Weight
            {
                get { return freq; }
            }

            public BytesRef Next()
            {
                if (termsEnum != null)
                {
                    BytesRef next;
                    while ((next = termsEnum.Next()) != null)
                    {
                        if (IsFrequent(termsEnum.DocFreq))
                        {
                            freq = termsEnum.DocFreq;
                            spare.CopyBytes(next);
                            return spare;
                        }
                    }
                }
                return null;
            }

            public IComparer<BytesRef> Comparer
            {
                get
                {
                    if (termsEnum == null)
                    {
                        return null;
                    }
                    else
                    {
                        return termsEnum.Comparer;
                    }
                }
            }

            public BytesRef Payload
            {
                get { return null; }
            }

            public bool HasPayloads
            {
                get { return false; }
            }

            public IEnumerable<BytesRef> Contexts
            {
                get { return null; }
            }

            public bool HasContexts
            {
                get { return false; }
            }
        }
    }
}