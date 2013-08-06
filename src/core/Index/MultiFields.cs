using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class MultiFields : Fields
    {
        private readonly Fields[] subs;
        private readonly ReaderSlice[] subSlices;
        private readonly IDictionary<String, Terms> terms = new ConcurrentHashMap<String, Terms>();

        public static Fields GetFields(IndexReader reader)
        {
            IList<AtomicReaderContext> leaves = reader.Leaves;
            switch (leaves.Count)
            {
                case 0:
                    // no fields
                    return null;
                case 1:
                    // already an atomic reader / reader with one leave
                    return leaves[0].AtomicReader.Fields;
                default:
                    IList<Fields> fields = new List<Fields>();
                    IList<ReaderSlice> slices = new List<ReaderSlice>();
                    foreach (AtomicReaderContext ctx in leaves)
                    {
                        AtomicReader r = ctx.AtomicReader;
                        Fields f = r.Fields;
                        if (f != null)
                        {
                            fields.Add(f);
                            slices.Add(new ReaderSlice(ctx.docBase, r.MaxDoc, fields.Count - 1));
                        }
                    }
                    if (fields.Count == 0)
                    {
                        return null;
                    }
                    else if (fields.Count == 1)
                    {
                        return fields[0];
                    }
                    else
                    {
                        return new MultiFields(fields.ToArray(), slices.ToArray());
                    }
            }
        }

        public static IBits GetLiveDocs(IndexReader reader)
        {
            if (reader.HasDeletions)
            {
                IList<AtomicReaderContext> leaves = reader.Leaves;
                int size = leaves.Count;
                //assert size > 0 : "A reader with deletions must have at least one leave";
                if (size == 1)
                {
                    return leaves[0].AtomicReader.LiveDocs;
                }
                IBits[] liveDocs = new IBits[size];
                int[] starts = new int[size + 1];
                for (int i = 0; i < size; i++)
                {
                    // record all liveDocs, even if they are null
                    AtomicReaderContext ctx = leaves[i];
                    liveDocs[i] = ctx.AtomicReader.LiveDocs;
                    starts[i] = ctx.docBase;
                }
                starts[size] = reader.MaxDoc;
                return new MultiBits(liveDocs, starts, true);
            }
            else
            {
                return null;
            }
        }

        public static Terms GetTerms(IndexReader r, String field)
        {
            Fields fields = GetFields(r);
            if (fields == null)
            {
                return null;
            }
            else
            {
                return fields.Terms(field);
            }
        }

        public static DocsEnum GetTermDocsEnum(IndexReader r, IBits liveDocs, String field, BytesRef term)
        {
            return GetTermDocsEnum(r, liveDocs, field, term, DocsEnum.FLAG_FREQS);
        }

        public static DocsEnum GetTermDocsEnum(IndexReader r, IBits liveDocs, String field, BytesRef term, int flags)
        {
            //assert field != null;
            //assert term != null;
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                if (termsEnum.SeekExact(term, true))
                {
                    return termsEnum.Docs(liveDocs, null, flags);
                }
            }
            return null;
        }

        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, IBits liveDocs, String field, BytesRef term)
        {
            return GetTermPositionsEnum(r, liveDocs, field, term, DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS);
        }

        public static DocsAndPositionsEnum GetTermPositionsEnum(IndexReader r, IBits liveDocs, String field, BytesRef term, int flags)
        {
            //assert field != null;
            //assert term != null;
            Terms terms = GetTerms(r, field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                if (termsEnum.SeekExact(term, true))
                {
                    return termsEnum.DocsAndPositions(liveDocs, null, flags);
                }
            }
            return null;
        }

        public MultiFields(Fields[] subs, ReaderSlice[] subSlices)
        {
            this.subs = subs;
            this.subSlices = subSlices;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            IEnumerator<String>[] subIterators = new IEnumerator<string>[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                subIterators[i] = subs[i].GetEnumerator();
            }
            return new MergedIterator<String>(subIterators);
        }

        public override Terms Terms(string field)
        {
            Terms result = this.terms[field];
            if (result != null)
                return result;


            // Lazy init: first time this field is requested, we
            // create & add to terms:
            IList<Terms> subs2 = new List<Terms>();
            IList<ReaderSlice> slices2 = new List<ReaderSlice>();

            // Gather all sub-readers that share this field
            for (int i = 0; i < subs.Length; i++)
            {
                Terms terms = subs[i].Terms(field);
                if (terms != null)
                {
                    subs2.Add(terms);
                    slices2.Add(subSlices[i]);
                }
            }
            if (subs2.Count == 0)
            {
                result = null;
                // don't cache this case with an unbounded cache, since the number of fields that don't exist
                // is unbounded.
            }
            else
            {
                result = new MultiTerms(subs2.ToArray(), slices2.ToArray());
                terms[field] = result;
            }

            return result;
        }

        public override int Size
        {
            get { return -1; }
        }

        public static FieldInfos GetMergedFieldInfos(IndexReader reader)
        {
            FieldInfos.Builder builder = new FieldInfos.Builder();
            foreach (AtomicReaderContext ctx in reader.Leaves)
            {
                builder.Add(ctx.AtomicReader.FieldInfos);
            }
            return builder.Finish();
        }

        public static ICollection<String> GetIndexedFields(IndexReader reader)
        {
            ICollection<String> fields = new HashSet<String>();
            foreach (FieldInfo fieldInfo in GetMergedFieldInfos(reader))
            {
                if (fieldInfo.IsIndexed)
                {
                    fields.Add(fieldInfo.name);
                }
            }
            return fields;
        }
    }
}
