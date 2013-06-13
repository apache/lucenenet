using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public abstract class AtomicReader : IndexReader
    {
        private readonly AtomicReaderContext readerContext = new AtomicReaderContext(this);

        protected AtomicReader()
        {
            super();
        }


        public static override AtomicReaderContext getContext()
        {
            ensureOpen();
            return readerContext;
        }

        public abstract Fields fields();


        public static override int DocFreq(Term term)
        {
            Fields fields = fields();
            if (fields == null)
            {
                return 0;
            }
            Terms terms = fields.terms(term.field());
            if (terms == null)
            {
                return 0;
            }
            TermsEnum termsEnum = terms.iterator(null);
            if (termsEnum.seekExact(term.bytes(), true))
            {
                return termsEnum.docFreq();
            }
            else
            {
                return 0;
            }
        }

        public static override long TotalTermFreq(Term term)
        {
            Fields fields = fields();
            if (fields == null)
            {
                return 0;
            }
            Terms terms = fields.terms(term.field());
            if (terms == null)
            {
                return 0;
            }
            TermsEnum termsEnum = terms.iterator(null);
            if (termsEnum.seekExact(term.bytes(), true))
            {
                return termsEnum.totalTermFreq();
            }
            else
            {
                return 0;
            }
        }

        public static override long GetSumDocFreq(String field)
        {
            Terms terms = terms(field);
            if (terms == null)
            {
                return 0;
            }
            return terms.getSumDocFreq();
        }

        public static override int GetDocCount(String field)
        {
            Terms terms = terms(field);
            if (terms == null)
            {
                return 0;
            }
            return terms.getDocCount();
        }



        public static override long GetSumTotalTermFreq(String field)
        {
            Terms terms = terms(field);
            if (terms == null)
            {
                return 0;
            }
            return terms.getSumTotalTermFreq();
        }

        /** This may return null if the field does not exist.*/
        public static Terms terms(String field)
        {
            Fields fields = fields();
            if (fields == null)
            {
                return null;
            }
            return fields.terms(field);
        }

        public static DocsEnum TermDocsEnum(Term term)
        {
            if (term.field == null)
                throw new IOException();
            if (term.bytes() == null)
                throw new IOException();
            Fields fields = fields();
            if (fields != null)
            {
                Terms terms = fields.terms(term.field());
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.iterator(null);
                    if (termsEnum.seekExact(term.bytes(), true))
                    {
                        return termsEnum.docs(getLiveDocs(), null);
                    }
                }
            }
            return null;
        }


        public static DocsAndPositionsEnum TermPositionsEnum(Term term)
        {
            if (term.field == null)
                throw new IOException();
            if (term.bytes == null)
                throw new IOException();

            Fields fields = fields();
            if (fields != null)
            {
                Terms terms = fields.terms(term.field);
                if (terms != null)
                {
                    TermsEnum termsEnum = terms.iterator(null);
                    if (termsEnum.seekExact(term.bytes, true))
                    {
                        return termsEnum.docsAndPositions(getLiveDocs(), null);
                    }
                }
            }
            return null;
        }

        public abstract NumericDocValues GetNumericDocValues(String field);
        public abstract BinaryDocValues GetBinaryDocValues(String field);
        public abstract SortedDocValues GetSortedDocValues(String field);
        public abstract SortedSetDocValues getSortedSetDocValues(String field);
        public abstract NumericDocValues getNormValues(String field);
        public abstract FieldInfos getFieldInfos();
        public abstract Bits GetLiveDocs();
    }
}
