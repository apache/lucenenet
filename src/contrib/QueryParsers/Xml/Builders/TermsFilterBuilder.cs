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
    public class TermsFilterBuilder : IFilterBuilder
    {
        private readonly Analyzer analyzer;

        public TermsFilterBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public Filter GetFilter(XElement e)
        {
            List<BytesRef> terms = new List<BytesRef>();
            string text = DOMUtils.GetNonBlankTextOrFail(e);
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            try
            {
                TokenStream ts = analyzer.TokenStream(fieldName, new StringReader(text));
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                //Term term = null;
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    terms.Add(BytesRef.DeepCopyOf(bytes));
                }

                ts.End();
                ts.Dispose();
            }
            catch (IOException ioe)
            {
                throw new Exception(@"Error constructing terms from index:" + ioe);
            }

            return new TermsFilter(fieldName, terms);
        }
    }
}
