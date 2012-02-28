using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Sinks
{
    public class TokenTypeSinkFilter : TeeSinkTokenFilter.SinkFilter
    {
        private string typeToMatch;
        private TypeAttribute typeAtt;

        public TokenTypeSinkFilter(string typeToMatch)
        {
            this.typeToMatch = typeToMatch;
        }

        public override bool Accept(AttributeSource source)
        {
            if (typeAtt == null)
            {
                typeAtt = source.AddAttribute<TypeAttribute>();
            }

            return typeToMatch.Equals(typeAtt.Type());
        }
    }
}
