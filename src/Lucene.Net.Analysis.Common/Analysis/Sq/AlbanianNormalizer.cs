// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using J2N;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Analysis.Sq
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Normalizer for Albanian.
    /// <para>
    /// Standard Albanian orthography has two letters outside ASCII, <c>ë</c> and <c>ç</c>.
    /// Both are very frequently written as plain <c>e</c> and <c>c</c> in informal text,
    /// on keyboards without those keys, and in search queries, so the two spellings must
    /// index to the same term. This normalizer folds:
    /// <list type="bullet">
    ///     <item><description><c>ë</c> → <c>e</c>, and <c>ç</c> and <c>c</c> → <c>q</c>, whether precomposed or
    ///     written as a base letter followed by a combining diaeresis / cedilla
    ///     (text copied from PDFs and some web pages comes in that decomposed form)</description></item>
    ///     <item><description>the circumflex and acute vowels of Gheg writing (<c>â ê î ô û</c>
    ///     for nasal vowels, <c>á é í ó ú</c> for long ones, as in <c>âsht</c>, <c>nânë</c>,
    ///     <c>Malcís</c>) → plain vowels</description></item>
    ///     <item><description>elided clitics written with an apostrophe are dropped
    ///     (<c>ç'është</c> → <c>është</c>, <c>s'ka</c> → <c>ka</c>, <c>t'i</c> → <c>i</c>),
    ///     so the remaining word can be matched against the stop list</description></item>
    ///     <item><description>other apostrophes inside a word are removed (<c>Kur'an</c> → <c>kuran</c>),
    ///     covering U+0027, U+2019 and U+02BC which all occur in Albanian text</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <c>ç</c>, <c>c</c> and <c>q</c> become one class because that is how the letter is typed:
    /// without the cedilla as <c>c</c>, or as <c>q</c>, which sounds the same to most speakers
    /// (search logs of an Albanian site show <c>cdo</c> and <c>qdo</c> far more often than
    /// <c>çdo</c>). The class is spelled <c>q</c> so that the stemmer's palatal rules
    /// (<c>miq</c> / <c>mik</c>) keep working on folded text. A few genuine pairs merge
    /// (<c>qaj</c> / <c>çaj</c>), the same trade-off as <c>ë</c> / <c>e</c>.
    /// </para>
    /// <para>
    /// Words that really have a <c>c</c> (<c>cili</c>, <c>caktuar</c>, <c>shumica</c>, <c>kec</c>) keep it:
    /// they are listed, as whole words or stem prefixes, in <c>cwords.txt</c>, so <c>kec</c> stays apart
    /// from <c>keq</c> while <c>cdo</c> still meets <c>çdo</c>.
    /// </para>
    /// <para>
    /// The digraph <c>xh</c> is folded to <c>gj</c> for the same reason: the two sound alike to many
    /// speakers, and the logs have <c>xhami</c> and <c>gjami</c>, <c>haxh</c> and <c>hagj</c> for the
    /// same word.
    /// </para>
    /// <para>
    /// The folding is applied to both indexed text and queries, so it also lets the
    /// <see cref="AlbanianLightStemmer"/> work on a single spelling of every suffix.
    /// </para>
    /// </summary>
    public class AlbanianNormalizer
    {
        /// <summary>The words that keep their c: <c>cwords.txt</c>, next to this class (see the file's header).</summary>
        public const string KEEP_C_FILE = "cwords.txt";

        private static readonly CharArraySet KEEP_C_WORDS;
        private static readonly CharArraySet KEEP_C_PREFIXES;

        static AlbanianNormalizer()
        {
            var words = new CharArraySet(LuceneVersion.LUCENE_48, 16, ignoreCase: false);
            var prefixes = new CharArraySet(LuceneVersion.LUCENE_48, 256, ignoreCase: false);
            try
            {
                using (Stream stream = typeof(AlbanianNormalizer).FindAndGetManifestResourceStream(KEEP_C_FILE))
                using (TextReader reader = IOUtils.GetDecodingReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0 || line[0] == '#') continue;
                        if (line[line.Length - 1] == '*') prefixes.Add(line.Substring(0, line.Length - 1));
                        else words.Add(line);
                    }
                }
            }
            catch (Exception ex) when (ex.IsIOException())
            {
                throw RuntimeException.Create("Unable to load " + KEEP_C_FILE, ex);
            }
            KEEP_C_WORDS = words.AsReadOnly();
            KEEP_C_PREFIXES = prefixes.AsReadOnly();
        }

        /// <summary>
        /// Whether the word really has a c (cili, caktuar, kec) and keeps it: it is, lowercased and with
        /// ë read as e, one of the words in <see cref="KEEP_C_FILE"/> or starts with one of its prefixes.
        /// </summary>
        internal static bool KeepsC(char[] s, int len)
        {
            int first = -1;
            for (int i = 0; i < len; i++)
            {
                if (s[i] == 'c' || s[i] == 'C') { first = i; break; }
            }
            if (first < 0) return false;
            char[] key = new char[len];
            for (int i = 0; i < len; i++)
            {
                char ch = char.ToLowerInvariant(s[i]);
                key[i] = ch == 'ë' ? 'e' : ch;
            }
            if (KEEP_C_WORDS.Contains(key, 0, len)) return true;
            for (int k = first + 1; k <= len; k++)
            {
                if (KEEP_C_PREFIXES.Contains(key, 0, k)) return true;
            }
            return false;
        }

        /// <summary>
        /// Normalize an input buffer of Albanian text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Normalize(char[] s, int len)
        {
            bool keepC = KeepsC(s, len);
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'ë':
                    case 'ê':
                    case 'é':
                        s[i] = 'e';
                        break;
                    case 'Ë':
                    case 'Ê':
                    case 'É':
                        s[i] = 'E';
                        break;
                    case 'x':
                    case 'X':
                        // the digraph xh is heard as gj by many speakers (xhami / gjami, haxh / hagj): one class
                        if (i + 1 < len && (s[i + 1] == 'h' || s[i + 1] == 'H'))
                        {
                            s[i] = s[i] == 'x' ? 'g' : 'G';
                            s[i + 1] = s[i + 1] == 'h' ? 'j' : 'J';
                            i++;
                        }
                        break;
                    case 'ç':
                        // ç is typed as c (no cedilla) and as q (the same sound to most speakers): one class,
                        // spelled q so the stemmer's palatal rules (miq / mik) still apply after folding
                        s[i] = 'q';
                        break;
                    case 'c':
                        // unless the word really has a c (cili, kec): those are listed in cwords.txt
                        if (!keepC) s[i] = 'q';
                        break;
                    case 'Ç':
                        s[i] = 'Q';
                        break;
                    case 'C':
                        if (!keepC) s[i] = 'Q';
                        break;
                    case 'â':
                    case 'á':
                        s[i] = 'a';
                        break;
                    case 'Â':
                    case 'Á':
                        s[i] = 'A';
                        break;
                    case 'î':
                    case 'í':
                        s[i] = 'i';
                        break;
                    case 'Î':
                    case 'Í':
                        s[i] = 'I';
                        break;
                    case 'ô':
                    case 'ó':
                        s[i] = 'o';
                        break;
                    case 'Ô':
                    case 'Ó':
                        s[i] = 'O';
                        break;
                    case 'û':
                    case 'ú':
                        s[i] = 'u';
                        break;
                    case 'Û':
                    case 'Ú':
                        s[i] = 'U';
                        break;
                    case '̈': // combining diaeresis (e + ̈ = ë)
                    case '̧': // combining cedilla (c + ̧ = ç)
                    case '̂': // combining circumflex (Gheg â ê î ô û)
                    case '́': // combining acute (Gheg á é í ó ú)
                        len = StemmerUtil.Delete(s, i, len);
                        i--;
                        break;
                    case '\'':      // apostrophe
                    case '’':  // right single quotation mark
                    case 'ʼ':  // modifier letter apostrophe
                        if (i == 1 && len > 2)
                        {
                            // a one-letter elided clitic: ç'është, s'ka, t'i, m'u, q'u
                            len = StemmerUtil.DeleteN(s, 0, len, 2);
                            i = -1;
                        }
                        else
                        {
                            len = StemmerUtil.Delete(s, i, len);
                            i--;
                        }
                        break;
                }
            }
            return len;
        }
    }
}
