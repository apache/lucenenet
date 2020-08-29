using Lucene.Net.Index;
using Lucene.Net.Search.Suggest;
using System;

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
    /// Lucene Dictionary: terms taken from the given field
    /// of a Lucene index.
    /// </summary>
    public class LuceneDictionary : IDictionary
    {
        private readonly IndexReader reader;
        private readonly string field;

        /// <summary>
        /// Creates a new Dictionary, pulling source terms from
        /// the specified <code>field</code> in the provided <code>reader</code>
        /// </summary>
        public LuceneDictionary(IndexReader reader, string field)
        {
            this.reader = reader;
            this.field = field;
        }

        public virtual IInputEnumerator GetEntryEnumerator()
        {
            Terms terms = MultiFields.GetTerms(reader, field);
            if (terms != null)
            {
                return new InputEnumeratorWrapper(terms.GetEnumerator(null));
            }
            else
            {
                return InputEnumerator.EMPTY;
            }
        }
    }
}