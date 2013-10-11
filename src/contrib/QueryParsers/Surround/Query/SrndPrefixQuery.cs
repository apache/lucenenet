using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class SrndPrefixQuery : SimpleTerm
    {
        private readonly BytesRef prefixRef;

        public SrndPrefixQuery(string prefix, bool quoted, char truncator)
            : base(quoted)
        {
            this.prefix = prefix;
            prefixRef = new BytesRef(prefix);
            this.truncator = truncator;
        }

        private readonly string prefix;
        public string Prefix { get { return prefix; } }

        private readonly char truncator;
        public char SuffixOperator { get { return truncator; } }

        public Term GetLucenePrefixTerm(string fieldName)
        {
            return new Term(fieldName, Prefix);
        }

        public override string ToStringUnquoted()
        {
            return Prefix;
        }

        protected override void SuffixToString(StringBuilder r)
        {
            r.Append(SuffixOperator);
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, IMatchingTermVisitor mtv)
        {
            /* inspired by PrefixQuery.rewrite(): */
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);

                bool skip = false;
                TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(Prefix));
                if (status == TermsEnum.SeekStatus.FOUND)
                {
                    mtv.VisitMatchingTerm(GetLucenePrefixTerm(fieldName));
                }
                else if (status == TermsEnum.SeekStatus.NOT_FOUND)
                {
                    if (StringHelper.StartsWith(termsEnum.Term, prefixRef))
                    {
                        mtv.VisitMatchingTerm(new Term(fieldName, termsEnum.Term.Utf8ToString()));
                    }
                    else
                    {
                        skip = true;
                    }
                }
                else
                {
                    // EOF
                    skip = true;
                }

                if (!skip)
                {
                    while (true)
                    {
                        BytesRef text = termsEnum.Next();
                        if (text != null && StringHelper.StartsWith(text, prefixRef))
                        {
                            mtv.VisitMatchingTerm(new Term(fieldName, text.Utf8ToString()));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
