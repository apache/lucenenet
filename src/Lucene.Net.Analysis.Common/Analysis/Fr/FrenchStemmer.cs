// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Analysis.Fr
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
    /// A stemmer for French words. 
    /// <para/>
    /// The algorithm is based on the work of
    /// Dr Martin Porter on his snowball project<para/>
    /// refer to http://snowball.sourceforge.net/french/stemmer.html
    /// (French stemming algorithm) for details
    /// </summary>
    /// @deprecated Use <see cref="Tartarus.Snowball.Ext.FrenchStemmer"/> instead, 
    /// which has the same functionality. This filter will be removed in Lucene 4.0
    [Obsolete("Use FrenchStemmer instead, which has the same functionality.")]
    public class FrenchStemmer
    {
        private static readonly CultureInfo locale = new CultureInfo("fr-FR");

        /// <summary>
        /// Buffer for the terms while stemming them.
        /// </summary>
        private StringBuilder sb = new StringBuilder();

        /// <summary>
        /// A temporary buffer, used to reconstruct R2
        /// </summary>
        private readonly StringBuilder tb = new StringBuilder();

        /// <summary>
        /// Region R0 is equal to the whole buffer
        /// </summary>
        private string R0;

        /// <summary>
        /// Region RV
        /// "If the word begins with two vowels, RV is the region after the third letter,
        /// otherwise the region after the first vowel not at the beginning of the word,
        /// or the end of the word if these positions cannot be found."
        /// </summary>
        private string RV;

        /// <summary>
        /// Region R1
        /// "R1 is the region after the first non-vowel following a vowel
        /// or is the null region at the end of the word if there is no such non-vowel"
        /// </summary>
        private string R1;

        /// <summary>
        /// Region R2
        /// "R2 is the region after the first non-vowel in R1 following a vowel
        /// or is the null region at the end of the word if there is no such non-vowel"
        /// </summary>
        private string R2;


        /// <summary>
        /// Set to true if we need to perform step 2
        /// </summary>
        private bool suite;

        /// <summary>
        /// Set to true if the buffer was modified
        /// </summary>
        private bool modified;


        /// <summary>
        /// Stems the given term to a unique <c>discriminator</c>.
        /// </summary>
        /// <param name="term"> The term that should be stemmed </param>
        /// <returns> Discriminator for <paramref name="term"/> </returns>
        protected internal virtual string Stem(string term)
        {
            if (!IsStemmable(term))
            {
                return term;
            }

            // Use lowercase for medium stemming.
            term = locale.TextInfo.ToLower(term);

            // Reset the StringBuilder.
            sb.Remove(0, sb.Length);
            sb.Insert(0, term);

            // reset the booleans
            modified = false;
            suite = false;

            sb = TreatVowels(sb);

            SetStrings();

            Step1();

            if (!modified || suite)
            {
                if (RV != null)
                {
                    suite = Step2a();
                    if (!suite)
                    {
                        Step2b();
                    }
                }
            }

            if (modified || suite)
            {
                Step3();
            }
            else
            {
                Step4();
            }

            Step5();

            Step6();

            return sb.ToString();
        }

        /// <summary>
        /// Sets the search region strings
        /// it needs to be done each time the buffer was modified
        /// </summary>
        private void SetStrings()
        {
            // set the strings
            R0 = sb.ToString();
            RV = RetrieveRV(sb);
            R1 = RetrieveR(sb);
            if (R1 != null)
            {
                tb.Remove(0, tb.Length);
                tb.Insert(0, R1);
                R2 = RetrieveR(tb);
            }
            else
            {
                R2 = null;
            }
        }

        /// <summary>
        /// First step of the Porter Algorithm<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step1()
        {
            string[] suffix = new string[] { "ances", "iqUes", "ismes", "ables", "istes", "ance", "iqUe", "isme", "able", "iste" };
            DeleteFrom(R2, suffix);

            ReplaceFrom(R2, new string[] { "logies", "logie" }, "log");
            ReplaceFrom(R2, new string[] { "usions", "utions", "usion", "ution" }, "u");
            ReplaceFrom(R2, new string[] { "ences", "ence" }, "ent");

            string[] search = new string[] { "atrices", "ateurs", "ations", "atrice", "ateur", "ation" };
            DeleteButSuffixFromElseReplace(R2, search, "ic", true, R0, "iqU");

            DeleteButSuffixFromElseReplace(R2, new string[] { "ements", "ement" }, "eus", false, R0, "eux");
            DeleteButSuffixFrom(R2, new string[] { "ements", "ement" }, "ativ", false);
            DeleteButSuffixFrom(R2, new string[] { "ements", "ement" }, "iv", false);
            DeleteButSuffixFrom(R2, new string[] { "ements", "ement" }, "abl", false);
            DeleteButSuffixFrom(R2, new string[] { "ements", "ement" }, "iqU", false);

            DeleteFromIfTestVowelBeforeIn(R1, new string[] { "issements", "issement" }, false, R0);
            DeleteFrom(RV, new string[] { "ements", "ement" });

            DeleteButSuffixFromElseReplace(R2, new string[] { "ités", "ité" }, "abil", false, R0, "abl");
            DeleteButSuffixFromElseReplace(R2, new string[] { "ités", "ité" }, "ic", false, R0, "iqU");
            DeleteButSuffixFrom(R2, new string[] { "ités", "ité" }, "iv", true);

            string[] autre = new string[] { "ifs", "ives", "if", "ive" };
            DeleteButSuffixFromElseReplace(R2, autre, "icat", false, R0, "iqU");
            DeleteButSuffixFromElseReplace(R2, autre, "at", true, R2, "iqU");

            ReplaceFrom(R0, new string[] { "eaux" }, "eau");

            ReplaceFrom(R1, new string[] { "aux" }, "al");

            DeleteButSuffixFromElseReplace(R2, new string[] { "euses", "euse" }, "", true, R1, "eux");

            DeleteFrom(R2, new string[] { "eux" });

            // if one of the next steps is performed, we will need to perform step2a
            bool temp = false;
            temp = ReplaceFrom(RV, new string[] { "amment" }, "ant");
            if (temp == true)
            {
                suite = true;
            }
            temp = ReplaceFrom(RV, new string[] { "emment" }, "ent");
            if (temp == true)
            {
                suite = true;
            }
            temp = DeleteFromIfTestVowelBeforeIn(RV, new string[] { "ments", "ment" }, true, RV);
            if (temp == true)
            {
                suite = true;
            }

        }

        /// <summary>
        /// Second step (A) of the Porter Algorithm<para/>
        /// Will be performed if nothing changed from the first step
        /// or changed were done in the amment, emment, ments or ment suffixes<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        /// <returns> true if something changed in the <see cref="StringBuilder"/> </returns>
        private bool Step2a()
        {
            string[] search = new string[] { "îmes", "îtes", "iraIent", "irait", "irais", "irai", "iras", "ira", "irent", "iriez", "irez", "irions", "irons", "iront", "issaIent", "issais", "issantes", "issante", "issants", "issant", "issait", "issais", "issions", "issons", "issiez", "issez", "issent", "isses", "isse", "ir", "is", "ît", "it", "ies", "ie", "i" };
            return DeleteFromIfTestVowelBeforeIn(RV, search, false, RV);
        }

        /// <summary>
        /// Second step (B) of the Porter Algorithm<para/>
        /// Will be performed if step 2 A was performed unsuccessfully<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step2b()
        {
            string[] suffix = new string[] { "eraIent", "erais", "erait", "erai", "eras", "erions", "eriez", "erons", "eront", "erez", "èrent", "era", "ées", "iez", "ée", "és", "er", "ez", "é" };
            DeleteFrom(RV, suffix);

            string[] search = new string[] { "assions", "assiez", "assent", "asses", "asse", "aIent", "antes", "aIent", "Aient", "ante", "âmes", "âtes", "ants", "ant", "ait", "aît", "ais", "Ait", "Aît", "Ais", "ât", "as", "ai", "Ai", "a" };
            DeleteButSuffixFrom(RV, search, "e", true);

            DeleteFrom(R2, new string[] { "ions" });
        }

        /// <summary>
        /// Third step of the Porter Algorithm<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step3()
        {
            if (sb.Length > 0)
            {
                char ch = sb[sb.Length - 1];
                if (ch == 'Y')
                {
                    sb[sb.Length - 1] = 'i';
                    SetStrings();
                }
                else if (ch == 'ç')
                {
                    sb[sb.Length - 1] = 'c';
                    SetStrings();
                }
            }
        }

        /// <summary>
        /// Fourth step of the Porter Algorithm<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step4()
        {
            if (sb.Length > 1)
            {
                char ch = sb[sb.Length - 1];
                if (ch == 's')
                {
                    char b = sb[sb.Length - 2];
                    if (b != 'a' && b != 'i' && b != 'o' && b != 'u' && b != 'è' && b != 's')
                    {
                        sb.Remove(sb.Length - 1, sb.Length - (sb.Length - 1));
                        SetStrings();
                    }
                }
            }
            bool found = DeleteFromIfPrecededIn(R2, new string[] { "ion" }, RV, "s");
            if (!found)
            {
                found = DeleteFromIfPrecededIn(R2, new string[] { "ion" }, RV, "t");
            }

            ReplaceFrom(RV, new string[] { "Ière", "ière", "Ier", "ier" }, "i");
            DeleteFrom(RV, new string[] { "e" });
            DeleteFromIfPrecededIn(RV, new string[] { "ë" }, R0, "gu");
        }

        /// <summary>
        /// Fifth step of the Porter Algorithm<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step5()
        {
            if (R0 != null)
            {
                if (R0.EndsWith("enn", StringComparison.Ordinal) || R0.EndsWith("onn", StringComparison.Ordinal) || R0.EndsWith("ett", StringComparison.Ordinal) || R0.EndsWith("ell", StringComparison.Ordinal) || R0.EndsWith("eill", StringComparison.Ordinal))
                {
                    sb.Remove(sb.Length - 1, sb.Length - (sb.Length - 1));
                    SetStrings();
                }
            }
        }

        /// <summary>
        /// Sixth (and last!) step of the Porter Algorithm<para/>
        /// refer to http://snowball.sourceforge.net/french/stemmer.html for an explanation
        /// </summary>
        private void Step6()
        {
            if (R0 != null && R0.Length > 0)
            {
                bool seenVowel = false;
                bool seenConson = false;
                int pos = -1;
                for (int i = R0.Length - 1; i > -1; i--)
                {
                    char ch = R0[i];
                    if (IsVowel(ch))
                    {
                        if (!seenVowel)
                        {
                            if (ch == 'é' || ch == 'è')
                            {
                                pos = i;
                                break;
                            }
                        }
                        seenVowel = true;
                    }
                    else
                    {
                        if (seenVowel)
                        {
                            break;
                        }
                        else
                        {
                            seenConson = true;
                        }
                    }
                }
                if (pos > -1 && seenConson && !seenVowel)
                {
                    sb[pos] = 'e';
                }
            }
        }

        /// <summary>
        /// Delete a suffix searched in zone "source" if zone "from" contains prefix + search string
        /// </summary>
        /// <param name="source"> the primary source zone for search </param>
        /// <param name="search"> the strings to search for suppression </param>
        /// <param name="from"> the secondary source zone for search </param>
        /// <param name="prefix"> the prefix to add to the search string to test </param>
        /// <returns> true if modified </returns>
        private bool DeleteFromIfPrecededIn(string source, string[] search, string from, string prefix)
        {
            bool found = false;
            if (source != null)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (source.EndsWith(search[i], StringComparison.Ordinal))
                    {
                        if (from != null && from.EndsWith(prefix + search[i], StringComparison.Ordinal))
                        {
                            sb.Remove(sb.Length - search[i].Length, sb.Length - (sb.Length - search[i].Length));
                            found = true;
                            SetStrings();
                            break;
                        }
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Delete a suffix searched in zone "source" if the preceding letter is (or isn't) a vowel
        /// </summary>
        /// <param name="source"> the primary source zone for search </param>
        /// <param name="search"> the strings to search for suppression </param>
        /// <param name="vowel"> true if we need a vowel before the search string </param>
        /// <param name="from"> the secondary source zone for search (where vowel could be) </param>
        /// <returns> true if modified </returns>
        private bool DeleteFromIfTestVowelBeforeIn(string source, string[] search, bool vowel, string from)
        {
            bool found = false;
            if (source != null && from != null)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (source.EndsWith(search[i], StringComparison.Ordinal))
                    {
                        if ((search[i].Length + 1) <= from.Length)
                        {
                            bool test = IsVowel(sb[sb.Length - (search[i].Length + 1)]);
                            if (test == vowel)
                            {
                                sb.Remove(sb.Length - search[i].Length, sb.Length - (sb.Length - search[i].Length));
                                modified = true;
                                found = true;
                                SetStrings();
                                break;
                            }
                        }
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Delete a suffix searched in zone "source" if preceded by the prefix
        /// </summary>
        /// <param name="source"> the primary source zone for search </param>
        /// <param name="search"> the strings to search for suppression </param>
        /// <param name="prefix"> the prefix to add to the search string to test </param>
        /// <param name="without"> true if it will be deleted even without prefix found </param>
        private void DeleteButSuffixFrom(string source, string[] search, string prefix, bool without)
        {
            if (source != null)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (source.EndsWith(prefix + search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - (prefix.Length + search[i].Length), sb.Length - (sb.Length - (prefix.Length + search[i].Length)));
                        modified = true;
                        SetStrings();
                        break;
                    }
                    else if (without && source.EndsWith(search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - search[i].Length, sb.Length - (sb.Length - search[i].Length));
                        modified = true;
                        SetStrings();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Delete a suffix searched in zone "source" if preceded by prefix<para/>
        /// or replace it with the replace string if preceded by the prefix in the zone "from"<para/>
        /// or delete the suffix if specified
        /// </summary>
        /// <param name="source"> the primary source zone for search </param>
        /// <param name="search"> the strings to search for suppression </param>
        /// <param name="prefix"> the prefix to add to the search string to test </param>
        /// <param name="without"> true if it will be deleted even without prefix found </param>
        /// <param name="from"> the secondary source zone for search </param>
        /// <param name="replace"> the replacement string </param>
        private void DeleteButSuffixFromElseReplace(string source, string[] search, string prefix, bool without, string from, string replace)
        {
            if (source != null)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (source.EndsWith(prefix + search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - (prefix.Length + search[i].Length), sb.Length - (sb.Length - (prefix.Length + search[i].Length)));
                        modified = true;
                        SetStrings();
                        break;
                    }
                    else if (from != null && from.EndsWith(prefix + search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - (prefix.Length + search[i].Length), sb.Length - (sb.Length - (prefix.Length + search[i].Length))).Insert(sb.Length - (prefix.Length + search[i].Length), replace);
                        modified = true;
                        SetStrings();
                        break;
                    }
                    else if (without && source.EndsWith(search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - search[i].Length, sb.Length - (sb.Length - search[i].Length));
                        modified = true;
                        SetStrings();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Replace a search string with another within the source zone
        /// </summary>
        /// <param name="source"> the source zone for search </param>
        /// <param name="search"> the strings to search for replacement </param>
        /// <param name="replace"> the replacement string </param>
        private bool ReplaceFrom(string source, string[] search, string replace)
        {
            bool found = false;
            if (source != null)
            {
                for (int i = 0; i < search.Length; i++)
                {
                    if (source.EndsWith(search[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - search[i].Length, sb.Length - (sb.Length - search[i].Length)).Insert(sb.Length - search[i].Length, replace);
                        modified = true;
                        found = true;
                        SetStrings();
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Delete a search string within the source zone
        /// </summary>
        /// <param name="source"> the source zone for search </param>
        /// <param name="suffix"> the strings to search for suppression </param>
        private void DeleteFrom(string source, string[] suffix)
        {
            if (source != null)
            {
                for (int i = 0; i < suffix.Length; i++)
                {
                    if (source.EndsWith(suffix[i], StringComparison.Ordinal))
                    {
                        sb.Remove(sb.Length - suffix[i].Length, sb.Length - (sb.Length - suffix[i].Length));
                        modified = true;
                        SetStrings();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Test if a char is a french vowel, including accentuated ones
        /// </summary>
        /// <param name="ch"> the char to test </param>
        /// <returns> true if the char is a vowel </returns>
        private bool IsVowel(char ch)
        {
            switch (ch)
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                case 'y':
                case 'â':
                case 'à':
                case 'ë':
                case 'é':
                case 'ê':
                case 'è':
                case 'ï':
                case 'î':
                case 'ô':
                case 'ü':
                case 'ù':
                case 'û':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retrieve the "R zone" (1 or 2 depending on the buffer) and return the corresponding string<para/>
        /// "R is the region after the first non-vowel following a vowel
        /// or is the null region at the end of the word if there is no such non-vowel" </summary>
        /// <param name="buffer"> the in buffer </param>
        /// <returns> the resulting string </returns>
        private string RetrieveR(StringBuilder buffer)
        {
            int len = buffer.Length;
            int pos = -1;
            for (int c = 0; c < len; c++)
            {
                if (IsVowel(buffer[c]))
                {
                    pos = c;
                    break;
                }
            }
            if (pos > -1)
            {
                int consonne = -1;
                for (int c = pos; c < len; c++)
                {
                    if (!IsVowel(buffer[c]))
                    {
                        consonne = c;
                        break;
                    }
                }
                if (consonne > -1 && (consonne + 1) < len)
                {
                    return buffer.ToString(consonne + 1, len - (consonne + 1));
                    //return StringHelperClass.SubstringSpecial(buffer, consonne+1, len);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve the "RV zone" from a buffer an return the corresponding string<para/>
        /// "If the word begins with two vowels, RV is the region after the third letter,
        /// otherwise the region after the first vowel not at the beginning of the word,
        /// or the end of the word if these positions cannot be found." </summary>
        /// <param name="buffer"> the in buffer </param>
        /// <returns> the resulting string </returns>
        private string RetrieveRV(StringBuilder buffer)
        {
            int len = buffer.Length;
            if (buffer.Length > 3)
            {
                if (IsVowel(buffer[0]) && IsVowel(buffer[1]))
                {
                    return buffer.ToString(3, len - 3);
                }
                else
                {
                    int pos = 0;
                    for (int c = 1; c < len; c++)
                    {
                        if (IsVowel(buffer[c]))
                        {
                            pos = c;
                            break;
                        }
                    }
                    if (pos + 1 < len)
                    {
                        return buffer.ToString(pos + 1, len - (pos + 1));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// Turns u and i preceded AND followed by a vowel to UpperCase<para/>
        /// Turns y preceded OR followed by a vowel to UpperCase<para/>
        /// Turns u preceded by q to UpperCase
        /// </summary>
        /// <param name="buffer"> the buffer to treat </param>
        /// <returns> the treated buffer </returns>
        private StringBuilder TreatVowels(StringBuilder buffer)
        {
            for (int c = 0; c < buffer.Length; c++)
            {
                char ch = buffer[c];

                if (c == 0) // first char
                {
                    if (buffer.Length > 1)
                    {
                        if (ch == 'y' && IsVowel(buffer[c + 1]))
                        {
                            buffer[c] = 'Y';
                        }
                    }
                }
                else if (c == buffer.Length - 1) // last char
                {
                    if (ch == 'u' && buffer[c - 1] == 'q')
                    {
                        buffer[c] = 'U';
                    }
                    if (ch == 'y' && IsVowel(buffer[c - 1]))
                    {
                        buffer[c] = 'Y';
                    }
                }
                else // other cases
                {
                    if (ch == 'u')
                    {
                        if (buffer[c - 1] == 'q')
                        {
                            buffer[c] = 'U';
                        }
                        else if (IsVowel(buffer[c - 1]) && IsVowel(buffer[c + 1]))
                        {
                            buffer[c] = 'U';
                        }
                    }
                    if (ch == 'i')
                    {
                        if (IsVowel(buffer[c - 1]) && IsVowel(buffer[c + 1]))
                        {
                            buffer[c] = 'I';
                        }
                    }
                    if (ch == 'y')
                    {
                        if (IsVowel(buffer[c - 1]) || IsVowel(buffer[c + 1]))
                        {
                            buffer[c] = 'Y';
                        }
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Checks a term if it can be processed correctly.
        /// </summary>
        /// <returns> true if, and only if, the given term consists in letters. </returns>
        private bool IsStemmable(string term)
        {
            bool upper = false;
            int first = -1;
            for (int c = 0; c < term.Length; c++)
            {
                // Discard terms that contain non-letter characters.
                if (!char.IsLetter(term[c]))
                {
                    return false;
                }
                // Discard terms that contain multiple uppercase letters.
                if (char.IsUpper(term[c]))
                {
                    if (upper)
                    {
                        return false;
                    }
                    // First encountered uppercase letter, set flag and save
                    // position.
                    else
                    {
                        first = c;
                        upper = true;
                    }
                }
            }
            // Discard the term if it contains a single uppercase letter that
            // is not starting the term.
            if (first > 0)
            {
                return false;
            }
            return true;
        }
    }
}