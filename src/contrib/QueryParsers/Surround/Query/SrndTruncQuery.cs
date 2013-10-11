using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class SrndTruncQuery : SimpleTerm
    {
        public SrndTruncQuery(string truncated, char unlimited, char mask)
            : base(false)
        {
            this.truncated = truncated;
            this.unlimited = unlimited;
            this.mask = mask;
            TruncatedToPrefixAndPattern();
        }

        private readonly string truncated;
        private readonly char unlimited;
        private readonly char mask;

        private string prefix;
        private BytesRef prefixRef;
        private Regex pattern;
        
        public string Truncated { get { return truncated; } }

        public override string ToStringUnquoted()
        {
            return Truncated;
        }

        protected bool MatchingChar(char c)
        {
            return (c != unlimited) && (c != mask);
        }

        protected void AppendRegExpForChar(char c, StringBuilder re)
        {
            if (c == unlimited)
                re.Append(".*");
            else if (c == mask)
                re.Append(".");
            else
                re.Append(c);
        }

        protected void TruncatedToPrefixAndPattern()
        {
            int i = 0;
            while ((i < truncated.Length) && MatchingChar(truncated[i]))
            {
                i++;
            }
            prefix = truncated.Substring(0, i);
            prefixRef = new BytesRef(prefix);

            StringBuilder re = new StringBuilder();
            while (i < truncated.Length)
            {
                AppendRegExpForChar(truncated[i], re);
                i++;
            }
            pattern = new Regex(re.ToString(), RegexOptions.Compiled);
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, IMatchingTermVisitor mtv)
        {
            int prefixLength = prefix.Length;
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                Match matcher;
                try
                {
                    TermsEnum termsEnum = terms.Iterator(null);

                    TermsEnum.SeekStatus status = termsEnum.SeekCeil(prefixRef);
                    BytesRef text;
                    if (status == TermsEnum.SeekStatus.FOUND)
                    {
                        text = prefixRef;
                    }
                    else if (status == TermsEnum.SeekStatus.NOT_FOUND)
                    {
                        text = termsEnum.Term;
                    }
                    else
                    {
                        text = null;
                    }

                    while (text != null)
                    {
                        if (text != null && StringHelper.StartsWith(text, prefixRef))
                        {
                            String textString = text.Utf8ToString();
                            matcher = pattern.Match(textString.Substring(prefixLength));
                            if (matcher.Success)
                            {
                                mtv.VisitMatchingTerm(new Term(fieldName, textString));
                            }
                        }
                        else
                        {
                            break;
                        }
                        text = termsEnum.Next();
                    }
                }
                finally
                {
                    //matcher.reset();
                }
            }
        }
    }
}
