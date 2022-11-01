// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.El
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
    /// A stemmer for Greek words, according to: <c>Development of a Stemmer for the
    /// Greek Language.</c> Georgios Ntais
    /// <para>
    /// NOTE: Input is expected to be casefolded for Greek (including folding of final
    /// sigma to sigma), and with diacritics removed. This can be achieved with 
    /// either <see cref="GreekLowerCaseFilter"/> or ICUFoldingFilter.
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class GreekStemmer
    {
        /// <summary>
        /// Stems a word contained in a leading portion of a <see cref="T:char[]"/> array.
        /// The word is passed through a number of rules that modify it's length.
        /// </summary>
        /// <param name="s"> A <see cref="T:char[]"/> array that contains the word to be stemmed. </param>
        /// <param name="len"> The length of the <see cref="T:char[]"/> array. </param>
        /// <returns> The new length of the stemmed word. </returns>
        public virtual int Stem(char[] s, int len)
        {
            if (len < 4) // too short
            {
                return len;
            }

            int origLen = len;
            // "short rules": if it hits one of these, it skips the "long list"
            len = Rule0(s, len);
            len = Rule1(s, len);
            len = Rule2(s, len);
            len = Rule3(s, len);
            len = Rule4(s, len);
            len = Rule5(s, len);
            len = Rule6(s, len);
            len = Rule7(s, len);
            len = Rule8(s, len);
            len = Rule9(s, len);
            len = Rule10(s, len);
            len = Rule11(s, len);
            len = Rule12(s, len);
            len = Rule13(s, len);
            len = Rule14(s, len);
            len = Rule15(s, len);
            len = Rule16(s, len);
            len = Rule17(s, len);
            len = Rule18(s, len);
            len = Rule19(s, len);
            len = Rule20(s, len);
            // "long list"
            if (len == origLen)
            {
                len = Rule21(s, len);
            }

            return Rule22(s, len);
        }

        private static int Rule0(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 9 && (StemmerUtil.EndsWith(s, len, "καθεστωτοσ") || 
                StemmerUtil.EndsWith(s, len, "καθεστωτων")))
            {
                return len - 4;
            }

            if (len > 8 && (StemmerUtil.EndsWith(s, len, "γεγονοτοσ") || 
                StemmerUtil.EndsWith(s, len, "γεγονοτων")))
            {
                return len - 4;
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "καθεστωτα"))
            {
                return len - 3;
            }

            if (len > 7 && (StemmerUtil.EndsWith(s, len, "τατογιου") || 
                StemmerUtil.EndsWith(s, len, "τατογιων")))
            {
                return len - 4;
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "γεγονοτα"))
            {
                return len - 3;
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "καθεστωσ"))
            {
                return len - 2;
            }

            if (len > 6 && (StemmerUtil.EndsWith(s, len, "σκαγιου")) || 
                StemmerUtil.EndsWith(s, len, "σκαγιων") || 
                StemmerUtil.EndsWith(s, len, "ολογιου") || 
                StemmerUtil.EndsWith(s, len, "ολογιων") || 
                StemmerUtil.EndsWith(s, len, "κρεατοσ") || 
                StemmerUtil.EndsWith(s, len, "κρεατων") || 
                StemmerUtil.EndsWith(s, len, "περατοσ") || 
                StemmerUtil.EndsWith(s, len, "περατων") || 
                StemmerUtil.EndsWith(s, len, "τερατοσ") || 
                StemmerUtil.EndsWith(s, len, "τερατων"))
            {
                return len - 4;
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "τατογια"))
            {
                return len - 3;
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "γεγονοσ"))
            {
                return len - 2;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "φαγιου") || 
                StemmerUtil.EndsWith(s, len, "φαγιων") || 
                StemmerUtil.EndsWith(s, len, "σογιου") || 
                StemmerUtil.EndsWith(s, len, "σογιων")))
            {
                return len - 4;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "σκαγια") || 
                StemmerUtil.EndsWith(s, len, "ολογια") || 
                StemmerUtil.EndsWith(s, len, "κρεατα") || 
                StemmerUtil.EndsWith(s, len, "περατα") || 
                StemmerUtil.EndsWith(s, len, "τερατα")))
            {
                return len - 3;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "φαγια") || 
                StemmerUtil.EndsWith(s, len, "σογια") || 
                StemmerUtil.EndsWith(s, len, "φωτοσ") || 
                StemmerUtil.EndsWith(s, len, "φωτων")))
            {
                return len - 3;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "κρεασ") || 
                StemmerUtil.EndsWith(s, len, "περασ") || 
                StemmerUtil.EndsWith(s, len, "τερασ")))
            {
                return len - 2;
            }

            if (len > 3 && StemmerUtil.EndsWith(s, len, "φωτα"))
            {
                return len - 2;
            }

            if (len > 2 && StemmerUtil.EndsWith(s, len, "φωσ"))
            {
                return len - 1;
            }

            return len;
        }

        private static int Rule1(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && (StemmerUtil.EndsWith(s, len, "αδεσ") || 
                StemmerUtil.EndsWith(s, len, "αδων")))
            {
                len -= 4;
                if (!(StemmerUtil.EndsWith(s, len, "οκ") || 
                    StemmerUtil.EndsWith(s, len, "μαμ") || 
                    StemmerUtil.EndsWith(s, len, "μαν") || 
                    StemmerUtil.EndsWith(s, len, "μπαμπ") || 
                    StemmerUtil.EndsWith(s, len, "πατερ") || 
                    StemmerUtil.EndsWith(s, len, "γιαγι") || 
                    StemmerUtil.EndsWith(s, len, "νταντ") || 
                    StemmerUtil.EndsWith(s, len, "κυρ") || 
                    StemmerUtil.EndsWith(s, len, "θει") || 
                    StemmerUtil.EndsWith(s, len, "πεθερ")))
                {
                    len += 2; // add back -αδ
                }
            }
            return len;
        }

        private static int Rule2(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && (StemmerUtil.EndsWith(s, len, "εδεσ") || 
                StemmerUtil.EndsWith(s, len, "εδων")))
            {
                len -= 4;
                if (StemmerUtil.EndsWith(s, len, "οπ") ||
                    StemmerUtil.EndsWith(s, len, "ιπ") || 
                    StemmerUtil.EndsWith(s, len, "εμπ") || 
                    StemmerUtil.EndsWith(s, len, "υπ") || 
                    StemmerUtil.EndsWith(s, len, "γηπ") || 
                    StemmerUtil.EndsWith(s, len, "δαπ") || 
                    StemmerUtil.EndsWith(s, len, "κρασπ") || 
                    StemmerUtil.EndsWith(s, len, "μιλ"))
                {
                    len += 2; // add back -εδ
                }
            }
            return len;
        }

        private static int Rule3(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && (StemmerUtil.EndsWith(s, len, "ουδεσ") || 
                StemmerUtil.EndsWith(s, len, "ουδων")))
            {
                len -= 5;
                if (StemmerUtil.EndsWith(s, len, "αρκ") || 
                    StemmerUtil.EndsWith(s, len, "καλιακ") || 
                    StemmerUtil.EndsWith(s, len, "πεταλ") || 
                    StemmerUtil.EndsWith(s, len, "λιχ") || 
                    StemmerUtil.EndsWith(s, len, "πλεξ") || 
                    StemmerUtil.EndsWith(s, len, "σκ") || 
                    StemmerUtil.EndsWith(s, len, "σ") || 
                    StemmerUtil.EndsWith(s, len, "φλ") || 
                    StemmerUtil.EndsWith(s, len, "φρ") || 
                    StemmerUtil.EndsWith(s, len, "βελ") || 
                    StemmerUtil.EndsWith(s, len, "λουλ") || 
                    StemmerUtil.EndsWith(s, len, "χν") || 
                    StemmerUtil.EndsWith(s, len, "σπ") || 
                    StemmerUtil.EndsWith(s, len, "τραγ") || 
                    StemmerUtil.EndsWith(s, len, "φε"))
                {
                    len += 3; // add back -ουδ
                }
            }
            return len;
        }

#pragma warning disable 612, 618
        private static readonly CharArraySet exc4 = new CharArraySet(LuceneVersion.LUCENE_CURRENT, new string[] { "θ", "δ", "ελ", "γαλ", "ν", "π", "ιδ", "παρ" }, false);
#pragma warning restore 612, 618

        private static int Rule4(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 3 && (StemmerUtil.EndsWith(s, len, "εωσ") || 
                StemmerUtil.EndsWith(s, len, "εων")))
            {
                len -= 3;
                if (exc4.Contains(s, 0, len))
                {
                    len++; // add back -ε
                }
            }
            return len;
        }

        private static int Rule5(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 2 && StemmerUtil.EndsWith(s, len, "ια"))
            {
                len -= 2;
                if (EndsWithVowel(s, len))
                {
                    len++; // add back -ι
                }
            }
            else if (len > 3 && (StemmerUtil.EndsWith(s, len, "ιου") || 
                StemmerUtil.EndsWith(s, len, "ιων")))
            {
                len -= 3;
                if (EndsWithVowel(s, len))
                {
                    len++; // add back -ι
                }
            }
            return len;
        }

        private static readonly CharArraySet exc6 =
#pragma warning disable 612, 618
            new CharArraySet(LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "αλ", "αδ", "ενδ", "αμαν", "αμμοχαλ", "ηθ", "ανηθ",
                "αντιδ", "φυσ", "βρωμ", "γερ", "εξωδ", "καλπ", "καλλιν", "καταδ",
                "μουλ", "μπαν", "μπαγιατ", "μπολ", "μποσ", "νιτ", "ξικ", "συνομηλ",
                "πετσ", "πιτσ", "πικαντ", "πλιατσ", "ποστελν", "πρωτοδ", "σερτ",
                "συναδ", "τσαμ", "υποδ", "φιλον", "φυλοδ", "χασ" }, false);

        private static int Rule6(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;
            if (len > 3 && (StemmerUtil.EndsWith(s, len, "ικα") || 
                StemmerUtil.EndsWith(s, len, "ικο")))
            {
                len -= 3;
                removed = true;
            }
            else if (len > 4 && (StemmerUtil.EndsWith(s, len, "ικου") || 
                StemmerUtil.EndsWith(s, len, "ικων")))
            {
                len -= 4;
                removed = true;
            }

            if (removed)
            {
                if (EndsWithVowel(s, len) || exc6.Contains(s, 0, len))
                {
                    len += 2; // add back -ικ
                }
            }
            return len;
        }

        private static readonly CharArraySet exc7 =
#pragma warning disable 612, 618
            new CharArraySet(LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "αναπ", "αποθ", "αποκ", "αποστ", "βουβ", "ξεθ", "ουλ",
                "πεθ", "πικρ", "ποτ", "σιχ", "χ" }, false);

        private static int Rule7(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len == 5 && StemmerUtil.EndsWith(s, len, "αγαμε"))
            {
                return len - 1;
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "ηθηκαμε"))
            {
                len -= 7;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "ουσαμε"))
            {
                len -= 6;
            }
            else if (len > 5 && (StemmerUtil.EndsWith(s, len, "αγαμε") || 
                StemmerUtil.EndsWith(s, len, "ησαμε") || 
                StemmerUtil.EndsWith(s, len, "ηκαμε")))
            {
                len -= 5;
            }

            if (len > 3 && StemmerUtil.EndsWith(s, len, "αμε"))
            {
                len -= 3;
                if (exc7.Contains(s, 0, len))
                {
                    len += 2; // add back -αμ
                }
            }

            return len;
        }

        private static readonly CharArraySet exc8a = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "τρ", "τσ" }, false);

        private static readonly CharArraySet exc8b = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "βετερ", "βουλκ", "βραχμ", "γ", "δραδουμ", "θ", "καλπουζ",
                "καστελ", "κορμορ", "λαοπλ", "μωαμεθ", "μ", "μουσουλμ", "ν", "ουλ",
                "π", "πελεκ", "πλ", "πολισ", "πορτολ", "σαρακατσ", "σουλτ",
                "τσαρλατ", "ορφ", "τσιγγ", "τσοπ", "φωτοστεφ", "χ", "ψυχοπλ", "αγ",
                "ορφ", "γαλ", "γερ", "δεκ", "διπλ", "αμερικαν", "ουρ", "πιθ",
                "πουριτ", "σ", "ζωντ", "ικ", "καστ", "κοπ", "λιχ", "λουθηρ", "μαιντ",
                "μελ", "σιγ", "σπ", "στεγ", "τραγ", "τσαγ", "φ", "ερ", "αδαπ",
                "αθιγγ", "αμηχ", "ανικ", "ανοργ", "απηγ", "απιθ", "ατσιγγ", "βασ",
                "βασκ", "βαθυγαλ", "βιομηχ", "βραχυκ", "διατ", "διαφ", "ενοργ",
                "θυσ", "καπνοβιομηχ", "καταγαλ", "κλιβ", "κοιλαρφ", "λιβ",
                "μεγλοβιομηχ", "μικροβιομηχ", "νταβ", "ξηροκλιβ", "ολιγοδαμ",
                "ολογαλ", "πενταρφ", "περηφ", "περιτρ", "πλατ", "πολυδαπ", "πολυμηχ",
                "στεφ", "ταβ", "τετ", "υπερηφ", "υποκοπ", "χαμηλοδαπ", "ψηλοταβ" }, false);

        private static int Rule8(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;

            if (len > 8 && StemmerUtil.EndsWith(s, len, "ιουντανε"))
            {
                len -= 8;
                removed = true;
            }
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "ιοντανε") || 
                StemmerUtil.EndsWith(s, len, "ουντανε") || 
                StemmerUtil.EndsWith(s, len, "ηθηκανε"))
            {
                len -= 7;
                removed = true;
            }
            else if (len > 6 && StemmerUtil.EndsWith(s, len, "ιοτανε") || 
                StemmerUtil.EndsWith(s, len, "οντανε") || 
                StemmerUtil.EndsWith(s, len, "ουσανε"))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 5 && StemmerUtil.EndsWith(s, len, "αγανε") || 
                StemmerUtil.EndsWith(s, len, "ησανε") || 
                StemmerUtil.EndsWith(s, len, "οτανε") || 
                StemmerUtil.EndsWith(s, len, "ηκανε"))
            {
                len -= 5;
                removed = true;
            }

            if (removed && exc8a.Contains(s, 0, len))
            {
                // add -αγαν (we removed > 4 chars so its safe)
                len += 4;
                s[len - 4] = 'α';
                s[len - 3] = 'γ';
                s[len - 2] = 'α';
                s[len - 1] = 'ν';
            }

            if (len > 3 && StemmerUtil.EndsWith(s, len, "ανε"))
            {
                len -= 3;
                if (EndsWithVowelNoY(s, len) || exc8b.Contains(s, 0, len))
                {
                    len += 2; // add back -αν
                }
            }

            return len;
        }

        private static readonly CharArraySet exc9 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "αβαρ", "βεν", "εναρ", "αβρ", "αδ", "αθ", "αν", "απλ",
                "βαρον", "ντρ", "σκ", "κοπ", "μπορ", "νιφ", "παγ", "παρακαλ", "σερπ",
                "σκελ", "συρφ", "τοκ", "υ", "δ", "εμ", "θαρρ", "θ" }, false);

        private static int Rule9(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && StemmerUtil.EndsWith(s, len, "ησετε"))
            {
                len -= 5;
            }

            if (len > 3 && StemmerUtil.EndsWith(s, len, "ετε"))
            {
                len -= 3;
                if (exc9.Contains(s, 0, len) || 
                    EndsWithVowelNoY(s, len) || 
                    StemmerUtil.EndsWith(s, len, "οδ") || 
                    StemmerUtil.EndsWith(s, len, "αιρ") || 
                    StemmerUtil.EndsWith(s, len, "φορ") || 
                    StemmerUtil.EndsWith(s, len, "ταθ") || 
                    StemmerUtil.EndsWith(s, len, "διαθ") || 
                    StemmerUtil.EndsWith(s, len, "σχ") || 
                    StemmerUtil.EndsWith(s, len, "ενδ") || 
                    StemmerUtil.EndsWith(s, len, "ευρ") || 
                    StemmerUtil.EndsWith(s, len, "τιθ") || 
                    StemmerUtil.EndsWith(s, len, "υπερθ") || 
                    StemmerUtil.EndsWith(s, len, "ραθ") || 
                    StemmerUtil.EndsWith(s, len, "ενθ") || 
                    StemmerUtil.EndsWith(s, len, "ροθ") || 
                    StemmerUtil.EndsWith(s, len, "σθ") || 
                    StemmerUtil.EndsWith(s, len, "πυρ") || 
                    StemmerUtil.EndsWith(s, len, "αιν") || 
                    StemmerUtil.EndsWith(s, len, "συνδ") || 
                    StemmerUtil.EndsWith(s, len, "συν") || 
                    StemmerUtil.EndsWith(s, len, "συνθ") || 
                    StemmerUtil.EndsWith(s, len, "χωρ") || 
                    StemmerUtil.EndsWith(s, len, "πον") || 
                    StemmerUtil.EndsWith(s, len, "βρ") || 
                    StemmerUtil.EndsWith(s, len, "καθ") || 
                    StemmerUtil.EndsWith(s, len, "ευθ") || 
                    StemmerUtil.EndsWith(s, len, "εκθ") || 
                    StemmerUtil.EndsWith(s, len, "νετ") || 
                    StemmerUtil.EndsWith(s, len, "ρον") || 
                    StemmerUtil.EndsWith(s, len, "αρκ") || 
                    StemmerUtil.EndsWith(s, len, "βαρ") || 
                    StemmerUtil.EndsWith(s, len, "βολ") || 
                    StemmerUtil.EndsWith(s, len, "ωφελ"))
                {
                    len += 2; // add back -ετ
                }
            }

            return len;
        }

        private static int Rule10(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && (StemmerUtil.EndsWith(s, len, "οντασ") || StemmerUtil.EndsWith(s, len, "ωντασ")))
            {
                len -= 5;
                if (len == 3 && StemmerUtil.EndsWith(s, len, "αρχ"))
                {
                    len += 3; // add back *ντ
                    s[len - 3] = 'ο';
                }
                if (StemmerUtil.EndsWith(s, len, "κρε"))
                {
                    len += 3; // add back *ντ
                    s[len - 3] = 'ω';
                }
            }

            return len;
        }

        private static int Rule11(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6 && StemmerUtil.EndsWith(s, len, "ομαστε"))
            {
                len -= 6;
                if (len == 2 && StemmerUtil.EndsWith(s, len, "ον"))
                {
                    len += 5; // add back -ομαστ
                }
            }
            else if (len > 7 && StemmerUtil.EndsWith(s, len, "ιομαστε"))
            {
                len -= 7;
                if (len == 2 && StemmerUtil.EndsWith(s, len, "ον"))
                {
                    len += 5;
                    s[len - 5] = 'ο';
                    s[len - 4] = 'μ';
                    s[len - 3] = 'α';
                    s[len - 2] = 'σ';
                    s[len - 1] = 'τ';
                }
            }
            return len;
        }

#pragma warning disable 612, 618
        private static readonly CharArraySet exc12a = new CharArraySet(LuceneVersion.LUCENE_CURRENT, 
            new string[] { "π", "απ", "συμπ", "ασυμπ", "ακαταπ", "αμεταμφ" }, false);

        private static readonly CharArraySet exc12b = new CharArraySet(LuceneVersion.LUCENE_CURRENT, 
            new string[] { "αλ", "αρ", "εκτελ", "ζ", "μ", "ξ", "παρακαλ", "αρ", "προ", "νισ" }, false);
#pragma warning restore 612, 618

        private static int Rule12(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && StemmerUtil.EndsWith(s, len, "ιεστε"))
            {
                len -= 5;
                if (exc12a.Contains(s, 0, len))
                {
                    len += 4; // add back -ιεστ
                }
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "εστε"))
            {
                len -= 4;
                if (exc12b.Contains(s, 0, len))
                {
                    len += 3; // add back -εστ
                }
            }

            return len;
        }

        private static readonly CharArraySet exc13 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "διαθ", "θ", "παρακαταθ", "προσθ", "συνθ" }, false);

        private static int Rule13(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6 && StemmerUtil.EndsWith(s, len, "ηθηκεσ"))
            {
                len -= 6;
            }
            else if (len > 5 && (StemmerUtil.EndsWith(s, len, "ηθηκα") || StemmerUtil.EndsWith(s, len, "ηθηκε")))
            {
                len -= 5;
            }

            bool removed = false;

            if (len > 4 && StemmerUtil.EndsWith(s, len, "ηκεσ"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (StemmerUtil.EndsWith(s, len, "ηκα") || StemmerUtil.EndsWith(s, len, "ηκε")))
            {
                len -= 3;
                removed = true;
            }

            if (removed && (exc13.Contains(s, 0, len) || 
                StemmerUtil.EndsWith(s, len, "σκωλ") || 
                StemmerUtil.EndsWith(s, len, "σκουλ") || 
                StemmerUtil.EndsWith(s, len, "ναρθ") || 
                StemmerUtil.EndsWith(s, len, "σφ") || 
                StemmerUtil.EndsWith(s, len, "οθ") || 
                StemmerUtil.EndsWith(s, len, "πιθ")))
            {
                len += 2; // add back the -ηκ
            }

            return len;
        }

        private static readonly CharArraySet exc14 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "φαρμακ", "χαδ", "αγκ", "αναρρ", "βρομ", "εκλιπ", "λαμπιδ",
                "λεχ", "μ", "πατ", "ρ", "λ", "μεδ", "μεσαζ", "υποτειν", "αμ", "αιθ",
                "ανηκ", "δεσποζ", "ενδιαφερ", "δε", "δευτερευ", "καθαρευ", "πλε", "τσα" }, false);

        private static int Rule14(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;

            if (len > 5 && StemmerUtil.EndsWith(s, len, "ουσεσ"))
            {
                len -= 5;
                removed = true;
            }
            else if (len > 4 && (StemmerUtil.EndsWith(s, len, "ουσα") || 
                StemmerUtil.EndsWith(s, len, "ουσε")))
            {
                len -= 4;
                removed = true;
            }

            if (removed && (exc14.Contains(s, 0, len) || 
                EndsWithVowel(s, len) || 
                StemmerUtil.EndsWith(s, len, "ποδαρ") || 
                StemmerUtil.EndsWith(s, len, "βλεπ") || 
                StemmerUtil.EndsWith(s, len, "πανταχ") || 
                StemmerUtil.EndsWith(s, len, "φρυδ") || 
                StemmerUtil.EndsWith(s, len, "μαντιλ") || 
                StemmerUtil.EndsWith(s, len, "μαλλ") || 
                StemmerUtil.EndsWith(s, len, "κυματ") || 
                StemmerUtil.EndsWith(s, len, "λαχ") || 
                StemmerUtil.EndsWith(s, len, "ληγ") || 
                StemmerUtil.EndsWith(s, len, "φαγ") || 
                StemmerUtil.EndsWith(s, len, "ομ") || 
                StemmerUtil.EndsWith(s, len, "πρωτ")))
            {
                len += 3; // add back -ουσ
            }

            return len;
        }

        private static readonly CharArraySet exc15a = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "αβαστ", "πολυφ", "αδηφ", "παμφ", "ρ", "ασπ", "αφ", "αμαλ",
                "αμαλλι", "ανυστ", "απερ", "ασπαρ", "αχαρ", "δερβεν", "δροσοπ",
                "ξεφ", "νεοπ", "νομοτ", "ολοπ", "ομοτ", "προστ", "προσωποπ", "συμπ",
                "συντ", "τ", "υποτ", "χαρ", "αειπ", "αιμοστ", "ανυπ", "αποτ",
                "αρτιπ", "διατ", "εν", "επιτ", "κροκαλοπ", "σιδηροπ", "λ", "ναυ",
                "ουλαμ", "ουρ", "π", "τρ", "μ" }, false);

        private static readonly CharArraySet exc15b = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "ψοφ", "ναυλοχ" }, false);

        private static int Rule15(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;
            if (len > 4 && StemmerUtil.EndsWith(s, len, "αγεσ"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (StemmerUtil.EndsWith(s, len, "αγα") || StemmerUtil.EndsWith(s, len, "αγε")))
            {
                len -= 3;
                removed = true;
            }

            if (removed)
            {
                bool cond1 = exc15a.Contains(s, 0, len) || 
                    StemmerUtil.EndsWith(s, len, "οφ") || 
                    StemmerUtil.EndsWith(s, len, "πελ") || 
                    StemmerUtil.EndsWith(s, len, "χορτ") || 
                    StemmerUtil.EndsWith(s, len, "λλ") || 
                    StemmerUtil.EndsWith(s, len, "σφ") || 
                    StemmerUtil.EndsWith(s, len, "ρπ") || 
                    StemmerUtil.EndsWith(s, len, "φρ") || 
                    StemmerUtil.EndsWith(s, len, "πρ") || 
                    StemmerUtil.EndsWith(s, len, "λοχ") || 
                    StemmerUtil.EndsWith(s, len, "σμην");

                bool cond2 = exc15b.Contains(s, 0, len) || StemmerUtil.EndsWith(s, len, "κολλ");

                if (cond1 && !cond2)
                {
                    len += 2; // add back -αγ
                }
            }

            return len;
        }

        private static readonly CharArraySet exc16 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "ν", "χερσον", "δωδεκαν", "ερημον", "μεγαλον", "επταν" }, false);

        private static int Rule16(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;
            if (len > 4 && StemmerUtil.EndsWith(s, len, "ησου"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (StemmerUtil.EndsWith(s, len, "ησε") || StemmerUtil.EndsWith(s, len, "ησα")))
            {
                len -= 3;
                removed = true;
            }

            if (removed && exc16.Contains(s, 0, len))
            {
                len += 2; // add back -ησ
            }

            return len;
        }

        private static readonly CharArraySet exc17 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "ασβ", "σβ", "αχρ", "χρ", "απλ", "αειμν", "δυσχρ", "ευχρ", "κοινοχρ", "παλιμψ" }, false);

        private static int Rule17(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && StemmerUtil.EndsWith(s, len, "ηστε"))
            {
                len -= 4;
                if (exc17.Contains(s, 0, len))
                {
                    len += 3; // add back the -ηστ
                }
            }

            return len;
        }

        private static readonly CharArraySet exc18 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "ν", "ρ", "σπι", "στραβομουτσ", "κακομουτσ", "εξων" }, false);

        private static int Rule18(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;

            if (len > 6 && (StemmerUtil.EndsWith(s, len, "ησουνε") || StemmerUtil.EndsWith(s, len, "ηθουνε")))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "ουνε"))
            {
                len -= 4;
                removed = true;
            }

            if (removed && exc18.Contains(s, 0, len))
            {
                len += 3;
                s[len - 3] = 'ο';
                s[len - 2] = 'υ';
                s[len - 1] = 'ν';
            }
            return len;
        }

        private static readonly CharArraySet exc19 = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
            new string[] { "παρασουσ", "φ", "χ", "ωριοπλ", "αζ", "αλλοσουσ", "ασουσ" }, false);

        private static int Rule19(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            bool removed = false;

            if (len > 6 && (StemmerUtil.EndsWith(s, len, "ησουμε") || StemmerUtil.EndsWith(s, len, "ηθουμε")))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "ουμε"))
            {
                len -= 4;
                removed = true;
            }

            if (removed && exc19.Contains(s, 0, len))
            {
                len += 3;
                s[len - 3] = 'ο';
                s[len - 2] = 'υ';
                s[len - 1] = 'μ';
            }
            return len;
        }

        private static int Rule20(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && (StemmerUtil.EndsWith(s, len, "ματων") || StemmerUtil.EndsWith(s, len, "ματοσ")))
            {
                len -= 3;
            }
            else if (len > 4 && StemmerUtil.EndsWith(s, len, "ματα"))
            {
                len -= 2;
            }
            return len;
        }

        private static int Rule21(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 9 && StemmerUtil.EndsWith(s, len, "ιοντουσαν"))
            {
                return len - 9;
            }

            if (len > 8 && (StemmerUtil.EndsWith(s, len, "ιομασταν") || 
                StemmerUtil.EndsWith(s, len, "ιοσασταν") || 
                StemmerUtil.EndsWith(s, len, "ιουμαστε") || 
                StemmerUtil.EndsWith(s, len, "οντουσαν")))
            {
                return len - 8;
            }

            if (len > 7 && (StemmerUtil.EndsWith(s, len, "ιεμαστε") || 
                StemmerUtil.EndsWith(s, len, "ιεσαστε") || 
                StemmerUtil.EndsWith(s, len, "ιομουνα") || 
                StemmerUtil.EndsWith(s, len, "ιοσαστε") || 
                StemmerUtil.EndsWith(s, len, "ιοσουνα") || 
                StemmerUtil.EndsWith(s, len, "ιουνται") || 
                StemmerUtil.EndsWith(s, len, "ιουνταν") || 
                StemmerUtil.EndsWith(s, len, "ηθηκατε") || 
                StemmerUtil.EndsWith(s, len, "ομασταν") || 
                StemmerUtil.EndsWith(s, len, "οσασταν") || 
                StemmerUtil.EndsWith(s, len, "ουμαστε")))
            {
                return len - 7;
            }

            if (len > 6 && (StemmerUtil.EndsWith(s, len, "ιομουν") || 
                StemmerUtil.EndsWith(s, len, "ιονταν") || 
                StemmerUtil.EndsWith(s, len, "ιοσουν") || 
                StemmerUtil.EndsWith(s, len, "ηθειτε") || 
                StemmerUtil.EndsWith(s, len, "ηθηκαν") || 
                StemmerUtil.EndsWith(s, len, "ομουνα") || 
                StemmerUtil.EndsWith(s, len, "οσαστε") || 
                StemmerUtil.EndsWith(s, len, "οσουνα") || 
                StemmerUtil.EndsWith(s, len, "ουνται") || 
                StemmerUtil.EndsWith(s, len, "ουνταν") || 
                StemmerUtil.EndsWith(s, len, "ουσατε")))
            {
                return len - 6;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "αγατε") || 
                StemmerUtil.EndsWith(s, len, "ιεμαι") || 
                StemmerUtil.EndsWith(s, len, "ιεται") || 
                StemmerUtil.EndsWith(s, len, "ιεσαι") || 
                StemmerUtil.EndsWith(s, len, "ιοταν") || 
                StemmerUtil.EndsWith(s, len, "ιουμα") || 
                StemmerUtil.EndsWith(s, len, "ηθεισ") || 
                StemmerUtil.EndsWith(s, len, "ηθουν") || 
                StemmerUtil.EndsWith(s, len, "ηκατε") || 
                StemmerUtil.EndsWith(s, len, "ησατε") || 
                StemmerUtil.EndsWith(s, len, "ησουν") || 
                StemmerUtil.EndsWith(s, len, "ομουν") ||
                StemmerUtil.EndsWith(s, len, "ονται") || 
                StemmerUtil.EndsWith(s, len, "ονταν") || 
                StemmerUtil.EndsWith(s, len, "οσουν") || 
                StemmerUtil.EndsWith(s, len, "ουμαι") || 
                StemmerUtil.EndsWith(s, len, "ουσαν")))
            {
                return len - 5;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "αγαν") || 
                StemmerUtil.EndsWith(s, len, "αμαι") || 
                StemmerUtil.EndsWith(s, len, "ασαι") || 
                StemmerUtil.EndsWith(s, len, "αται") || 
                StemmerUtil.EndsWith(s, len, "ειτε") || 
                StemmerUtil.EndsWith(s, len, "εσαι") || 
                StemmerUtil.EndsWith(s, len, "εται") || 
                StemmerUtil.EndsWith(s, len, "ηδεσ") || 
                StemmerUtil.EndsWith(s, len, "ηδων") || 
                StemmerUtil.EndsWith(s, len, "ηθει") || 
                StemmerUtil.EndsWith(s, len, "ηκαν") || 
                StemmerUtil.EndsWith(s, len, "ησαν") || 
                StemmerUtil.EndsWith(s, len, "ησει") || 
                StemmerUtil.EndsWith(s, len, "ησεσ") || 
                StemmerUtil.EndsWith(s, len, "ομαι") || 
                StemmerUtil.EndsWith(s, len, "οταν")))
            {
                return len - 4;
            }

            if (len > 3 && (StemmerUtil.EndsWith(s, len, "αει") || 
                StemmerUtil.EndsWith(s, len, "εισ") || 
                StemmerUtil.EndsWith(s, len, "ηθω") || 
                StemmerUtil.EndsWith(s, len, "ησω") || 
                StemmerUtil.EndsWith(s, len, "ουν") || 
                StemmerUtil.EndsWith(s, len, "ουσ")))
            {
                return len - 3;
            }

            if (len > 2 && (StemmerUtil.EndsWith(s, len, "αν") || 
                StemmerUtil.EndsWith(s, len, "ασ") || 
                StemmerUtil.EndsWith(s, len, "αω") || 
                StemmerUtil.EndsWith(s, len, "ει") || 
                StemmerUtil.EndsWith(s, len, "εσ") || 
                StemmerUtil.EndsWith(s, len, "ησ") || 
                StemmerUtil.EndsWith(s, len, "οι") || 
                StemmerUtil.EndsWith(s, len, "οσ") || 
                StemmerUtil.EndsWith(s, len, "ου") || 
                StemmerUtil.EndsWith(s, len, "υσ") || 
                StemmerUtil.EndsWith(s, len, "ων")))
            {
                return len - 2;
            }

            if (len > 1 && EndsWithVowel(s, len))
            {
                return len - 1;
            }

            return len;
        }

        private static int Rule22(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (StemmerUtil.EndsWith(s, len, "εστερ") || 
                StemmerUtil.EndsWith(s, len, "εστατ"))
            {
                return len - 5;
            }

            if (StemmerUtil.EndsWith(s, len, "οτερ") || 
                StemmerUtil.EndsWith(s, len, "οτατ") || 
                StemmerUtil.EndsWith(s, len, "υτερ") || 
                StemmerUtil.EndsWith(s, len, "υτατ") || 
                StemmerUtil.EndsWith(s, len, "ωτερ") || 
                StemmerUtil.EndsWith(s, len, "ωτατ"))
            {
                return len - 4;
            }

            return len;
        }

        /// <summary>
        /// Checks if the word contained in the leading portion of char[] array , 
        /// ends with the suffix given as parameter.
        /// </summary>
        /// <param name="s"> A char[] array that represents a word. </param>
        /// <param name="len"> The length of the char[] array. </param>
        /// <param name="suffix"> A <see cref="string"/> object to check if the word given ends with these characters. </param>
        /// <returns> True if the word ends with the suffix given , false otherwise. </returns>
        private static bool EndsWith(char[] s, int len, string suffix) // LUCENENET: CA1822: Mark members as static
        {
            int suffixLen = suffix.Length;
            if (suffixLen > len)
            {
                return false;
            }
            for (int i = suffixLen - 1; i >= 0; i--)
            {
                if (s[len - (suffixLen - i)] != suffix[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the word contained in the leading portion of <see cref="T:char[]"/> array , 
        /// ends with a Greek vowel.
        /// </summary>
        /// <param name="s"> A <see cref="T:char[]"/> array that represents a word. </param>
        /// <param name="len"> The length of the <see cref="T:char[]"/> array. </param>
        /// <returns> True if the word contained in the leading portion of <see cref="T:char[]"/> array , 
        /// ends with a vowel , false otherwise. </returns>
        private static bool EndsWithVowel(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len == 0)
            {
                return false;
            }
            switch (s[len - 1])
            {
                case 'α':
                case 'ε':
                case 'η':
                case 'ι':
                case 'ο':
                case 'υ':
                case 'ω':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the word contained in the leading portion of <see cref="T:char[]"/> array , 
        /// ends with a Greek vowel.
        /// </summary>
        /// <param name="s"> A <see cref="T:char[]"/> array that represents a word. </param>
        /// <param name="len"> The length of the <see cref="T:char[]"/> array. </param>
        /// <returns> True if the word contained in the leading portion of <see cref="T:char[]"/> array , 
        /// ends with a vowel , false otherwise. </returns>
        private static bool EndsWithVowelNoY(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len == 0)
            {
                return false;
            }
            switch (s[len - 1])
            {
                case 'α':
                case 'ε':
                case 'η':
                case 'ι':
                case 'ο':
                case 'ω':
                    return true;
                default:
                    return false;
            }
        }
    }
}