using Lucene.Net.Facet.Params;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public class FacetsPayloadMigrationReader : FilterAtomicReader
    {
        private class PayloadMigratingBinaryDocValues : BinaryDocValues
        {
            private Fields fields;
            private Term term;
            private DocsAndPositionsEnum dpe;
            private int curDocID = -1;
            private int lastRequestedDocID;
            private DocsAndPositionsEnum GetDPE()
            {
                try
                {
                    DocsAndPositionsEnum dpe = null;
                    if (fields != null)
                    {
                        Terms terms = fields.Terms(term.Field);
                        if (terms != null)
                        {
                            TermsEnum te = terms.Iterator(null);
                            if (te.SeekExact(term.Bytes, true))
                            {
                                dpe = te.DocsAndPositions(null, null, DocsAndPositionsEnum.FLAG_PAYLOADS);
                            }
                        }
                    }

                    return dpe;
                }
                catch (IOException ioe)
                {
                    throw;
                }
            }

            internal PayloadMigratingBinaryDocValues(Fields fields, Term term)
            {
                this.fields = fields;
                this.term = term;
                this.dpe = GetDPE();
                if (dpe == null)
                {
                    curDocID = DocIdSetIterator.NO_MORE_DOCS;
                }
                else
                {
                    try
                    {
                        curDocID = dpe.NextDoc();
                    }
                    catch (IOException e)
                    {
                        throw;
                    }
                }
            }

            public override void Get(int docID, BytesRef result)
            {
                try
                {
                    if (docID <= lastRequestedDocID)
                    {
                        dpe = GetDPE();
                        if (dpe == null)
                        {
                            curDocID = DocIdSetIterator.NO_MORE_DOCS;
                        }
                        else
                        {
                            curDocID = dpe.NextDoc();
                        }
                    }

                    lastRequestedDocID = docID;
                    if (curDocID > docID)
                    {
                        result.length = 0;
                        return;
                    }

                    if (curDocID < docID)
                    {
                        curDocID = dpe.Advance(docID);
                        if (curDocID != docID)
                        {
                            result.length = 0;
                            return;
                        }
                    }

                    dpe.NextPosition();
                    result.CopyBytes(dpe.Payload);
                }
                catch (IOException e)
                {
                    throw;
                }
            }
        }

        public static readonly string PAYLOAD_TERM_TEXT = @"$fulltree$";
        public static IDictionary<String, Term> BuildFieldTermsMap(Lucene.Net.Store.Directory dir, FacetIndexingParams fip)
        {
            DirectoryReader reader = DirectoryReader.Open(dir);
            IDictionary<String, Term> fieldTerms = new HashMap<String, Term>();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                foreach (CategoryListParams clp in fip.AllCategoryListParams)
                {
                    Terms terms = context.AtomicReader.Terms(clp.field);
                    if (terms != null)
                    {
                        TermsEnum te = terms.Iterator(null);
                        BytesRef termBytes = null;
                        while ((termBytes = te.Next()) != null)
                        {
                            string term = termBytes.Utf8ToString();
                            if (term.StartsWith(PAYLOAD_TERM_TEXT))
                            {
                                if (term.Equals(PAYLOAD_TERM_TEXT))
                                {
                                    fieldTerms[clp.field] = new Term(clp.field, term);
                                }
                                else
                                {
                                    fieldTerms[clp.field + term.Substring(PAYLOAD_TERM_TEXT.Length)] = new Term(clp.field, term);
                                }
                            }
                        }
                    }
                }
            }

            reader.Dispose();
            return fieldTerms;
        }

        private readonly IDictionary<String, Term> fieldTerms;

        public FacetsPayloadMigrationReader(AtomicReader in_renamed, IDictionary<String, Term> fieldTerms)
            : base(in_renamed)
        {
            this.fieldTerms = fieldTerms;
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            Term term = fieldTerms[field];
            if (term == null)
            {
                return base.GetBinaryDocValues(field);
            }
            else
            {
                return new PayloadMigratingBinaryDocValues(Fields, term);
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                FieldInfos innerInfos = base.FieldInfos;
                List<FieldInfo> infos = new List<FieldInfo>(innerInfos.Size);
                HashSet<String> leftoverFields = new HashSet<String>(fieldTerms.Keys);
                int number = -1;
                foreach (FieldInfo info in innerInfos)
                {
                    if (fieldTerms.ContainsKey(info.name))
                    {
                        infos.Add(new FieldInfo(info.name, true, info.number, info.HasVectors, info.OmitsNorms, 
                            info.HasPayloads, info.IndexOptionsValue, FieldInfo.DocValuesType.BINARY, info.NormType, info.Attributes));
                        leftoverFields.Remove(info.name);
                    }
                    else
                    {
                        infos.Add(info);
                    }

                    number = Math.Max(number, info.number);
                }

                foreach (string field in leftoverFields)
                {
                    infos.Add(new FieldInfo(field, false, ++number, false, false, false, null, FieldInfo.DocValuesType.BINARY, null, null));
                }

                return new FieldInfos(infos.ToArray());
            }
        }
    }
}
