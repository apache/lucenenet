using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class TermsQueryBuilder : IQueryBuilder
    {
        private readonly Analyzer analyzer;

        public TermsQueryBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public Query GetQuery(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string text = DOMUtils.GetNonBlankTextOrFail(e);
            BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false));
            bq.MinimumNumberShouldMatch = DOMUtils.GetAttribute(e, "minimumNumberShouldMatch", 0);
            try
            {
                TokenStream ts = analyzer.TokenStream(fieldName, new StringReader(text));
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                Term term = null;
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    term = new Term(fieldName, BytesRef.DeepCopyOf(bytes));
                    bq.Add(new BooleanClause(new TermQuery(term), Occur.SHOULD));
                }

                ts.End();
                ts.Dispose();
            }
            catch (IOException ioe)
            {
                throw new Exception(@"Error constructing terms from index:" + ioe);
            }

            bq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return bq;
        }
    }
}
