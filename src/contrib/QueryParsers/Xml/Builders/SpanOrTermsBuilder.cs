using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanOrTermsBuilder : SpanBuilderBase
    {
        private readonly Analyzer analyzer;

        public SpanOrTermsBuilder(Analyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public override SpanQuery GetSpanQuery(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string value = DOMUtils.GetNonBlankTextOrFail(e);
            try
            {
                List<SpanQuery> clausesList = new List<SpanQuery>();
                TokenStream ts = analyzer.TokenStream(fieldName, new StringReader(value));
                ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                BytesRef bytes = termAtt.BytesRef;
                ts.Reset();
                while (ts.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    SpanTermQuery stq = new SpanTermQuery(new Term(fieldName, BytesRef.DeepCopyOf(bytes)));
                    clausesList.Add(stq);
                }

                ts.End();
                ts.Dispose();
                SpanOrQuery soq = new SpanOrQuery(clausesList.ToArray());
                soq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
                return soq;
            }
            catch (IOException)
            {
                throw new ParserException(@"IOException parsing value:" + value);
            }
        }
    }
}
