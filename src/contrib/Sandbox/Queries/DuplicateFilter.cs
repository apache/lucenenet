using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class DuplicateFilter : Filter
    {
        public enum KeepMode
        {
            KM_USE_FIRST_OCCURRENCE,
            KM_USE_LAST_OCCURRENCE
        }

        private KeepMode keepMode;

        public enum ProcessingMode
        {
            PM_FULL_VALIDATION,
            PM_FAST_INVALIDATION
        }

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
            FixedBitSet bits = new FixedBitSet(reader.MaxDoc);
            Terms terms = reader.Fields.Terms(fieldName);
            if (terms == null)
            {
                return bits;
            }

            TermsEnum termsEnum = terms.Iterator(null);
            DocsEnum docs = null;
            while (true)
            {
                BytesRef currTerm = termsEnum.Next();
                if (currTerm == null)
                {
                    break;
                }
                else
                {
                    docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE);
                    int doc = docs.NextDoc();
                    if (doc != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (keepMode == KeepMode.KM_USE_FIRST_OCCURRENCE)
                        {
                            bits.Set(doc);
                        }
                        else
                        {
                            int lastDoc = doc;
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
            }

            return bits;
        }

        private FixedBitSet FastBits(AtomicReader reader, IBits acceptDocs)
        {
            FixedBitSet bits = new FixedBitSet(reader.MaxDoc);
            bits.Set(0, reader.MaxDoc);
            Terms terms = reader.Fields.Terms(fieldName);
            if (terms == null)
            {
                return bits;
            }

            TermsEnum termsEnum = terms.Iterator(null);
            DocsEnum docs = null;
            while (true)
            {
                BytesRef currTerm = termsEnum.Next();
                if (currTerm == null)
                {
                    break;
                }
                else
                {
                    if (termsEnum.DocFreq > 1)
                    {
                        docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE);
                        int doc = docs.NextDoc();
                        if (doc != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            if (keepMode == KeepMode.KM_USE_FIRST_OCCURRENCE)
                            {
                                doc = docs.NextDoc();
                            }
                        }

                        int lastDoc = -1;
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
                            bits.Set(lastDoc);
                        }
                    }
                }
            }

            return bits;
        }

        public virtual string FieldName
        {
            get
            {
                return fieldName;
            }
            set
            {
                this.fieldName = value;
            }
        }

        public virtual KeepMode KeepModeValue
        {
            get
            {
                return keepMode;
            }
            set
            {
                this.keepMode = value;
            }
        }
        
        public override bool Equals(Object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if ((obj == null) || (obj.GetType() != this.GetType()))
            {
                return false;
            }

            DuplicateFilter other = (DuplicateFilter)obj;
            return keepMode == other.keepMode && processingMode == other.processingMode && fieldName != null && fieldName.Equals(other.fieldName);
        }

        public override int GetHashCode()
        {
            int hash = 217;
            hash = 31 * hash + keepMode.GetHashCode();
            hash = 31 * hash + processingMode.GetHashCode();
            hash = 31 * hash + fieldName.GetHashCode();
            return hash;
        }

        public virtual ProcessingMode ProcessingModeValue
        {
            get
            {
                return processingMode;
            }
            set
            {
                this.processingMode = value;
            }
        }
    }
}
