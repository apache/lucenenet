using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.De
{
    /// <summary>
    /// A stemmer for the german language that uses the
    /// DIN-5007-2 "Phone Book" rules for handling
    /// umlaut characters.
    /// </summary>
    public sealed class GermanDIN2Stemmer : GermanStemmer
    {
        protected override void Substitute(StringBuilder buffer)
        {
            for (int c = 0; c < buffer.Length; c++)
            {
                if (buffer[c] == 'e')
                {
                    switch (buffer[c - 1])
                    {
                        case 'a':
                        case 'o':
                        case 'u':
                            buffer.Remove(c, 1);
                            break;
                    }
                }
            }
            base.Substitute(buffer);
        }
    }
}
