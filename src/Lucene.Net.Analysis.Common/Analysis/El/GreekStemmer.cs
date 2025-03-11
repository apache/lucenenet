// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;

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
            if (len > 9 && (EndsWith(s, len, "καθεστωτοσ") ||
                EndsWith(s, len, "καθεστωτων")))
            {
                return len - 4;
            }

            if (len > 8 && (EndsWith(s, len, "γεγονοτοσ") ||
                EndsWith(s, len, "γεγονοτων")))
            {
                return len - 4;
            }

            if (len > 8 && EndsWith(s, len, "καθεστωτα"))
            {
                return len - 3;
            }

            if (len > 7 && (EndsWith(s, len, "τατογιου") ||
                EndsWith(s, len, "τατογιων")))
            {
                return len - 4;
            }

            if (len > 7 && EndsWith(s, len, "γεγονοτα"))
            {
                return len - 3;
            }

            if (len > 7 && EndsWith(s, len, "καθεστωσ"))
            {
                return len - 2;
            }

            if (len > 6 && (EndsWith(s, len, "σκαγιου")) ||
                EndsWith(s, len, "σκαγιων") ||
                EndsWith(s, len, "ολογιου") ||
                EndsWith(s, len, "ολογιων") ||
                EndsWith(s, len, "κρεατοσ") ||
                EndsWith(s, len, "κρεατων") ||
                EndsWith(s, len, "περατοσ") ||
                EndsWith(s, len, "περατων") ||
                EndsWith(s, len, "τερατοσ") ||
                EndsWith(s, len, "τερατων"))
            {
                return len - 4;
            }

            if (len > 6 && EndsWith(s, len, "τατογια"))
            {
                return len - 3;
            }

            if (len > 6 && EndsWith(s, len, "γεγονοσ"))
            {
                return len - 2;
            }

            if (len > 5 && (EndsWith(s, len, "φαγιου") ||
                EndsWith(s, len, "φαγιων") ||
                EndsWith(s, len, "σογιου") ||
                EndsWith(s, len, "σογιων")))
            {
                return len - 4;
            }

            if (len > 5 && (EndsWith(s, len, "σκαγια") ||
                EndsWith(s, len, "ολογια") ||
                EndsWith(s, len, "κρεατα") ||
                EndsWith(s, len, "περατα") ||
                EndsWith(s, len, "τερατα")))
            {
                return len - 3;
            }

            if (len > 4 && (EndsWith(s, len, "φαγια") ||
                EndsWith(s, len, "σογια") ||
                EndsWith(s, len, "φωτοσ") ||
                EndsWith(s, len, "φωτων")))
            {
                return len - 3;
            }

            if (len > 4 && (EndsWith(s, len, "κρεασ") ||
                EndsWith(s, len, "περασ") ||
                EndsWith(s, len, "τερασ")))
            {
                return len - 2;
            }

            if (len > 3 && EndsWith(s, len, "φωτα"))
            {
                return len - 2;
            }

            if (len > 2 && EndsWith(s, len, "φωσ"))
            {
                return len - 1;
            }

            return len;
        }

        private static int Rule1(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && (EndsWith(s, len, "αδεσ") ||
                EndsWith(s, len, "αδων")))
            {
                len -= 4;
                if (!(EndsWith(s, len, "οκ") ||
                    EndsWith(s, len, "μαμ") ||
                    EndsWith(s, len, "μαν") ||
                    EndsWith(s, len, "μπαμπ") ||
                    EndsWith(s, len, "πατερ") ||
                    EndsWith(s, len, "γιαγι") ||
                    EndsWith(s, len, "νταντ") ||
                    EndsWith(s, len, "κυρ") ||
                    EndsWith(s, len, "θει") ||
                    EndsWith(s, len, "πεθερ")))
                {
                    len += 2; // add back -αδ
                }
            }
            return len;
        }

        private static int Rule2(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && (EndsWith(s, len, "εδεσ") ||
                EndsWith(s, len, "εδων")))
            {
                len -= 4;
                if (EndsWith(s, len, "οπ") ||
                    EndsWith(s, len, "ιπ") ||
                    EndsWith(s, len, "εμπ") ||
                    EndsWith(s, len, "υπ") ||
                    EndsWith(s, len, "γηπ") ||
                    EndsWith(s, len, "δαπ") ||
                    EndsWith(s, len, "κρασπ") ||
                    EndsWith(s, len, "μιλ"))
                {
                    len += 2; // add back -εδ
                }
            }
            return len;
        }

        private static int Rule3(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && (EndsWith(s, len, "ουδεσ") ||
                EndsWith(s, len, "ουδων")))
            {
                len -= 5;
                if (EndsWith(s, len, "αρκ") ||
                    EndsWith(s, len, "καλιακ") ||
                    EndsWith(s, len, "πεταλ") ||
                    EndsWith(s, len, "λιχ") ||
                    EndsWith(s, len, "πλεξ") ||
                    EndsWith(s, len, "σκ") ||
                    EndsWith(s, len, "σ") ||
                    EndsWith(s, len, "φλ") ||
                    EndsWith(s, len, "φρ") ||
                    EndsWith(s, len, "βελ") ||
                    EndsWith(s, len, "λουλ") ||
                    EndsWith(s, len, "χν") ||
                    EndsWith(s, len, "σπ") ||
                    EndsWith(s, len, "τραγ") ||
                    EndsWith(s, len, "φε"))
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
            if (len > 3 && (EndsWith(s, len, "εωσ") ||
                EndsWith(s, len, "εων")))
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
            if (len > 2 && EndsWith(s, len, "ια"))
            {
                len -= 2;
                if (EndsWithVowel(s, len))
                {
                    len++; // add back -ι
                }
            }
            else if (len > 3 && (EndsWith(s, len, "ιου") ||
                EndsWith(s, len, "ιων")))
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
            if (len > 3 && (EndsWith(s, len, "ικα") ||
                EndsWith(s, len, "ικο")))
            {
                len -= 3;
                removed = true;
            }
            else if (len > 4 && (EndsWith(s, len, "ικου") ||
                EndsWith(s, len, "ικων")))
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
            if (len == 5 && EndsWith(s, len, "αγαμε"))
            {
                return len - 1;
            }

            if (len > 7 && EndsWith(s, len, "ηθηκαμε"))
            {
                len -= 7;
            }
            else if (len > 6 && EndsWith(s, len, "ουσαμε"))
            {
                len -= 6;
            }
            else if (len > 5 && (EndsWith(s, len, "αγαμε") ||
                EndsWith(s, len, "ησαμε") ||
                EndsWith(s, len, "ηκαμε")))
            {
                len -= 5;
            }

            if (len > 3 && EndsWith(s, len, "αμε"))
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

            if (len > 8 && EndsWith(s, len, "ιουντανε"))
            {
                len -= 8;
                removed = true;
            }
            else if (len > 7 && EndsWith(s, len, "ιοντανε") ||
                EndsWith(s, len, "ουντανε") ||
                EndsWith(s, len, "ηθηκανε"))
            {
                len -= 7;
                removed = true;
            }
            else if (len > 6 && EndsWith(s, len, "ιοτανε") ||
                EndsWith(s, len, "οντανε") ||
                EndsWith(s, len, "ουσανε"))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 5 && EndsWith(s, len, "αγανε") ||
                EndsWith(s, len, "ησανε") ||
                EndsWith(s, len, "οτανε") ||
                EndsWith(s, len, "ηκανε"))
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

            if (len > 3 && EndsWith(s, len, "ανε"))
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
            if (len > 5 && EndsWith(s, len, "ησετε"))
            {
                len -= 5;
            }

            if (len > 3 && EndsWith(s, len, "ετε"))
            {
                len -= 3;
                if (exc9.Contains(s, 0, len) ||
                    EndsWithVowelNoY(s, len) ||
                    EndsWith(s, len, "οδ") ||
                    EndsWith(s, len, "αιρ") ||
                    EndsWith(s, len, "φορ") ||
                    EndsWith(s, len, "ταθ") ||
                    EndsWith(s, len, "διαθ") ||
                    EndsWith(s, len, "σχ") ||
                    EndsWith(s, len, "ενδ") ||
                    EndsWith(s, len, "ευρ") ||
                    EndsWith(s, len, "τιθ") ||
                    EndsWith(s, len, "υπερθ") ||
                    EndsWith(s, len, "ραθ") ||
                    EndsWith(s, len, "ενθ") ||
                    EndsWith(s, len, "ροθ") ||
                    EndsWith(s, len, "σθ") ||
                    EndsWith(s, len, "πυρ") ||
                    EndsWith(s, len, "αιν") ||
                    EndsWith(s, len, "συνδ") ||
                    EndsWith(s, len, "συν") ||
                    EndsWith(s, len, "συνθ") ||
                    EndsWith(s, len, "χωρ") ||
                    EndsWith(s, len, "πον") ||
                    EndsWith(s, len, "βρ") ||
                    EndsWith(s, len, "καθ") ||
                    EndsWith(s, len, "ευθ") ||
                    EndsWith(s, len, "εκθ") ||
                    EndsWith(s, len, "νετ") ||
                    EndsWith(s, len, "ρον") ||
                    EndsWith(s, len, "αρκ") ||
                    EndsWith(s, len, "βαρ") ||
                    EndsWith(s, len, "βολ") ||
                    EndsWith(s, len, "ωφελ"))
                {
                    len += 2; // add back -ετ
                }
            }

            return len;
        }

        private static int Rule10(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 5 && (EndsWith(s, len, "οντασ") || EndsWith(s, len, "ωντασ")))
            {
                len -= 5;
                if (len == 3 && EndsWith(s, len, "αρχ"))
                {
                    len += 3; // add back *ντ
                    s[len - 3] = 'ο';
                }
                if (EndsWith(s, len, "κρε"))
                {
                    len += 3; // add back *ντ
                    s[len - 3] = 'ω';
                }
            }

            return len;
        }

        private static int Rule11(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6 && EndsWith(s, len, "ομαστε"))
            {
                len -= 6;
                if (len == 2 && EndsWith(s, len, "ον"))
                {
                    len += 5; // add back -ομαστ
                }
            }
            else if (len > 7 && EndsWith(s, len, "ιομαστε"))
            {
                len -= 7;
                if (len == 2 && EndsWith(s, len, "ον"))
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
            if (len > 5 && EndsWith(s, len, "ιεστε"))
            {
                len -= 5;
                if (exc12a.Contains(s, 0, len))
                {
                    len += 4; // add back -ιεστ
                }
            }

            if (len > 4 && EndsWith(s, len, "εστε"))
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
            if (len > 6 && EndsWith(s, len, "ηθηκεσ"))
            {
                len -= 6;
            }
            else if (len > 5 && (EndsWith(s, len, "ηθηκα") || EndsWith(s, len, "ηθηκε")))
            {
                len -= 5;
            }

            bool removed = false;

            if (len > 4 && EndsWith(s, len, "ηκεσ"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (EndsWith(s, len, "ηκα") || EndsWith(s, len, "ηκε")))
            {
                len -= 3;
                removed = true;
            }

            if (removed && (exc13.Contains(s, 0, len) ||
                EndsWith(s, len, "σκωλ") ||
                EndsWith(s, len, "σκουλ") ||
                EndsWith(s, len, "ναρθ") ||
                EndsWith(s, len, "σφ") ||
                EndsWith(s, len, "οθ") ||
                EndsWith(s, len, "πιθ")))
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

            if (len > 5 && EndsWith(s, len, "ουσεσ"))
            {
                len -= 5;
                removed = true;
            }
            else if (len > 4 && (EndsWith(s, len, "ουσα") ||
                EndsWith(s, len, "ουσε")))
            {
                len -= 4;
                removed = true;
            }

            if (removed && (exc14.Contains(s, 0, len) ||
                EndsWithVowel(s, len) ||
                EndsWith(s, len, "ποδαρ") ||
                EndsWith(s, len, "βλεπ") ||
                EndsWith(s, len, "πανταχ") ||
                EndsWith(s, len, "φρυδ") ||
                EndsWith(s, len, "μαντιλ") ||
                EndsWith(s, len, "μαλλ") ||
                EndsWith(s, len, "κυματ") ||
                EndsWith(s, len, "λαχ") ||
                EndsWith(s, len, "ληγ") ||
                EndsWith(s, len, "φαγ") ||
                EndsWith(s, len, "ομ") ||
                EndsWith(s, len, "πρωτ")))
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
            if (len > 4 && EndsWith(s, len, "αγεσ"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (EndsWith(s, len, "αγα") || EndsWith(s, len, "αγε")))
            {
                len -= 3;
                removed = true;
            }

            if (removed)
            {
                bool cond1 = exc15a.Contains(s, 0, len) ||
                    EndsWith(s, len, "οφ") ||
                    EndsWith(s, len, "πελ") ||
                    EndsWith(s, len, "χορτ") ||
                    EndsWith(s, len, "λλ") ||
                    EndsWith(s, len, "σφ") ||
                    EndsWith(s, len, "ρπ") ||
                    EndsWith(s, len, "φρ") ||
                    EndsWith(s, len, "πρ") ||
                    EndsWith(s, len, "λοχ") ||
                    EndsWith(s, len, "σμην");

                bool cond2 = exc15b.Contains(s, 0, len) || EndsWith(s, len, "κολλ");

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
            if (len > 4 && EndsWith(s, len, "ησου"))
            {
                len -= 4;
                removed = true;
            }
            else if (len > 3 && (EndsWith(s, len, "ησε") || EndsWith(s, len, "ησα")))
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
            if (len > 4 && EndsWith(s, len, "ηστε"))
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

            if (len > 6 && (EndsWith(s, len, "ησουνε") || EndsWith(s, len, "ηθουνε")))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 4 && EndsWith(s, len, "ουνε"))
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

            if (len > 6 && (EndsWith(s, len, "ησουμε") || EndsWith(s, len, "ηθουμε")))
            {
                len -= 6;
                removed = true;
            }
            else if (len > 4 && EndsWith(s, len, "ουμε"))
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
            if (len > 5 && (EndsWith(s, len, "ματων") || EndsWith(s, len, "ματοσ")))
            {
                len -= 3;
            }
            else if (len > 4 && EndsWith(s, len, "ματα"))
            {
                len -= 2;
            }
            return len;
        }

        private static int Rule21(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 9 && EndsWith(s, len, "ιοντουσαν"))
            {
                return len - 9;
            }

            if (len > 8 && (EndsWith(s, len, "ιομασταν") ||
                EndsWith(s, len, "ιοσασταν") ||
                EndsWith(s, len, "ιουμαστε") ||
                EndsWith(s, len, "οντουσαν")))
            {
                return len - 8;
            }

            if (len > 7 && (EndsWith(s, len, "ιεμαστε") ||
                EndsWith(s, len, "ιεσαστε") ||
                EndsWith(s, len, "ιομουνα") ||
                EndsWith(s, len, "ιοσαστε") ||
                EndsWith(s, len, "ιοσουνα") ||
                EndsWith(s, len, "ιουνται") ||
                EndsWith(s, len, "ιουνταν") ||
                EndsWith(s, len, "ηθηκατε") ||
                EndsWith(s, len, "ομασταν") ||
                EndsWith(s, len, "οσασταν") ||
                EndsWith(s, len, "ουμαστε")))
            {
                return len - 7;
            }

            if (len > 6 && (EndsWith(s, len, "ιομουν") ||
                EndsWith(s, len, "ιονταν") ||
                EndsWith(s, len, "ιοσουν") ||
                EndsWith(s, len, "ηθειτε") ||
                EndsWith(s, len, "ηθηκαν") ||
                EndsWith(s, len, "ομουνα") ||
                EndsWith(s, len, "οσαστε") ||
                EndsWith(s, len, "οσουνα") ||
                EndsWith(s, len, "ουνται") ||
                EndsWith(s, len, "ουνταν") ||
                EndsWith(s, len, "ουσατε")))
            {
                return len - 6;
            }

            if (len > 5 && (EndsWith(s, len, "αγατε") ||
                EndsWith(s, len, "ιεμαι") ||
                EndsWith(s, len, "ιεται") ||
                EndsWith(s, len, "ιεσαι") ||
                EndsWith(s, len, "ιοταν") ||
                EndsWith(s, len, "ιουμα") ||
                EndsWith(s, len, "ηθεισ") ||
                EndsWith(s, len, "ηθουν") ||
                EndsWith(s, len, "ηκατε") ||
                EndsWith(s, len, "ησατε") ||
                EndsWith(s, len, "ησουν") ||
                EndsWith(s, len, "ομουν") ||
                EndsWith(s, len, "ονται") ||
                EndsWith(s, len, "ονταν") ||
                EndsWith(s, len, "οσουν") ||
                EndsWith(s, len, "ουμαι") ||
                EndsWith(s, len, "ουσαν")))
            {
                return len - 5;
            }

            if (len > 4 && (EndsWith(s, len, "αγαν") ||
                EndsWith(s, len, "αμαι") ||
                EndsWith(s, len, "ασαι") ||
                EndsWith(s, len, "αται") ||
                EndsWith(s, len, "ειτε") ||
                EndsWith(s, len, "εσαι") ||
                EndsWith(s, len, "εται") ||
                EndsWith(s, len, "ηδεσ") ||
                EndsWith(s, len, "ηδων") ||
                EndsWith(s, len, "ηθει") ||
                EndsWith(s, len, "ηκαν") ||
                EndsWith(s, len, "ησαν") ||
                EndsWith(s, len, "ησει") ||
                EndsWith(s, len, "ησεσ") ||
                EndsWith(s, len, "ομαι") ||
                EndsWith(s, len, "οταν")))
            {
                return len - 4;
            }

            if (len > 3 && (EndsWith(s, len, "αει") ||
                EndsWith(s, len, "εισ") ||
                EndsWith(s, len, "ηθω") ||
                EndsWith(s, len, "ησω") ||
                EndsWith(s, len, "ουν") ||
                EndsWith(s, len, "ουσ")))
            {
                return len - 3;
            }

            if (len > 2 && (EndsWith(s, len, "αν") ||
                EndsWith(s, len, "ασ") ||
                EndsWith(s, len, "αω") ||
                EndsWith(s, len, "ει") ||
                EndsWith(s, len, "εσ") ||
                EndsWith(s, len, "ησ") ||
                EndsWith(s, len, "οι") ||
                EndsWith(s, len, "οσ") ||
                EndsWith(s, len, "ου") ||
                EndsWith(s, len, "υσ") ||
                EndsWith(s, len, "ων")))
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
            if (EndsWith(s, len, "εστερ") ||
                EndsWith(s, len, "εστατ"))
            {
                return len - 5;
            }

            if (EndsWith(s, len, "οτερ") ||
                EndsWith(s, len, "οτατ") ||
                EndsWith(s, len, "υτερ") ||
                EndsWith(s, len, "υτατ") ||
                EndsWith(s, len, "ωτερ") ||
                EndsWith(s, len, "ωτατ"))
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
        private static bool EndsWith(ReadOnlySpan<char> s, int len, string suffix) // LUCENENET: CA1822: Mark members as static
            => s.Slice(0, len).EndsWith(suffix.AsSpan()); // LUCENENET specific - optimized for ReadOnlySpan<char>

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
