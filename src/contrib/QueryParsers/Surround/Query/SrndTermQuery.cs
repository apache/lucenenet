using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class SrndTermQuery : SimpleTerm
    {
        public SrndTermQuery(string termText, bool quoted)
            : base(quoted)
        {
            this.termText = termText;
        }

        private readonly string termText;
        public string TermText { get { return termText; } }

        public Term GetLuceneTerm(string fieldName)
        {
            return new Term(fieldName, TermText);
        }

        public override string ToStringUnquoted()
        {
            return TermText;
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, IMatchingTermVisitor mtv)
        {
            /* check term presence in index here for symmetry with other SimpleTerm's */
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);

                TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(TermText));
                if (status == TermsEnum.SeekStatus.FOUND)
                {
                    mtv.VisitMatchingTerm(GetLuceneTerm(fieldName));
                }
            }
        }
    }
}
