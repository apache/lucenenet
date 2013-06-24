using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class FilterAtomicReader : AtomicReader
    {
        public class FilterFields : Fields
        {
            protected readonly Fields instance;

            public FilterFields(Fields instance)
            {
                this.instance = instance;
            }

            public override IEnumerable<string> Iterator
            {
                get { return instance.Iterator; }
            }

            public override Terms Terms(string field)
            {
                return instance.Terms(field);
            }

            public override int Size
            {
                get { return instance.Size; }
            }
        }

        public class FilterTerms : Terms
        {
            protected readonly Terms instance;

            public FilterTerms(Terms instance)
            {
                this.instance = instance;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return instance.Iterator(reuse);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return instance.Comparator; }
            }

            public override long Size
            {
                get { return instance.Size; }
            }

            public override long SumTotalTermFreq
            {
                get { return instance.SumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return instance.SumDocFreq; }
            }

            public override int DocCount
            {
                get { return instance.DocCount; }
            }

            public override bool HasOffsets
            {
                get { return instance.HasOffsets; }
            }

            public override bool HasPositions
            {
                get { return instance.HasPositions; }
            }

            public override bool HasPayloads
            {
                get { return instance.HasPayloads; }
            }
        }

        public class FilterTermsEnum : TermsEnum
        {
            protected readonly TermsEnum instance;

            public FilterTermsEnum(TermsEnum instance)
            {
                this.instance = instance;
            }

            public override AttributeSource Attributes
            {
                get
                {
                    return instance.Attributes;
                }
            }

            public override SeekStatus SeekCeil(BytesRef text, bool useCache)
            {
                return instance.SeekCeil(text, useCache);
            }

            public override void SeekExact(long ord)
            {
                instance.SeekExact(ord);
            }

            public override BytesRef Next()
            {
                return instance.Next();
            }

            public override BytesRef Term
            {
                get { return instance.Term; }
            }

            public override long Ord
            {
                get { return instance.Ord; }
            }

            public override int DocFreq
            {
                get { return instance.DocFreq; }
            }

            public override long TotalTermFreq
            {
                get { return instance.TotalTermFreq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                return instance.Docs(liveDocs, reuse, flags);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return instance.DocsAndPositions(liveDocs, reuse, flags);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return instance.Comparator; }
            }
        }

        public class FilterDocsEnum : DocsEnum
        {
            protected readonly DocsEnum instance;

            public FilterDocsEnum(DocsEnum instance)
            {
                this.instance = instance;
            }

            public override AttributeSource Attributes
            {
                get
                {
                    return instance.Attributes;
                }
            }

            public override int DocID
            {
                get { return instance.DocID; }
            }

            public override int Freq
            {
                get { return instance.Freq; }
            }

            public override int NextDoc()
            {
                return instance.NextDoc();
            }

            public override int Advance(int target)
            {
                return instance.Advance(target);
            }

            public override long Cost
            {
                get { return instance.Cost; }
            }
        }

        public class FilterDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            protected readonly DocsAndPositionsEnum instance;

            public FilterDocsAndPositionsEnum(DocsAndPositionsEnum instance)
            {
                this.instance = instance;
            }

            public override AttributeSource Attributes
            {
                get
                {
                    return instance.Attributes;
                }
            }

            public override int DocID
            {
                get { return instance.DocID; }
            }

            public override int Freq
            {
                get { return instance.Freq; }
            }

            public override int NextDoc()
            {
                return instance.NextDoc();
            }

            public override int Advance(int target)
            {
                return instance.Advance(target);
            }

            public override int NextPosition()
            {
                return instance.NextPosition();
            }

            public override int StartOffset
            {
                get { return instance.StartOffset; }
            }

            public override int EndOffset
            {
                get { return instance.EndOffset; }
            }

            public override BytesRef Payload
            {
                get { return instance.Payload; }
            }

            public override long Cost
            {
                get { return instance.Cost; }
            }
        }

        protected readonly AtomicReader instance;

        public FilterAtomicReader(AtomicReader instance)
            : base()
        {
            this.instance = instance;
            instance.RegisterParentReader(this);
        }

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return instance.LiveDocs;
            }
        }

        public override FieldInfos FieldInfos
        {
            get { return instance.FieldInfos; }
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return instance.GetTermVectors(docID);
        }

        public override int NumDocs
        {
            get { return instance.NumDocs; }
        }

        public override int MaxDoc
        {
            get { return instance.MaxDoc; }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            instance.Document(docID, visitor);
        }

        protected override void DoClose()
        {
            instance.Dispose();
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return instance.Fields;
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("FilterAtomicReader(");
            buffer.Append(instance);
            buffer.Append(')');
            return buffer.ToString();
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return instance.GetNumericDocValues(field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return instance.GetBinaryDocValues(field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            return instance.GetSortedDocValues(field);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            return instance.GetSortedSetDocValues(field);
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return instance.GetNormValues(field);
        }
    }
}
