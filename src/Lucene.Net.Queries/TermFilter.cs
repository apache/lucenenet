using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

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
            if (term == null)
            {
                throw new System.ArgumentException("Term must not be null");
            }
            else if (term.Field == null)
            {
                throw new System.ArgumentException("Field must not be null");
            }
            this.term = term;
        }

        /// <summary> Gets the term this filter includes documents with. </summary>
        public Term Term
        {
            get
            {
                return term;
            }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            Terms terms = context.AtomicReader.GetTerms(term.Field);
            if (terms == null)
            {
                return null;
            }

            TermsEnum termsEnum = terms.GetIterator(null);
            if (!termsEnum.SeekExact(term.Bytes))
            {
                return null;
            }
            return new DocIdSetAnonymousInnerClassHelper(acceptDocs, termsEnum);
        }

        private class DocIdSetAnonymousInnerClassHelper : DocIdSet
        {
            private readonly IBits acceptDocs;
            private readonly TermsEnum termsEnum;

            public DocIdSetAnonymousInnerClassHelper(IBits acceptDocs, TermsEnum termsEnum)
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
            if (o == null || this.GetType() != o.GetType())
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
            return term.Field + ":" + term.Text();
        }
    }
}