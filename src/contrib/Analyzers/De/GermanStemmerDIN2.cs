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
    public sealed class GermanStemmerDIN2 : GermanStemmer
    {
        protected override void SubstituteUmlauts(StringBuilder buffer, int c)
        {
            if (buffer[c] == 'ä')
            {
                buffer[c] = 'a';
                buffer.Insert(c + 1, 'e');
            }
            else if (buffer[c] == 'ö')
            {
                buffer[c] = 'o';
                buffer.Insert(c + 1, 'e');
            }
            else if (buffer[c] == 'ü')
            {
                buffer[c] = 'u';
                buffer.Insert(c + 1, 'e');
            }
            // Fix bug so that 'ß' at the end of a word is replaced.
            else if (buffer[c] == 'ß')
            {
                buffer[c] = 's';
                buffer.Insert(c + 1, 's');
                substCount++;
            }
        }
    }
}
