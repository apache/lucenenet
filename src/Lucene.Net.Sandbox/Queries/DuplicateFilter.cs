using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Sandbox.Queries
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
    /// Filter to remove duplicate values from search results.
    /// <para/>
    /// WARNING: for this to work correctly, you may have to wrap
    /// your reader as it cannot current deduplicate across different
    /// index segments.
    /// </summary>
    /// <seealso cref="SlowCompositeReaderWrapper"/>
    public class DuplicateFilter : Filter
    {
        // TODO: make duplicate filter aware of ReaderContext such that we can
        // filter duplicates across segments

        // LUCENENET NOTE: KeepMode enum moved outside of this class to avoid naming collisions

        private KeepMode keepMode;

        // LUCENENET NOTE: ProcessingMode enum moved outside of this class to avoid naming collisions

        private ProcessingMode processingMode;

        private string fieldName;

        public DuplicateFilter(string fieldName)
            : this(fieldName, KeepMode.KM_USE_LAST_OCCURRENCE, ProcessingMode.PM_FULL_VALIDATION)
        {
        }

        public DuplicateFilter(string fieldName, KeepMode keepMode, ProcessingMode processingMode)
        {
            this.fieldName = fieldName;
            this.keepMode = keepMode;
            this.processingMode = processingMode;
        }


        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            if (processingMode == ProcessingMode.PM_FAST_INVALIDATION)
            {
                return FastBits(context.AtomicReader, acceptDocs);
            }
            else
            {
                return CorrectBits(context.AtomicReader, acceptDocs);
            }
        }

        private FixedBitSet CorrectBits(AtomicReader reader, IBits acceptDocs)
        {
            FixedBitSet bits = new FixedBitSet(reader.MaxDoc); //assume all are INvalid
            Terms terms = reader.Fields.GetTerms(fieldName);

            if (terms is null)
            {
                return bits;
            }

            TermsEnum termsEnum = terms.GetEnumerator();
            DocsEnum docs = null;
            while (termsEnum.MoveNext())
            {
                docs = termsEnum.Docs(acceptDocs, docs, DocsFlags.NONE);
                int doc = docs.NextDoc();
                if (doc != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (keepMode == KeepMode.KM_USE_FIRST_OCCURRENCE)
                    {
                        bits.Set(doc);
                    }
                    else
                    {
                        int lastDoc/* = doc*/; // LUCENENET: Removed unnecessary assignment
                        while (true)
                        {
                            lastDoc = doc;
                            doc = docs.NextDoc();
                            if (doc == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                        }
                        bits.Set(lastDoc);
                    }
                }
            }
            return bits;
        }

        private FixedBitSet FastBits(AtomicReader reader, IBits acceptDocs)
        {
            FixedBitSet bits = new FixedBitSet(reader.MaxDoc);
            bits.Set(0, reader.MaxDoc); //assume all are valid
            Terms terms = reader.Fields.GetTerms(fieldName);

            if (terms is null)
            {
                return bits;
            }

            TermsEnum termsEnum = terms.GetEnumerator();
            DocsEnum docs = null;
            while (termsEnum.MoveNext())
            {
                if (termsEnum.DocFreq > 1)
                {
                    // unset potential duplicates
                    docs = termsEnum.Docs(acceptDocs, docs, DocsFlags.NONE);
                    int doc = docs.NextDoc();
                    if (doc != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (keepMode == KeepMode.KM_USE_FIRST_OCCURRENCE)
                        {
                            doc = docs.NextDoc();
                        }
                    }

                    int lastDoc/* = -1*/; // LUCENENET: Unnecessary assignment
                    while (true)
                    {
                        lastDoc = doc;
                        bits.Clear(lastDoc);
                        doc = docs.NextDoc();
                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                    }

                    if (keepMode == KeepMode.KM_USE_LAST_OCCURRENCE)
                    {
                        // restore the last bit
                        bits.Set(lastDoc);
                    }
                }
            }

            return bits;
        }

        public virtual string FieldName
        {
            get => fieldName;
            set => this.fieldName = value;
        }

        public KeepMode KeepMode
        {
            get => keepMode;
            set => keepMode = value;
        }


        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if ((obj is null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            DuplicateFilter other = (DuplicateFilter)obj;
            return keepMode == other.keepMode &&
                processingMode == other.processingMode &&
                fieldName != null && fieldName.Equals(other.fieldName, StringComparison.Ordinal);
        }


        public override int GetHashCode()
        {
            int hash = 217;
            hash = 31 * hash + keepMode.GetHashCode();
            hash = 31 * hash + processingMode.GetHashCode();
            hash = 31 * hash + fieldName.GetHashCode();
            return hash;
        }

        public ProcessingMode ProcessingMode
        {
            get => processingMode;
            set => processingMode = value;
        }
    }

    /// <summary>
    /// KeepMode determines which document id to consider as the master, all others being
    /// identified as duplicates. Selecting the "first occurrence" can potentially save on IO.
    /// </summary>
    public enum KeepMode
    {
        KM_USE_FIRST_OCCURRENCE,

        KM_USE_LAST_OCCURRENCE
    }

    /// <summary>
    /// "Full" processing mode starts by setting all bits to false and only setting bits
    /// for documents that contain the given field and are identified as none-duplicates.
    /// <para/>
    /// "Fast" processing sets all bits to true then unsets all duplicate docs found for the
    /// given field. This approach avoids the need to read DocsEnum for terms that are seen
    /// to have a document frequency of exactly "1" (i.e. no duplicates). While a potentially
    /// faster approach , the downside is that bitsets produced will include bits set for
    /// documents that do not actually contain the field given.
    /// </summary>
    public enum ProcessingMode
    {
        /// <summary>
        /// "Full" processing mode starts by setting all bits to false and only setting bits
        /// for documents that contain the given field and are identified as none-duplicates.
        /// </summary>
        PM_FULL_VALIDATION,

        /// <summary>
        /// "Fast" processing sets all bits to true then unsets all duplicate docs found for the
        /// given field. This approach avoids the need to read DocsEnum for terms that are seen
        /// to have a document frequency of exactly "1" (i.e. no duplicates). While a potentially
        /// faster approach , the downside is that bitsets produced will include bits set for
        /// documents that do not actually contain the field given.
        /// </summary>
        PM_FAST_INVALIDATION
    }
}
