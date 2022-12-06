// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Queries
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
    /// A filter that includes documents that match with a specific term.
    /// </summary>
    public sealed class TermFilter : Filter
    {
        private readonly Term term;

        /// <param name="term"> The term documents need to have in order to be a match for this filter. </param>
        public TermFilter(Term term)
        {
            if (term is null)
            {
                throw new ArgumentNullException(nameof(term), "Term must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            else if (term.Field is null)
            {
                throw new ArgumentNullException(nameof(term.Field), "term.Field must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            this.term = term;
        }

        /// <summary> Gets the term this filter includes documents with. </summary>
        public Term Term => term;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            Terms terms = context.AtomicReader.GetTerms(term.Field);
            if (terms is null)
            {
                return null;
            }

            TermsEnum termsEnum = terms.GetEnumerator();
            if (!termsEnum.SeekExact(term.Bytes))
            {
                return null;
            }
            return new DocIdSetAnonymousClass(acceptDocs, termsEnum);
        }

        private sealed class DocIdSetAnonymousClass : DocIdSet
        {
            private readonly IBits acceptDocs;
            private readonly TermsEnum termsEnum;

            public DocIdSetAnonymousClass(IBits acceptDocs, TermsEnum termsEnum)
            {
                this.acceptDocs = acceptDocs;
                this.termsEnum = termsEnum;
            }

            public override DocIdSetIterator GetIterator()
            {
                return termsEnum.Docs(acceptDocs, null, DocsFlags.NONE);
            }
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o is null || this.GetType() != o.GetType())
            {
                return false;
            }

            TermFilter that = (TermFilter)o;

            if (term != null ? !term.Equals(that.term) : that.term != null)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return term != null ? term.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return term.Field + ":" + term.Text;
        }
    }
}