using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    public class ClassicFilter : TokenFilter
    {
        public ClassicFilter(TokenStream input)
            : base(input)
        {
            typeAtt = AddAttribute<ITypeAttribute>();
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        private static readonly String APOSTROPHE_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.APOSTROPHE];
        private static readonly String ACRONYM_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.ACRONYM];

        // this filters uses attribute type
        private readonly ITypeAttribute typeAtt; // = addAttribute(TypeAttribute.class);
        private readonly ICharTermAttribute termAtt; // = addAttribute(CharTermAttribute.class);

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            char[] buffer = termAtt.Buffer;
            int bufferLength = termAtt.Length;
            String type = typeAtt.Type;

            if (type == APOSTROPHE_TYPE &&      // remove 's
                bufferLength >= 2 &&
                buffer[bufferLength - 2] == '\'' &&
                (buffer[bufferLength - 1] == 's' || buffer[bufferLength - 1] == 'S'))
            {
                // Strip last 2 characters off
                termAtt.SetLength(bufferLength - 2);
            }
            else if (type == ACRONYM_TYPE)
            {      // remove dots
                int upto = 0;
                for (int i = 0; i < bufferLength; i++)
                {
                    char c = buffer[i];
                    if (c != '.')
                        buffer[upto++] = c;
                }
                termAtt.SetLength(upto);
            }

            return true;
        }
    }
}
