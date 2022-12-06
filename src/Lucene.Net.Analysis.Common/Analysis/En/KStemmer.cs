// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;

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

/*
This file was partially derived from the
original CIIR University of Massachusetts Amherst version of KStemmer.java (license for
the original shown below)
 */

/*
 Copyright © 2003,
 Center for Intelligent Information Retrieval,
 University of Massachusetts, Amherst.
 All rights reserved.

 Redistribution and use in source and binary forms, with or without modification,
 are permitted provided that the following conditions are met:

 1. Redistributions of source code must retain the above copyright notice, this
 list of conditions and the following disclaimer.

 2. Redistributions in binary form must reproduce the above copyright notice,
 this list of conditions and the following disclaimer in the documentation
 and/or other materials provided with the distribution.

 3. The names "Center for Intelligent Information Retrieval" and
 "University of Massachusetts" must not be used to endorse or promote products
 derived from this software without prior written permission. To obtain
 permission, contact info@ciir.cs.umass.edu.

 THIS SOFTWARE IS PROVIDED BY UNIVERSITY OF MASSACHUSETTS AND OTHER CONTRIBUTORS
 "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
 LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
 GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.
 */
namespace Lucene.Net.Analysis.En
{
    /// <summary>
    /// This class implements the Kstem algorithm
    /// </summary>
    /// <remarks>
    /// <para>Title: Kstemmer</para>
    /// <para>Description: This is a java version of Bob Krovetz' kstem stemmer</para>
    /// <para>Copyright: Copyright 2008, Luicid Imagination, Inc. </para>
    /// <para>Copyright: Copyright 2003, CIIR University of Massachusetts Amherst (http://ciir.cs.umass.edu) </para>
    /// </remarks>
    public class KStemmer
    {
        private const int MaxWordLen = 50;

        private static readonly string[] exceptionWords = new string[] { "aide", "bathe", "caste",
            "cute", "dame", "dime", "doge", "done", "dune", "envelope", "gage",
            "grille", "grippe", "lobe", "mane", "mare", "nape", "node", "pane",
            "pate", "plane", "pope", "programme", "quite", "ripe", "rote", "rune",
            "sage", "severe", "shoppe", "sine", "slime", "snipe", "steppe", "suite",
            "swinge", "tare", "tine", "tope", "tripe", "twine"
        };

        private static readonly string[][] directConflations = new string[][]
        {
            new string[] {"aging", "age"},
            new string[] {"going", "go"},
            new string[] {"goes", "go"},
            new string[] {"lying", "lie"},
            new string[] {"using", "use"},
            new string[] {"owing", "owe"},
            new string[] {"suing", "sue"},
            new string[] {"dying", "die"},
            new string[] {"tying", "tie"},
            new string[] {"vying", "vie"},
            new string[] {"aged", "age"},
            new string[] {"used", "use"},
            new string[] {"vied", "vie"},
            new string[] {"cued", "cue"},
            new string[] {"died", "die"},
            new string[] {"eyed", "eye"},
            new string[] {"hued", "hue"},
            new string[] {"iced", "ice"},
            new string[] {"lied", "lie"},
            new string[] {"owed", "owe"},
            new string[] {"sued", "sue"},
            new string[] {"toed", "toe"},
            new string[] {"tied", "tie"},
            new string[] {"does", "do"},
            new string[] {"doing", "do"},
            new string[] {"aeronautical", "aeronautics"},
            new string[] {"mathematical", "mathematics"},
            new string[] {"political", "politics"},
            new string[] {"metaphysical", "metaphysics"},
            new string[] {"cylindrical", "cylinder"},
            new string[] {"nazism", "nazi"},
            new string[] {"ambiguity", "ambiguous"},
            new string[] {"barbarity", "barbarous"},
            new string[] {"credulity", "credulous"},
            new string[] {"generosity", "generous"},
            new string[] {"spontaneity", "spontaneous"},
            new string[] {"unanimity", "unanimous"},
            new string[] {"voracity", "voracious"},
            new string[] {"fled", "flee"},
            new string[] {"miscarriage", "miscarry"}
        };

        private static readonly string[][] countryNationality = new string[][]
        {
            new string[] {"afghan", "afghanistan"},
            new string[] {"african", "africa"},
            new string[] {"albanian", "albania"},
            new string[] {"algerian", "algeria"},
            new string[] {"american", "america"},
            new string[] {"andorran", "andorra"},
            new string[] {"angolan", "angola"},
            new string[] {"arabian", "arabia"},
            new string[] {"argentine", "argentina"},
            new string[] {"armenian", "armenia"},
            new string[] {"asian", "asia"},
            new string[] {"australian", "australia"},
            new string[] {"austrian", "austria"},
            new string[] {"azerbaijani", "azerbaijan"},
            new string[] {"azeri", "azerbaijan"},
            new string[] {"bangladeshi", "bangladesh"},
            new string[] {"belgian", "belgium"},
            new string[] {"bermudan", "bermuda"},
            new string[] {"bolivian", "bolivia"},
            new string[] {"bosnian", "bosnia"},
            new string[] {"botswanan", "botswana"},
            new string[] {"brazilian", "brazil"},
            new string[] {"british", "britain"},
            new string[] {"bulgarian", "bulgaria"},
            new string[] {"burmese", "burma"},
            new string[] {"californian", "california"},
            new string[] {"cambodian", "cambodia"},
            new string[] {"canadian", "canada"},
            new string[] {"chadian", "chad"},
            new string[] {"chilean", "chile"},
            new string[] {"chinese", "china"},
            new string[] {"colombian", "colombia"},
            new string[] {"croat", "croatia"},
            new string[] {"croatian", "croatia"},
            new string[] {"cuban", "cuba"},
            new string[] {"cypriot", "cyprus"},
            new string[] {"czechoslovakian", "czechoslovakia"},
            new string[] {"danish", "denmark"},
            new string[] {"egyptian", "egypt"},
            new string[] {"equadorian", "equador"},
            new string[] {"eritrean", "eritrea"},
            new string[] {"estonian", "estonia"},
            new string[] {"ethiopian", "ethiopia"},
            new string[] {"european", "europe"},
            new string[] {"fijian", "fiji"},
            new string[] {"filipino", "philippines"},
            new string[] {"finnish", "finland"},
            new string[] {"french", "france"},
            new string[] {"gambian", "gambia"},
            new string[] {"georgian", "georgia"},
            new string[] {"german", "germany"},
            new string[] {"ghanian", "ghana"},
            new string[] {"greek", "greece"},
            new string[] {"grenadan", "grenada"},
            new string[] {"guamian", "guam"},
            new string[] {"guatemalan", "guatemala"},
            new string[] {"guinean", "guinea"},
            new string[] {"guyanan", "guyana"},
            new string[] {"haitian", "haiti"},
            new string[] {"hawaiian", "hawaii"},
            new string[] {"holland", "dutch"},
            new string[] {"honduran", "honduras"},
            new string[] {"hungarian", "hungary"},
            new string[] {"icelandic", "iceland"},
            new string[] {"indonesian", "indonesia"},
            new string[] {"iranian", "iran"},
            new string[] {"iraqi", "iraq"},
            new string[] {"iraqui", "iraq"},
            new string[] {"irish", "ireland"},
            new string[] {"israeli", "israel"},
            new string[] {"italian", "italy"},
            new string[] {"jamaican", "jamaica"},
            new string[] {"japanese", "japan"},
            new string[] {"jordanian", "jordan"},
            new string[] {"kampuchean", "cambodia"},
            new string[] {"kenyan", "kenya"},
            new string[] {"korean", "korea"},
            new string[] {"kuwaiti", "kuwait"},
            new string[] {"lankan", "lanka"},
            new string[] {"laotian", "laos"},
            new string[] {"latvian", "latvia"},
            new string[] {"lebanese", "lebanon"},
            new string[] {"liberian", "liberia"},
            new string[] {"libyan", "libya"},
            new string[] {"lithuanian", "lithuania"},
            new string[] {"macedonian", "macedonia"},
            new string[] {"madagascan", "madagascar"},
            new string[] {"malaysian", "malaysia"},
            new string[] {"maltese", "malta"},
            new string[] {"mauritanian", "mauritania"},
            new string[] {"mexican", "mexico"},
            new string[] {"micronesian", "micronesia"},
            new string[] {"moldovan", "moldova"},
            new string[] {"monacan", "monaco"},
            new string[] {"mongolian", "mongolia"},
            new string[] {"montenegran", "montenegro"},
            new string[] {"moroccan", "morocco"},
            new string[] {"myanmar", "burma"},
            new string[] {"namibian", "namibia"},
            new string[] {"nepalese", "nepal"},
            new string[] {"nicaraguan", "nicaragua"},
            new string[] {"nigerian", "nigeria"},
            new string[] {"norwegian", "norway"},
            new string[] {"omani", "oman"},
            new string[] {"pakistani", "pakistan"},
            new string[] {"panamanian", "panama"},
            new string[] {"papuan", "papua"},
            new string[] {"paraguayan", "paraguay"},
            new string[] {"peruvian", "peru"},
            new string[] {"portuguese", "portugal"},
            new string[] {"romanian", "romania"},
            new string[] {"rumania", "romania"},
            new string[] {"rumanian", "romania"},
            new string[] {"russian", "russia"},
            new string[] {"rwandan", "rwanda"},
            new string[] {"samoan", "samoa"},
            new string[] {"scottish", "scotland"},
            new string[] {"serb", "serbia"},
            new string[] {"serbian", "serbia"},
            new string[] {"siam", "thailand"},
            new string[] {"siamese", "thailand"},
            new string[] {"slovakia", "slovak"},
            new string[] {"slovakian", "slovak"},
            new string[] {"slovenian", "slovenia"},
            new string[] {"somali", "somalia"},
            new string[] {"somalian", "somalia"},
            new string[] {"spanish", "spain"},
            new string[] {"swedish", "sweden"},
            new string[] {"swiss", "switzerland"},
            new string[] {"syrian", "syria"},
            new string[] {"taiwanese", "taiwan"},
            new string[] {"tanzanian", "tanzania"},
            new string[] {"texan", "texas"},
            new string[] {"thai", "thailand"},
            new string[] {"tunisian", "tunisia"},
            new string[] {"turkish", "turkey"},
            new string[] {"ugandan", "uganda"},
            new string[] {"ukrainian", "ukraine"},
            new string[] {"uruguayan", "uruguay"},
            new string[] {"uzbek", "uzbekistan"},
            new string[] {"venezuelan", "venezuela"},
            new string[] {"vietnamese", "viet"},
            new string[] {"virginian", "virginia"},
            new string[] {"yemeni", "yemen"},
            new string[] {"yugoslav", "yugoslavia"},
            new string[] {"yugoslavian", "yugoslavia"},
            new string[] {"zambian", "zambia"},
            new string[] {"zealander", "zealand"},
            new string[] {"zimbabwean", "zimbabwe"}
        };

        private static readonly string[] supplementDict = new string[] { "aids", "applicator",
            "capacitor", "digitize", "electromagnet", "ellipsoid", "exosphere",
            "extensible", "ferromagnet", "graphics", "hydromagnet", "polygraph",
            "toroid", "superconduct", "backscatter", "connectionism"};

        private static readonly string[] properNouns = new string[] { "abrams", "achilles",
            "acropolis", "adams", "agnes", "aires", "alexander", "alexis", "alfred",
            "algiers", "alps", "amadeus", "ames", "amos", "andes", "angeles",
            "annapolis", "antilles", "aquarius", "archimedes", "arkansas", "asher",
            "ashly", "athens", "atkins", "atlantis", "avis", "bahamas", "bangor",
            "barbados", "barger", "bering", "brahms", "brandeis", "brussels",
            "bruxelles", "cairns", "camoros", "camus", "carlos", "celts", "chalker",
            "charles", "cheops", "ching", "christmas", "cocos", "collins",
            "columbus", "confucius", "conners", "connolly", "copernicus", "cramer",
            "cyclops", "cygnus", "cyprus", "dallas", "damascus", "daniels", "davies",
            "davis", "decker", "denning", "dennis", "descartes", "dickens", "doris",
            "douglas", "downs", "dreyfus", "dukakis", "dulles", "dumfries",
            "ecclesiastes", "edwards", "emily", "erasmus", "euphrates", "evans",
            "everglades", "fairbanks", "federales", "fisher", "fitzsimmons",
            "fleming", "forbes", "fowler", "france", "francis", "goering",
            "goodling", "goths", "grenadines", "guiness", "hades", "harding",
            "harris", "hastings", "hawkes", "hawking", "hayes", "heights",
            "hercules", "himalayas", "hippocrates", "hobbs", "holmes", "honduras",
            "hopkins", "hughes", "humphreys", "illinois", "indianapolis",
            "inverness", "iris", "iroquois", "irving", "isaacs", "italy", "james",
            "jarvis", "jeffreys", "jesus", "jones", "josephus", "judas", "julius",
            "kansas", "keynes", "kipling", "kiwanis", "lansing", "laos", "leeds",
            "levis", "leviticus", "lewis", "louis", "maccabees", "madras",
            "maimonides", "maldive", "massachusetts", "matthews", "mauritius",
            "memphis", "mercedes", "midas", "mingus", "minneapolis", "mohammed",
            "moines", "morris", "moses", "myers", "myknos", "nablus", "nanjing",
            "nantes", "naples", "neal", "netherlands", "nevis", "nostradamus",
            "oedipus", "olympus", "orleans", "orly", "papas", "paris", "parker",
            "pauling", "peking", "pershing", "peter", "peters", "philippines",
            "phineas", "pisces", "pryor", "pythagoras", "queens", "rabelais",
            "ramses", "reynolds", "rhesus", "rhodes", "richards", "robins",
            "rodgers", "rogers", "rubens", "sagittarius", "seychelles", "socrates",
            "texas", "thames", "thomas", "tiberias", "tunis", "venus", "vilnius",
            "wales", "warner", "wilkins", "williams", "wyoming", "xmas", "yonkers",
            "zeus", "frances", "aarhus", "adonis", "andrews", "angus", "antares",
            "aquinas", "arcturus", "ares", "artemis", "augustus", "ayers",
            "barnabas", "barnes", "becker", "bejing", "biggs", "billings", "boeing",
            "boris", "borroughs", "briggs", "buenos", "calais", "caracas", "cassius",
            "cerberus", "ceres", "cervantes", "chantilly", "chartres", "chester",
            "connally", "conner", "coors", "cummings", "curtis", "daedalus",
            "dionysus", "dobbs", "dolores", "edmonds"};

        internal class DictEntry
        {
            internal bool exception;
            internal string root;

            internal DictEntry(string root, bool isException)
            {
                this.root = root;
                this.exception = isException;
            }
        }

        private static readonly CharArrayDictionary<DictEntry> dict_ht = InitializeDictHash();

        // caching off 
        // 
        // private int maxCacheSize; private CharArrayDictionary{String} cache =
        // null; private static final String SAME = "SAME"; // use if stemmed form is
        // the same

        private readonly OpenStringBuilder word = new OpenStringBuilder();
        private int j; // index of final letter in stem (within word)
        /// <summary>
        /// INDEX of final letter in word. You must add 1 to k to get
        /// the current length of word. When you want the length of
        /// word, use the method wordLength, which returns (k+1).
        /// </summary>
        private int k;

        // private void initializeStemHash() { if (maxCacheSize > 0) cache = new
        // CharArrayDictionary<String>(maxCacheSize,false); }

        private char FinalChar => word[k];

        private char PenultChar => word[k - 1];

        private bool IsVowel(int index)
        {
            return !IsCons(index);
        }

        private bool IsCons(int index)
        {
            char ch;

            ch = word[index];

            if ((ch == 'a') || (ch == 'e') || (ch == 'i') || (ch == 'o') || (ch == 'u'))
            {
                return false;
            }
            if ((ch != 'y') || (index == 0))
            {
                return true;
            }
            else
            {
                return (!IsCons(index - 1));
            }
        }

        private static CharArrayDictionary<DictEntry> InitializeDictHash()
        {
            DictEntry defaultEntry;
            DictEntry entry;

#pragma warning disable 612, 618
            CharArrayDictionary<DictEntry> d = new CharArrayDictionary<DictEntry>(LuceneVersion.LUCENE_CURRENT, 1000, false);
#pragma warning restore 612, 618
            for (int i = 0; i < exceptionWords.Length; i++)
            {
                if (!d.ContainsKey(exceptionWords[i]))
                {
                    entry = new DictEntry(exceptionWords[i], true);
                    d[exceptionWords[i]] = entry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + exceptionWords[i] + "] already in dictionary 1");
                }
            }

            for (int i = 0; i < directConflations.Length; i++)
            {
                if (!d.ContainsKey(directConflations[i][0]))
                {
                    entry = new DictEntry(directConflations[i][1], false);
                    d[directConflations[i][0]] = entry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + directConflations[i][0] + "] already in dictionary 2");
                }
            }

            for (int i = 0; i < countryNationality.Length; i++)
            {
                if (!d.ContainsKey(countryNationality[i][0]))
                {
                    entry = new DictEntry(countryNationality[i][1], false);
                    d[countryNationality[i][0]] = entry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + countryNationality[i][0] + "] already in dictionary 3");
                }
            }

            defaultEntry = new DictEntry(null, false);

            string[] array;
            array = KStemData1.data;

            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData2.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData3.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData4.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData5.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData6.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            array = KStemData7.data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d[array[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + array[i] + "] already in dictionary 4");
                }
            }

            for (int i = 0; i < KStemData8.data.Length; i++)
            {
                if (!d.ContainsKey(KStemData8.data[i]))
                {
                    d[KStemData8.data[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + KStemData8.data[i] + "] already in dictionary 4");
                }
            }

            for (int i = 0; i < supplementDict.Length; i++)
            {
                if (!d.ContainsKey(supplementDict[i]))
                {
                    d[supplementDict[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + supplementDict[i] + "] already in dictionary 5");
                }
            }

            for (int i = 0; i < properNouns.Length; i++)
            {
                if (!d.ContainsKey(properNouns[i]))
                {
                    d[properNouns[i]] = defaultEntry;
                }
                else
                {
                    throw RuntimeException.Create("Warning: Entry [" + properNouns[i] + "] already in dictionary 6");
                }
            }

            return d;
        }

        private static bool IsAlpha(char ch) // LUCENENET: CA1822: Mark members as static
        {
            return ch >= 'a' && ch <= 'z'; // terms must be lowercased already
        }

        /// <summary>length of stem within word</summary>
        private int StemLength => j + 1;

        private bool EndsIn(char[] s)
        {
            if (s.Length > k)
            {
                return false;
            }

            int r = word.Length - s.Length; // length of word before this suffix
            j = k;
            for (int r1 = r, i = 0; i < s.Length; i++, r1++)
            {
                if (s[i] != word[r1])
                {
                    return false;
                }
            }
            j = r - 1; // index of the character BEFORE the posfix
            return true;
        }

        private bool EndsIn(char a, char b)
        {
            if (2 > k)
            {
                return false;
            }
            // check left to right since the endings have often already matched
            if (word[k - 1] == a && word[k] == b)
            {
                j = k - 2;
                return true;
            }
            return false;
        }

        private bool EndsIn(char a, char b, char c)
        {
            if (3 > k)
            {
                return false;
            }
            if (word[k - 2] == a && word[k - 1] == b && word[k] == c)
            {
                j = k - 3;
                return true;
            }
            return false;
        }

        private bool EndsIn(char a, char b, char c, char d)
        {
            if (4 > k)
            {
                return false;
            }
            if (word[k - 3] == a && word[k - 2] == b && word[k - 1] == c && word[k] == d)
            {
                j = k - 4;
                return true;
            }
            return false;
        }

        private DictEntry WordInDict()
        {
            // if (matchedEntry != null) { if (dict_ht.get(word.getArray(), 0,
            // word.size()) != matchedEntry) {
            // System.out.println("Uh oh... cached entry doesn't match"); } return
            // matchedEntry; }

            if (matchedEntry != null)
            {
                return matchedEntry;
            }
            if (dict_ht.TryGetValue(word.Array, 0, word.Length, out DictEntry e) && e != null && !e.exception)
            {
                matchedEntry = e; // only cache if it's not an exception.
            }
            // lookups.add(word.toString());
            return e;
        }

        /// <summary>Convert plurals to singular form, and '-ies' to 'y'</summary>
        private void Plural()
        {
            if (word[k] == 's')
            {
                if (EndsIn('i', 'e', 's'))
                {
                    word.Length = j + 3;
                    k--;
                    if (Lookup()) // ensure calories -> calorie
                    {
                        return;
                    }
                    k++;
                    word.UnsafeWrite('s');
                    SetSuffix("y");
                    Lookup();
                }
                else if (EndsIn('e', 's'))
                {
                    /* try just removing the "s" */
                    word.Length = j + 2;
                    k--;

                    /*
                     * note: don't check for exceptions here. So, `aides' -> `aide', but
                     * `aided' -> `aid'. The exception for double s is used to prevent
                     * crosses -> crosse. This is actually correct if crosses is a plural
                     * noun (a type of racket used in lacrosse), but the verb is much more
                     * common
                     */


                    //**
                    // YCS: this was the one place where lookup was not followed by return.
                    // So restructure it. if ((j>0)&&(lookup(word.toString())) &&
                    // !((word.CharAt(j) == 's') && (word.CharAt(j-1) == 's'))) return;
                    // ****

                    bool tryE = j > 0 && !((word[j] == 's') && (word[j - 1] == 's'));
                    if (tryE && Lookup())
                    {
                        return;
                    }

                    /* try removing the "es" */

                    word.Length = j + 1;
                    k--;
                    if (Lookup())
                    {
                        return;
                    }

                    /* the default is to retain the "e" */
                    word.UnsafeWrite('e');
                    k++;

                    if (!tryE) // if we didn't try the "e" ending before
                    {
                        Lookup();
                    }
                    //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                }
                else
                {
                    if (word.Length > 3 && PenultChar != 's' && !EndsIn('o', 'u', 's'))
                    {
                        /* unless the word ends in "ous" or a double "s", remove the final "s" */

                        word.Length = k;
                        k--;
                        Lookup();
                    }
                }
            }
        }

        private void SetSuffix(string s)
        {
            SetSuff(s, s.Length);
        }

        /// <summary>replace old suffix with s</summary>
        private void SetSuff(string s, int len)
        {
            word.Length = j + 1;
            for (int l = 0; l < len; l++)
            {
                word.UnsafeWrite(s[l]);
            }
            k = j + len;
        }

        /* Returns true if the word is found in the dictionary */
        // almost all uses of Lookup() return immediately and are
        // followed by another lookup in the dict. Store the match
        // to avoid this double lookup.
        internal DictEntry matchedEntry = null;

        private bool Lookup()
        {
            // debugging code String thisLookup = word.toString(); boolean added =
            // lookups.add(thisLookup); if (!added) {
            // System.out.println("######extra lookup:" + thisLookup); // occaasional
            // extra lookups aren't necessarily errors... could happen by diff
            // manipulations // throw RuntimeException.Create("######extra lookup:" +
            // thisLookup); } else { // System.out.println("new lookup:" + thisLookup);
            // }

            return dict_ht.TryGetValue(word.Array, 0, word.Length, out matchedEntry) && matchedEntry != null;
        }

        // Set<String> lookups = new HashSet<>();

        /// <summary>convert past tense (-ed) to present, and `-ied' to `y'</summary>
        private void PastTense()
        {
            /*
             * Handle words less than 5 letters with a direct mapping This prevents
             * (fled -> fl).
             */
            if (word.Length <= 4)
            {
                return;
            }

            if (EndsIn('i', 'e', 'd'))
            {
                word.Length = j + 3;
                k--;
                if (Lookup()) // we almost always want to convert -ied to -y, but
                {
                    return; // this isn't true for short words (died->die)
                }
                k++; // I don't know any long words that this applies to,
                word.UnsafeWrite('d'); // but just in case...
                SetSuffix("y");
                Lookup();
                return;
            }

            /* the vowelInStem() is necessary so we don't stem acronyms */
            if (EndsIn('e', 'd') && VowelInStem())
            {
                /* see if the root ends in `e' */
                word.Length = j + 2;
                k = j + 1;

                DictEntry entry = WordInDict();
                if (entry != null) 
                {
                    if (!entry.exception) 
                    {
                        // if it's in the dictionary and
                        // not an exception
                        return;
                    }
                }

                /* try removing the "ed" */
                word.Length = j + 1;
                k = j;
                if (Lookup())
                {
                    return;
                }

                /*
                 * try removing a doubled consonant. if the root isn't found in the
                 * dictionary, the default is to leave it doubled. This will correctly
                 * capture `backfilled' -> `backfill' instead of `backfill' ->
                 * `backfille', and seems correct most of the time
                 */

                if (DoubleC(k))
                {
                    word.Length = k;
                    k--;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite(word[k]);
                    k++;
                    Lookup();
                    return;
                }

                /* if we have a `un-' prefix, then leave the word alone */
                /* (this will sometimes screw up with `under-', but we */
                /* will take care of that later) */

                if ((word[0] == 'u') && (word[1] == 'n'))
                {
                    word.UnsafeWrite('e');
                    word.UnsafeWrite('d');
                    k = k + 2;
                    // nolookup()
                    return;
                }

                /*
                 * it wasn't found by just removing the `d' or the `ed', so prefer to end
                 * with an `e' (e.g., `microcoded' -> `microcode').
                 */

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                // nolookup() - we already tried the "e" ending
                //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
            }
        }

        /// <summary>return TRUE if word ends with a double consonant</summary>
        private bool DoubleC(int i)
        {
            if (i < 1)
            {
                return false;
            }

            if (word[i] != word[i - 1])
            {
                return false;
            }
            return (IsCons(i));
        }

        private bool VowelInStem()
        {
            for (int i = 0; i < StemLength; i++)
            {
                if (IsVowel(i))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>handle `-ing' endings</summary>
        private void Aspect()
        {
            /*
             * handle short words (aging -> age) via a direct mapping. This prevents
             * (thing -> the) in the version of this routine that ignores inflectional
             * variants that are mentioned in the dictionary (when the root is also
             * present)
             */

            if (word.Length <= 5)
            {
                return;
            }

            /* the vowelinstem() is necessary so we don't stem acronyms */
            if (EndsIn('i', 'n', 'g') && VowelInStem())
            {

                /* try adding an `e' to the stem and check against the dictionary */
                word[j + 1] = 'e';
                word.Length = j + 2;
                k = j + 1;

                DictEntry entry = WordInDict();
                if (entry != null)
                {
                    if (!entry.exception) // if it's in the dictionary and not an exception
                    {
                        return;
                    }
                }

                /* adding on the `e' didn't work, so remove it */
                word.Length = k;
                k--; // note that `ing' has also been removed

                if (Lookup())
                {
                    return;
                }

                /* if I can remove a doubled consonant and get a word, then do so */
                if (DoubleC(k))
                {
                    k--;
                    word.Length = k + 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite(word[k]); // restore the doubled consonant

                    /* the default is to leave the consonant doubled */
                    /* (e.g.,`fingerspelling' -> `fingerspell'). Unfortunately */
                    /* `bookselling' -> `booksell' and `mislabelling' -> `mislabell'). */
                    /* Without making the algorithm significantly more complicated, this */
                    /* is the best I can do */
                    k++;
                    Lookup();
                    return;
                }

                /*
                 * the word wasn't in the dictionary after removing the stem, and then
                 * checking with and without a final `e'. The default is to add an `e'
                 * unless the word ends in two consonants, so `microcoding' ->
                 * `microcode'. The two consonants restriction wouldn't normally be
                 * necessary, but is needed because we don't try to deal with prefixes and
                 * compounds, and most of the time it is correct (e.g., footstamping ->
                 * footstamp, not footstampe; however, decoupled -> decoupl). We can
                 * prevent almost all of the incorrect stems if we try to do some prefix
                 * analysis first
                 */

                if ((j > 0) && IsCons(j) && IsCons(j - 1))
                {
                    k = j;
                    word.Length = k + 1;
                    // nolookup() because we already did according to the comment
                    return;
                }

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                // nolookup(); we already tried an 'e' ending
                //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
            }
        }

        /// <summary>
        /// this routine deals with -ity endings. It accepts -ability, -ibility, and
        /// -ality, even without checking the dictionary because they are so
        /// productive. The first two are mapped to -ble, and the -ity is remove for
        /// the latter
        /// </summary>
        private void ItyEndings()
        {
            int old_k = k;

            if (EndsIn('i', 't', 'y'))
            {
                word.Length = j + 1; // try just removing -ity
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.UnsafeWrite('e'); // try removing -ity and adding -e
                k = j + 1;
                if (Lookup())
                {
                    return;
                }
                word[j + 1] = 'i';
                word.Append("ty");
                k = old_k;
                /*
                 * the -ability and -ibility endings are highly productive, so just accept
                 * them
                 */
                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'l'))
                {
                    word.Length = j - 1;
                    word.Append("le"); // convert to -ble
                    k = j;
                    Lookup();
                    return;
                }

                /* ditto for -ivity */
                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'v'))
                {
                    word.Length = j + 1;
                    word.UnsafeWrite('e'); // convert to -ive
                    k = j + 1;
                    Lookup();
                    return;
                }
                /* ditto for -ality */
                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 'l'))
                {
                    word.Length = j + 1;
                    k = j;
                    Lookup();
                    return;
                }

                /*
                 * if the root isn't in the dictionary, and the variant *is* there, then
                 * use the variant. This allows `immunity'->`immune', but prevents
                 * `capacity'->`capac'. If neither the variant nor the root form are in
                 * the dictionary, then remove the ending as a default
                 */

                if (Lookup())
                {
                    return;
                }

                /* the default is to remove -ity altogether */
                word.Length = j + 1;
                k = j;
                // nolookup(), we already did it.
                //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
            }
        }

        /// <summary>handle -ence and -ance</summary>
        private void NceEndings()
        {
            int old_k = k;
            char word_char;

            if (EndsIn('n', 'c', 'e'))
            {
                word_char = word[j];
                if (!((word_char == 'e') || (word_char == 'a')))
                {
                    return;
                }
                word.Length = j;
                word.UnsafeWrite('e'); // try converting -e/ance to -e (adherance/adhere)
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.Length = j; /*
                              * try removing -e/ance altogether
                              * (disappearance/disappear)
                              */
                k = j - 1;
                if (Lookup())
                {
                    return;
                }
                word.UnsafeWrite(word_char); // restore the original ending
                word.Append("nce");
                k = old_k;
                // nolookup() because we restored the original ending
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>handle -ness</summary>
        private void NessEndings()
        {
            if (EndsIn('n', 'e', 's', 's'))
            {
                /*
                                                   * this is a very productive endings, so
                                                   * just accept it
                                                   */
                word.Length = j + 1;
                k = j;
                if (word[j] == 'i')
                {
                    word[j] = 'y';
                }
                Lookup();
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>handle -ism</summary>
        private void IsmEndings()
        {
            if (EndsIn('i', 's', 'm'))
            {
                /*
                                              * this is a very productive ending, so just
                                              * accept it
                                              */
                word.Length = j + 1;
                k = j;
                Lookup();
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>this routine deals with -ment endings.</summary>
        private void MentEndings()
        {
            int old_k = k;

            if (EndsIn('m', 'e', 'n', 't'))
            {
                word.Length = j + 1;
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.Append("ment");
                k = old_k;
                // nolookup
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>this routine deals with -ize endings.</summary>
        private void IzeEndings()
        {
            int old_k = k;

            if (EndsIn('i', 'z', 'e'))
            {
                word.Length = j + 1; // try removing -ize entirely
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.UnsafeWrite('i');

                if (DoubleC(j)) // allow for a doubled consonant
                {
                    word.Length = j;
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite(word[j - 1]);
                }

                word.Length = j + 1;
                word.UnsafeWrite('e'); // try removing -ize and adding -e
                k = j + 1;
                if (Lookup())
                {
                    return;
                }
                word.Length = j + 1;
                word.Append("ize");
                k = old_k;
                // nolookup()
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>handle -ency and -ancy</summary>
        private void NcyEndings()
        {
            if (EndsIn('n', 'c', 'y'))
            {
                if (!((word[j] == 'e') || (word[j] == 'a')))
                {
                    return;
                }
                word[j + 2] = 't'; // try converting -ncy to -nt
                word.Length = j + 3;
                k = j + 2;

                if (Lookup())
                {
                    return;
                }

                word[j + 2] = 'c'; // the default is to convert it to -nce
                word.UnsafeWrite('e');
                k = j + 3;
                Lookup();
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>handle -able and -ible</summary>
        private void BleEndings()
        {
            int old_k = k;
            char word_char;

            if (EndsIn('b', 'l', 'e'))
            {
                if (!((word[j] == 'a') || (word[j] == 'i')))
                {
                    return;
                }
                word_char = word[j];
                word.Length = j; // try just removing the ending
                k = j - 1;
                if (Lookup())
                {
                    return;
                }
                if (DoubleC(k)) // allow for a doubled consonant
                {
                    word.Length = k;
                    k--;
                    if (Lookup())
                    {
                        return;
                    }
                    k++;
                    word.UnsafeWrite(word[k - 1]);
                }
                word.Length = j;
                word.UnsafeWrite('e'); // try removing -a/ible and adding -e
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.Length = j;
                word.Append("ate"); // try removing -able and adding -ate
                                    /* (e.g., compensable/compensate) */
                k = j + 2;
                if (Lookup())
                {
                    return;
                }
                word.Length = j;
                word.UnsafeWrite(word_char); // restore the original values
                word.Append("ble");
                k = old_k;
                // nolookup()
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>
        /// handle -ic endings. This is fairly straightforward, but this is also the
        /// only place we try *expanding* an ending, -ic -> -ical. This is to handle
        /// cases like `canonic' -> `canonical'
        /// </summary>
        private void IcEndings()
        {
            if (EndsIn('i', 'c'))
            {
                word.Length = j + 3;
                word.Append("al"); // try converting -ic to -ical
                k = j + 4;
                if (Lookup())
                {
                    return;
                }

                word[j + 1] = 'y'; // try converting -ic to -y
                word.Length = j + 2;
                k = j + 1;
                if (Lookup())
                {
                    return;
                }

                word[j + 1] = 'e'; // try converting -ic to -e
                if (Lookup())
                {
                    return;
                }

                word.Length = j + 1; // try removing -ic altogether
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.Append("ic"); // restore the original ending
                k = j + 2;
                // nolookup()
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        private static char[] ization = "ization".ToCharArray();
        private static char[] ition = "ition".ToCharArray();
        private static char[] ation = "ation".ToCharArray();
        private static char[] ication = "ication".ToCharArray();

        /* handle some derivational endings */

        /// <summary>
        /// this routine deals with -ion, -ition, -ation, -ization, and -ication. The
        /// -ization ending is always converted to -ize
        /// </summary>
        private void IonEndings()
        {
            int old_k = k;
            if (!EndsIn('i', 'o', 'n'))
            {
                return;
            }

            if (EndsIn(ization))
            {
                /*
                                        * the -ize ending is very productive, so simply
                                        * accept it as the root
                                        */
                word.Length = j + 3;
                word.UnsafeWrite('e');
                k = j + 3;
                Lookup();
                return;
            }

            if (EndsIn(ition))
            {
                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                if (Lookup()) /*
                         * remove -ition and add `e', and check against the
                         * dictionary
                         */
                {
                    return; // (e.g., definition->define, opposition->oppose)
                }

                /* restore original values */
                word.Length = j + 1;
                word.Append("ition");
                k = old_k;
                // nolookup()
            }
            else if (EndsIn(ation))
            {
                word.Length = j + 3;
                word.UnsafeWrite('e');
                k = j + 3;
                if (Lookup()) // remove -ion and add `e', and check against the dictionary
                {
                    return; // (elmination -> eliminate)
                }

                word.Length = j + 1;
                word.UnsafeWrite('e'); /*
                                  * remove -ation and add `e', and check against the
                                  * dictionary
                                  */
                k = j + 1;
                if (Lookup())
                {
                    return;
                }

                word.Length = j + 1; /*
                                 * just remove -ation (resignation->resign) and
                                 * check dictionary
                                 */
                k = j;
                if (Lookup())
                {
                    return;
                }

                /* restore original values */
                word.Length = j + 1;
                word.Append("ation");
                k = old_k;
                // nolookup()

            }

            /*
             * test -ication after -ation is attempted (e.g., `complication->complicate'
             * rather than `complication->comply')
             */

            if (EndsIn(ication))
            {
                word.Length = j + 1;
                word.UnsafeWrite('y');
                k = j + 1;
                if (Lookup()) /*
                         * remove -ication and add `y', and check against the
                         * dictionary
                         */
                {
                    return; // (e.g., amplification -> amplify)
                }

                /* restore original values */
                word.Length = j + 1;
                word.Append("ication");
                k = old_k;
                // nolookup()
            }

            // if (EndsIn(ion)) {
            if (true) // we checked for this earlier... just need to set "j"
            {
                j = k - 3; // YCS

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                if (Lookup()) // remove -ion and add `e', and check against the dictionary
                {
                    return;
                }

                word.Length = j + 1;
                k = j;
                if (Lookup()) // remove -ion, and if it's found, treat that as the root
                {
                    return;
                }

                /* restore original values */
                word.Length = j + 1;
                word.Append("ion");
                k = old_k;
                // nolookup()
            }

            // nolookup(); all of the other paths restored original values
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>
        /// this routine deals with -er, -or, -ier, and -eer. The -izer ending is
        /// always converted to -ize
        /// </summary>
        private void ErAndOrEndings()
        {
            int old_k = k;

            if (word[k] != 'r') // YCS
            {
                return;
            }

            char word_char; // so we can remember if it was -er or -or

            if (EndsIn('i', 'z', 'e', 'r'))
            {
                /*
                                                   * -ize is very productive, so accept it
                                                   * as the root
                                                   */
                word.Length = j + 4;
                k = j + 3;
                Lookup();
                return;
            }

            if (EndsIn('e', 'r') || EndsIn('o', 'r'))
            {
                word_char = word[j + 1];
                if (DoubleC(j))
                {
                    word.Length = j;
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite(word[j - 1]); // restore the doubled consonant
                }

                if (word[j] == 'i') // do we have a -ier ending?
                {
                    word[j] = 'y';
                    word.Length = j + 1;
                    k = j;
                    if (Lookup()) // yes, so check against the dictionary
                    {
                        return;
                    }
                    word[j] = 'i'; // restore the endings
                    word.UnsafeWrite('e');
                }

                if (word[j] == 'e') // handle -eer
                {
                    word.Length = j;
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite('e');
                }

                word.Length = j + 2; // remove the -r ending
                k = j + 1;
                if (Lookup())
                {
                    return;
                }
                word.Length = j + 1; // try removing -er/-or
                k = j;
                if (Lookup())
                {
                    return;
                }
                word.UnsafeWrite('e'); // try removing -or and adding -e
                k = j + 1;
                if (Lookup())
                {
                    return;
                }
                word.Length = j + 1;
                word.UnsafeWrite(word_char);
                word.UnsafeWrite('r'); // restore the word to the way it was
                k = old_k;
                // nolookup()
            }

        }

        /// <summary>
        /// this routine deals with -ly endings. The -ally ending is always converted
        /// to -al Sometimes this will temporarily leave us with a non-word (e.g.,
        /// heuristically maps to heuristical), but then the -al is removed in the next
        /// step.
        /// </summary>
        private void LyEndings()
        {
            int old_k = k;

            if (EndsIn('l', 'y'))
            {

                word[j + 2] = 'e'; // try converting -ly to -le

                if (Lookup())
                {
                    return;
                }
                word[j + 2] = 'y';

                word.Length = j + 1; // try just removing the -ly
                k = j;

                if (Lookup())
                {
                    return;
                }

                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 'l')) /*
                                                                                  * always
                                                                                  * convert
                                                                                  * -
                                                                                  * ally
                                                                                  * to
                                                                                  * -
                                                                                  * al
                                                                                  */
                {
                    return;
                }
                word.Append("ly");
                k = old_k;

                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 'b'))
                {
                    /*
                                                                                              * always
                                                                                              * convert
                                                                                              * -
                                                                                              * ably
                                                                                              * to
                                                                                              * -
                                                                                              * able
                                                                                              */
                    word[j + 2] = 'e';
                    k = j + 2;
                    return;
                }

                if (word[j] == 'i') // e.g., militarily -> military
                {
                    word.Length = j;
                    word.UnsafeWrite('y');
                    k = j;
                    if (Lookup())
                    {
                        return;
                    }
                    word.Length = j;
                    word.Append("ily");
                    k = old_k;
                }

                word.Length = j + 1; // the default is to remove -ly

                k = j;
                // nolookup()... we already tried removing the "ly" variant
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>
        /// this routine deals with -al endings. Some of the endings from the previous
        /// routine are finished up here.
        /// </summary>
        private void AlEndings()
        {
            int old_k = k;

            if (word.Length < 4)
            {
                return;
            }
            if (EndsIn('a', 'l'))
            {
                word.Length = j + 1;
                k = j;
                if (Lookup()) // try just removing the -al
                {
                    return;
                }

                if (DoubleC(j)) // allow for a doubled consonant
                {
                    word.Length = j;
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.UnsafeWrite(word[j - 1]);
                }

                word.Length = j + 1;
                word.UnsafeWrite('e'); // try removing the -al and adding -e
                k = j + 1;
                if (Lookup())
                {
                    return;
                }

                word.Length = j + 1;
                word.Append("um"); // try converting -al to -um
                                   /* (e.g., optimal - > optimum ) */
                k = j + 2;
                if (Lookup())
                {
                    return;
                }

                word.Length = j + 1;
                word.Append("al"); // restore the ending to the way it was
                k = old_k;

                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'c'))
                {
                    word.Length = j - 1; // try removing -ical
                    k = j - 2;
                    if (Lookup())
                    {
                        return;
                    }

                    word.Length = j - 1;
                    word.UnsafeWrite('y'); // try turning -ical to -y (e.g., bibliographical)
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }

                    word.Length = j - 1;
                    word.Append("ic"); // the default is to convert -ical to -ic
                    k = j;
                    // nolookup() ... converting ical to ic means removing "al" which we
                    // already tried
                    // ERROR
                    Lookup();
                    return;
                }

                if (word[j] == 'i') // sometimes -ial endings should be removed
                {
                    word.Length = j; // (sometimes it gets turned into -y, but we
                    k = j - 1; // aren't dealing with that case for now)
                    if (Lookup())
                    {
                        return;
                    }
                    word.Append("ial");
                    k = old_k;
                    Lookup();
                }

            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        /// <summary>
        /// this routine deals with -ive endings. It normalizes some of the -ative
        /// endings directly, and also maps some -ive endings to -ion.
        /// </summary>
        private void IveEndings()
        {
            int old_k = k;

            if (EndsIn('i', 'v', 'e'))
            {
                word.Length = j + 1; // try removing -ive entirely
                k = j;
                if (Lookup())
                {
                    return;
                }

                word.UnsafeWrite('e'); // try removing -ive and adding -e
                k = j + 1;
                if (Lookup())
                {
                    return;
                }
                word.Length = j + 1;
                word.Append("ive");
                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 't'))
                {
                    word[j - 1] = 'e'; // try removing -ative and adding -e
                    word.Length = j; // (e.g., determinative -> determine)
                    k = j - 1;
                    if (Lookup())
                    {
                        return;
                    }
                    word.Length = j - 1; // try just removing -ative
                    if (Lookup())
                    {
                        return;
                    }

                    word.Append("ative");
                    k = old_k;
                }

                /* try mapping -ive to -ion (e.g., injunctive/injunction) */
                word[j + 2] = 'o';
                word[j + 3] = 'n';
                if (Lookup())
                {
                    return;
                }

                word[j + 2] = 'v'; // restore the original values
                word[j + 3] = 'e';
                k = old_k;
                // nolookup()
            }
            //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
        }

        internal KStemmer()
        {
        }

        internal virtual string Stem(string term)
        {
            bool changed = Stem(term.ToCharArray(), term.Length);
            if (!changed)
            {
                return term;
            }
            return AsString();
        }

        /// <summary>
        /// Returns the result of the stem (assuming the word was changed) as a <see cref="string"/>.
        /// </summary>
        internal virtual string AsString()
        {
            string s = String;
            if (s != null)
            {
                return s;
            }
            return word.ToString();
        }

        internal virtual ICharSequence AsCharSequence()
        {
            return result != null ? (ICharSequence)new CharsRef(result) : word;
        }

        internal virtual string String => result;

        internal virtual char[] Chars => word.Array;

        internal virtual int Length => word.Length;

        internal string result;

        private bool IsMatched =>
            //*
            // if (!lookups.contains(word.toString())) { throw new
            // RuntimeException("didn't look up "+word.toString()+" prev="+prevLookup);
            // }
            // **
            // lookup();
            matchedEntry != null;

        /// <summary>
        /// Stems the text in the token. Returns true if changed.
        /// </summary>
        internal virtual bool Stem(char[] term, int len)
        {

            result = null;

            k = len - 1;
            if ((k <= 1) || (k >= MaxWordLen - 1))
            {
                return false; // don't stem
            }

            // first check the stemmer dictionaries, and avoid using the
            // cache if it's in there.
            if (dict_ht.TryGetValue(term, 0, len, out DictEntry entry) && entry != null)
            {
                if (entry.root != null)
                {
                    result = entry.root;
                    return true;
                }
                return false;
            }

            //*
            // caching off is normally faster if (cache is null) initializeStemHash();
            // 
            // // now check the cache, before we copy chars to "word" if (cache != null)
            // { String val = cache.get(term, 0, len); if (val != null) { if (val !=
            // SAME) { result = val; return true; } return false; } }
            // **

            word.Reset();
            // allocate enough space so that an expansion is never needed
            word.EnsureCapacity(len + 10);
            for (int i = 0; i < len; i++)
            {
                char ch = term[i];
                if (!IsAlpha(ch)) // don't stem
                {
                    return false;
                }
                // don't lowercase... it's a requirement that lowercase filter be
                // used before this stemmer.
                word.UnsafeWrite(ch);
            }

            matchedEntry = null;

            //*
            // lookups.clear(); lookups.add(word.toString());
            // **


            /*
             * This while loop will never be executed more than one time; it is here
             * only to allow the break statement to be used to escape as soon as a word
             * is recognized
             */
            while (true)
            {
                // YCS: extra lookup()s were inserted so we don't need to
                // do an extra wordInDict() here.
                Plural();
                if (IsMatched)
                {
                    break;
                }
                PastTense();
                if (IsMatched)
                {
                    break;
                }
                Aspect();
                if (IsMatched)
                {
                    break;
                }
                ItyEndings();
                if (IsMatched)
                {
                    break;
                }
                NessEndings();
                if (IsMatched)
                {
                    break;
                }
                IonEndings();
                if (IsMatched)
                {
                    break;
                }
                ErAndOrEndings();
                if (IsMatched)
                {
                    break;
                }
                LyEndings();
                if (IsMatched)
                {
                    break;
                }
                AlEndings();
                if (IsMatched)
                {
                    break;
                }
                entry = WordInDict();
                IveEndings();
                if (IsMatched)
                {
                    break;
                }
                IzeEndings();
                if (IsMatched)
                {
                    break;
                }
                MentEndings();
                if (IsMatched)
                {
                    break;
                }
                BleEndings();
                if (IsMatched)
                {
                    break;
                }
                IsmEndings();
                if (IsMatched)
                {
                    break;
                }
                IcEndings();
                if (IsMatched)
                {
                    break;
                }
                NcyEndings();
                if (IsMatched)
                {
                    break;
                }
                NceEndings();
                bool foo = IsMatched;
                break;
            }

            /*
             * try for a direct mapping (allows for cases like `Italian'->`Italy' and
             * `Italians'->`Italy')
             */
            entry = matchedEntry;
            if (entry != null)
            {
                result = entry.root; // may be null, which means that "word" is the stem
            }

            //*
            // caching off is normally faster if (cache != null && cache.size() <
            // maxCacheSize) { char[] key = new char[len]; System.arraycopy(term, 0,
            // key, 0, len); if (result != null) { cache.put(key, result); } else {
            // cache.put(key, word.toString()); } }
            // **

            //*
            // if (entry is null) { if (!word.toString().equals(new String(term,0,len), StringComparison.Ordinal))
            // { System.out.println("CASE:" + word.toString() + "," + new
            // String(term,0,len));
            // 
            // } }
            // **

            // no entry matched means result is "word"
            return true;
        }
    }
}