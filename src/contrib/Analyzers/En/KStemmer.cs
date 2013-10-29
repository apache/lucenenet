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

using System;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.En
{
    public class KStemmer
    {
        private const int MaxWordLen = 50;

        private static readonly string[] ExceptionWords =
        {
            "aide", "bathe", "caste",
            "cute", "dame", "dime", "doge", "done", "dune", "envelope", "gage",
            "grille", "grippe", "lobe", "mane", "mare", "nape", "node", "pane",
            "pate", "plane", "pope", "programme", "quite", "ripe", "rote", "rune",
            "sage", "severe", "shoppe", "sine", "slime", "snipe", "steppe", "suite",
            "swinge", "tare", "tine", "tope", "tripe", "twine"
        };

        private static readonly string[,] DirectConflations =
        {
            {"aging", "age"},
            {"going", "go"}, {"goes", "go"}, {"lying", "lie"}, {"using", "use"},
            {"owing", "owe"}, {"suing", "sue"}, {"dying", "die"}, {"tying", "tie"},
            {"vying", "vie"}, {"aged", "age"}, {"used", "use"}, {"vied", "vie"},
            {"cued", "cue"}, {"died", "die"}, {"eyed", "eye"}, {"hued", "hue"},
            {"iced", "ice"}, {"lied", "lie"}, {"owed", "owe"}, {"sued", "sue"},
            {"toed", "toe"}, {"tied", "tie"}, {"does", "do"}, {"doing", "do"},
            {"aeronautical", "aeronautics"}, {"mathematical", "mathematics"},
            {"political", "politics"}, {"metaphysical", "metaphysics"},
            {"cylindrical", "cylinder"}, {"nazism", "nazi"},
            {"ambiguity", "ambiguous"}, {"barbarity", "barbarous"},
            {"credulity", "credulous"}, {"generosity", "generous"},
            {"spontaneity", "spontaneous"}, {"unanimity", "unanimous"},
            {"voracity", "voracious"}, {"fled", "flee"}, {"miscarriage", "miscarry"}
        };

        private static readonly string[,] CountryNationality =
        {
            {"afghan", "afghanistan"}, {"african", "africa"},
            {"albanian", "albania"}, {"algerian", "algeria"},
            {"american", "america"}, {"andorran", "andorra"}, {"angolan", "angola"},
            {"arabian", "arabia"}, {"argentine", "argentina"},
            {"armenian", "armenia"}, {"asian", "asia"}, {"australian", "australia"},
            {"austrian", "austria"}, {"azerbaijani", "azerbaijan"},
            {"azeri", "azerbaijan"}, {"bangladeshi", "bangladesh"},
            {"belgian", "belgium"}, {"bermudan", "bermuda"}, {"bolivian", "bolivia"},
            {"bosnian", "bosnia"}, {"botswanan", "botswana"},
            {"brazilian", "brazil"}, {"british", "britain"},
            {"bulgarian", "bulgaria"}, {"burmese", "burma"},
            {"californian", "california"}, {"cambodian", "cambodia"},
            {"canadian", "canada"}, {"chadian", "chad"}, {"chilean", "chile"},
            {"chinese", "china"}, {"colombian", "colombia"}, {"croat", "croatia"},
            {"croatian", "croatia"}, {"cuban", "cuba"}, {"cypriot", "cyprus"},
            {"czechoslovakian", "czechoslovakia"}, {"danish", "denmark"},
            {"egyptian", "egypt"}, {"equadorian", "equador"},
            {"eritrean", "eritrea"}, {"estonian", "estonia"},
            {"ethiopian", "ethiopia"}, {"european", "europe"}, {"fijian", "fiji"},
            {"filipino", "philippines"}, {"finnish", "finland"},
            {"french", "france"}, {"gambian", "gambia"}, {"georgian", "georgia"},
            {"german", "germany"}, {"ghanian", "ghana"}, {"greek", "greece"},
            {"grenadan", "grenada"}, {"guamian", "guam"},
            {"guatemalan", "guatemala"}, {"guinean", "guinea"},
            {"guyanan", "guyana"}, {"haitian", "haiti"}, {"hawaiian", "hawaii"},
            {"holland", "dutch"}, {"honduran", "honduras"}, {"hungarian", "hungary"},
            {"icelandic", "iceland"}, {"indonesian", "indonesia"},
            {"iranian", "iran"}, {"iraqi", "iraq"}, {"iraqui", "iraq"},
            {"irish", "ireland"}, {"israeli", "israel"},
            {"italian", "italy"},
            {"jamaican", "jamaica"},
            {"japanese", "japan"},
            {"jordanian", "jordan"},
            {"kampuchean", "cambodia"},
            {"kenyan", "kenya"},
            {"korean", "korea"},
            {"kuwaiti", "kuwait"},
            {"lankan", "lanka"},
            {"laotian", "laos"},
            {"latvian", "latvia"},
            {"lebanese", "lebanon"},
            {"liberian", "liberia"},
            {"libyan", "libya"},
            {"lithuanian", "lithuania"},
            {"macedonian", "macedonia"},
            {"madagascan", "madagascar"},
            {"malaysian", "malaysia"},
            {"maltese", "malta"},
            {"mauritanian", "mauritania"},
            {"mexican", "mexico"},
            {"micronesian", "micronesia"},
            {"moldovan", "moldova"},
            {"monacan", "monaco"},
            {"mongolian", "mongolia"},
            {"montenegran", "montenegro"},
            {"moroccan", "morocco"},
            {"myanmar", "burma"},
            {"namibian", "namibia"},
            {"nepalese", "nepal"},
            // {"netherlands", "dutch"},
            {"nicaraguan", "nicaragua"}, {"nigerian", "nigeria"},
            {"norwegian", "norway"}, {"omani", "oman"}, {"pakistani", "pakistan"},
            {"panamanian", "panama"}, {"papuan", "papua"},
            {"paraguayan", "paraguay"}, {"peruvian", "peru"},
            {"portuguese", "portugal"}, {"romanian", "romania"},
            {"rumania", "romania"}, {"rumanian", "romania"}, {"russian", "russia"},
            {"rwandan", "rwanda"}, {"samoan", "samoa"}, {"scottish", "scotland"},
            {"serb", "serbia"}, {"serbian", "serbia"}, {"siam", "thailand"},
            {"siamese", "thailand"}, {"slovakia", "slovak"}, {"slovakian", "slovak"},
            {"slovenian", "slovenia"}, {"somali", "somalia"},
            {"somalian", "somalia"}, {"spanish", "spain"}, {"swedish", "sweden"},
            {"swiss", "switzerland"}, {"syrian", "syria"}, {"taiwanese", "taiwan"},
            {"tanzanian", "tanzania"}, {"texan", "texas"}, {"thai", "thailand"},
            {"tunisian", "tunisia"}, {"turkish", "turkey"}, {"ugandan", "uganda"},
            {"ukrainian", "ukraine"}, {"uruguayan", "uruguay"},
            {"uzbek", "uzbekistan"}, {"venezuelan", "venezuela"},
            {"vietnamese", "viet"}, {"virginian", "virginia"}, {"yemeni", "yemen"},
            {"yugoslav", "yugoslavia"}, {"yugoslavian", "yugoslavia"},
            {"zambian", "zambia"}, {"zealander", "zealand"},
            {"zimbabwean", "zimbabwe"}
        };

        private static readonly string[] SupplementDict =
        {
            "aids", "applicator",
            "capacitor", "digitize", "electromagnet", "ellipsoid", "exosphere",
            "extensible", "ferromagnet", "graphics", "hydromagnet", "polygraph",
            "toroid", "superconduct", "backscatter", "connectionism"
        };

        private static readonly string[] ProperNouns =
        {
            "abrams", "achilles",
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
            "dionysus", "dobbs", "dolores", "edmonds"
        };

        protected internal class DictEntry
        {
            protected internal bool exception;
            protected internal string root;

            protected internal DictEntry(string root, bool isException)
            {
                this.root = root;
                this.exception = isException;
            }
        }

        private static readonly CharArrayMap<DictEntry> dict_ht = InitializeDictHash();

        private readonly OpenStringBuilder word = new OpenStringBuilder();
        private int j; /* index of final letter in stem (within word) */

        private int k; /*
                        * INDEX of final letter in word. You must add 1 to k to get
                        * the current Length of word. When you want the Length of
                        * word, use the method wordLength, which returns (k+1).
                        */

        private char FinalChar()
        {
            return word[k];
        }

        private char PenultChar()
        {
            return word[k - 1];
        }

        private bool IsVowel(int index)
        {
            return !IsCons(index);
        }

        private bool IsCons(int index)
        {
            char ch;

            ch = word[index];

            if ((ch == 'a') || (ch == 'e') || (ch == 'i') || (ch == 'o') || (ch == 'u')) return false;
            if ((ch != 'y') || (index == 0)) return true;
            else return (!IsCons(index - 1));
        }

        private static CharArrayMap<DictEntry> InitializeDictHash()
        {
            DictEntry defaultEntry;
            DictEntry entry;

            var d = new CharArrayMap<DictEntry>(Version.LUCENE_31, 1000, false);

            d = new CharArrayMap<DictEntry>(Version.LUCENE_31, 1000, false);
            for (var i = 0; i < ExceptionWords.Length; i++)
            {
                if (!d.ContainsKey(ExceptionWords[i]))
                {
                    entry = new DictEntry(ExceptionWords[i], true);
                    d.Put(ExceptionWords[i], entry);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Warning: Entry [{0}] already in dictionary 1",
                        ExceptionWords[i]));
                }
            }

            for (var i = 0; i < DirectConflations.Length; i++)
            {
                if (!d.ContainsKey(DirectConflations[i, 0]))
                {
                    entry = new DictEntry(DirectConflations[i, 1], false);
                    d.Put(DirectConflations[i, 0], entry);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Warning: Entry [{0}] already in dictionary 2",
                        DirectConflations[i, 0]));
                }
            }

            for (var i = 0; i < CountryNationality.Length; i++)
            {
                if (!d.ContainsKey(CountryNationality[i, 0]))
                {
                    entry = new DictEntry(CountryNationality[i, 1], false);
                    d.Put(CountryNationality[i, 0], entry);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Warning: Entry[{0}] already in dictionary 3",
                        CountryNationality[i, 0]));
                }
            }

            defaultEntry = new DictEntry(null, false);

            string[] array = KStemData1.Data;

            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData2.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData3.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData4.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData5.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData6.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            array = KStemData7.Data;
            for (int i = 0; i < array.Length; i++)
            {
                if (!d.ContainsKey(array[i]))
                {
                    d.Put(array[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + array[i]
                                                        + "] already in dictionary 4");
                }
            }

            for (int i = 0; i < KStemData8.Data.Length; i++)
            {
                if (!d.ContainsKey(KStemData8.Data[i]))
                {
                    d.Put(KStemData8.Data[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + KStemData8.Data[i]
                                                        + "] already in dictionary 4");
                }
            }

            for (int i = 0; i < SupplementDict.Length; i++)
            {
                if (!d.ContainsKey(SupplementDict[i]))
                {
                    d.Put(SupplementDict[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + SupplementDict[i]
                                                        + "] already in dictionary 5");
                }
            }

            for (int i = 0; i < ProperNouns.Length; i++)
            {
                if (!d.ContainsKey(ProperNouns[i]))
                {
                    d.Put(ProperNouns[i], defaultEntry);
                }
                else
                {
                    throw new InvalidOperationException("Warning: Entry [" + ProperNouns[i]
                                                        + "] already in dictionary 6");
                }
            }

            return d;
        }

        private bool IsAlpha(char ch)
        {
            return ch >= 'a' && ch <= 'z'; // terms must be lowercased already
        }

        private int StemLength()
        {
            return j + 1;
        }

        private bool EndsIn(char[] s)
        {
            if (s.Length > k) return false;

            var r = word.Length - s.Length; /* length of word before this suffix */
            j = k;
            for (int r1 = r, i = 0; i < s.Length; i++, r1++)
            {
                if (s[i] != word[r1]) return false;
            }
            j = r - 1; /* index of the character BEFORE the posfix */
            return true;
        }

        private bool EndsIn(char a, char b)
        {
            if (2 > k) return false;
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
            if (3 > k) return false;
            if (word[k - 2] == a && word[k - 1] == b && word[k] == c)
            {
                j = k - 3;
                return true;
            }
            return false;
        }

        private bool EndsIn(char a, char b, char c, char d)
        {
            if (4 > k) return false;
            if (word[k - 3] == a && word[k - 2] == b
                && word[k - 1] == c && word[k] == d)
            {
                j = k - 4;
                return true;
            }
            return false;
        }

        private DictEntry WordInDict()
        {
            if (matchedEntry != null) return matchedEntry;
            var e = dict_ht.Get(word.GetArray(), 0, word.Length);
            if (e != null && !e.exception)
            {
                matchedEntry = e; // only cache if it's not an exception.
            }
            return e;
        }

        private void Plural()
        {
            if (word[k] == 's')
            {
                if (EndsIn('i', 'e', 's'))
                {
                    word.Length = j + 3;
                    k--;
                    if (Lookup()) /* ensure calories -> calorie */
                        return;
                    k++;
                    word.UnsafeWrite('s');
                    SetSuffix("y");
                    Lookup();
                }
                else if (EndsIn('e', 's'))
                {
                    word.Length = j + 2;
                    k--;

                    /*
                     * note: don't check for exceptions here. So, `aides' -> `aide', but
                     * `aided' -> `aid'. The exception for double s is used to prevent
                     * crosses -> crosse. This is actually correct if crosses is a plural
                     * noun (a type of racket used in lacrosse), but the verb is much more
                     * common
                     */

                    /****
                     * YCS: this was the one place where Lookup was not followed by return.
                     * So restructure it. if ((j>0)&&(Lookup(word.toString())) &&
                     * !((word.[j) == 's') && (word.[j-1) == 's'))) return;
                     *****/
                    bool tryE = j > 0
                                && !((word[j] == 's') && (word[j - 1] == 's'));
                    if (tryE && Lookup()) return;

                    /* try removing the "es" */

                    word.Length = j + 1;
                    k--;
                    if (Lookup()) return;

                    /* the default is to retain the "e" */
                    word.UnsafeWrite('e');
                    k++;

                    if (!tryE) Lookup(); // if we didn't try the "e" ending before
                    return;
                }
            }
            else
            {
                if (word.Length > 3 && PenultChar() != 's' && !EndsIn('o', 'u', 's'))
                {
                    word.Length = k;
                    k--;
                    Lookup();
                }
            }
        }

        private void SetSuffix(string s)
        {
            SetSuff(s, s.Length);
        }

        private void SetSuff(string s, int len)
        {
            word.Length = j + 1;
            for (var l = 0; l < len; l++)
            {
                word.UnsafeWrite(s[l]);
            }
            k = j + len;
        }

        protected internal DictEntry matchedEntry = null;

        private bool Lookup()
        {
            matchedEntry = dict_ht.Get(word.GetArray(), 0, word.Size);
            return matchedEntry != null;
        }

        private void PastTense()
        {
            if (word.Length <= 4) return;

            if (EndsIn('i', 'e', 'd'))
            {
                word.Length = j + 3;
                k--;
                if (Lookup()) return;
                k++;
                word.UnsafeWrite('d');
                SetSuffix("y");
                Lookup();
                return;
            }

            if (EndsIn('e', 'd') && VowelInStem())
            {
                word.Length = j + 2;
                k = j + 1;

                var entry = WordInDict();
                if (entry != null) if (!entry.exception) return;

                word.Length = j + 1;
                k = j;
                if (Lookup()) return;

                if (DoubleC(k))
                {
                    word.Length = k;
                    k--;
                    if (Lookup()) return;
                    word.UnsafeWrite(word[k]);
                    k++;
                    Lookup();
                    return;
                }

                if ((word[0] == 'u') && (word[1] == 'n'))
                {
                    word.UnsafeWrite('e');
                    word.UnsafeWrite('d');
                    k = k + 2;
                    return;
                }

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                return;
            }
        }


        private bool DoubleC(int i)
        {
            if (i < 1) return false;

            if (word[i] != word[i - 1]) return false;
            return IsCons(i);
        }

        private bool VowelInStem()
        {
            for (var i = 0; i < StemLength(); i++)
            {
                if (IsVowel(i)) return true;
            }
            return false;
        }

        private void Aspect()
        {
            if (word.Length <= 5) return;

            if (EndsIn('i', 'n', 'g') && VowelInStem())
            {
                word[j + 1] = 'e';
                word.Length = j + 2;
                k = j + 1;

                var entry = WordInDict();
                if (entry != null)
                {
                    if (!entry.exception)
                        return;
                }

                word.Length = k;
                k--;

                if (Lookup()) return;

                if (DoubleC(k))
                {
                    k--;
                    word.Length = k + 1;
                    if (Lookup()) return;
                    word.UnsafeWrite(word[k]);
                    k++;
                    Lookup();
                    return;
                }

                if ((j > 0) && IsCons(j) && IsCons(j - 1))
                {
                    k = j;
                    word.Length = k + 1;
                    return;
                }

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                return;
            }
        }

        private void ItyEndings()
        {
            var old_k = k;

            if (EndsIn('i', 't', 'y'))
            {
                word.Length = j + 1;
                k = j;
                if (Lookup()) return;
                word.UnsafeWrite('e');
                k = j + 1;
                if (Lookup()) return;
                word[j + 1] = 'i';
                word.Append("ty".AsCharSequence());
                k = old_k;

                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'l'))
                {
                    word.Length = j - 1;
                    word.Append("le".AsCharSequence());
                    k = j;
                    Lookup();
                    return;
                }

                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'v'))
                {
                    word.Length = j + 1;
                    word.UnsafeWrite('e');
                    k = j + 1;
                    Lookup();
                    return;
                }

                if ((j < 0) && (word[j - 1] == 'a') && (word[j] == 'l'))
                {
                    word.Length = j + 1;
                    k = j;
                    Lookup();
                    return;
                }

                if (Lookup()) return;

                word.Length = j + 1;
                k = j;
                return;
            }
        }

        private void NceEndings()
        {
            int old_k = k;
            char word_char;

            if (EndsIn('n', 'c', 'e'))
            {
                word_char = word[j];
                if (!((word_char == 'e') || (word_char == 'a'))) return;
                word.Length = j;
                word.UnsafeWrite('e'); /* try converting -e/ance to -e (adherance/adhere) */
                k = j;
                if (Lookup()) return;
                word.Length = j; /*
                          * try removing -e/ance altogether
                          * (disappearance/disappear)
                          */
                k = j - 1;
                if (Lookup()) return;
                word.UnsafeWrite(word_char); /* restore the original ending */
                word.Append("nce".AsCharSequence());
                k = old_k;
                // nolookup() because we restored the original ending
            }
            return;
        }

        private void NessEndings()
        {
            if (EndsIn('n', 'e', 's', 's'))
            { /*
                                       * this is a very productive endings, so
                                       * just accept it
                                       */
                word.Length = j + 1;
                k = j;
                if (word[j] == 'i') word[j] = 'y';
                Lookup();
            }
            return;
        }

        private void IsmEndings()
        {
            if (EndsIn('i', 's', 'm'))
            { /*
                                  * this is a very productive ending, so just
                                  * accept it
                                  */
                word.Length = j + 1;
                k = j;
                Lookup();
            }
            return;
        }

        private void MentEndings()
        {
            int old_k = k;

            if (EndsIn('m', 'e', 'n', 't'))
            {
                word.Length = j + 1;
                k = j;
                if (Lookup()) return;
                word.Append("ment".AsCharSequence());
                k = old_k;
                // nolookup
            }
            return;
        }

        private void IzeEndings()
        {
            int old_k = k;

            if (EndsIn('i', 'z', 'e'))
            {
                word.Length = j + 1; /* try removing -ize entirely */
                k = j;
                if (Lookup()) return;
                word.UnsafeWrite('i');

                if (DoubleC(j))
                { /* allow for a doubled consonant */
                    word.Length = j;
                    k = j - 1;
                    if (Lookup()) return;
                    word.UnsafeWrite(word[j - 1]);
                }

                word.Length = j + 1;
                word.UnsafeWrite('e'); /* try removing -ize and adding -e */
                k = j + 1;
                if (Lookup()) return;
                word.Length = j + 1;
                word.Append("ize".AsCharSequence());
                k = old_k;
                // nolookup()
            }
            return;
        }

        private void NcyEndings()
        {
            if (EndsIn('n', 'c', 'y'))
            {
                if (!((word[j] == 'e') || (word[j] == 'a'))) return;
                word[j + 2] = 't'; /* try converting -ncy to -nt */
                word.Length = j + 3;
                k = j + 2;

                if (Lookup()) return;

                word[j + 2] = 'c'; /* the default is to convert it to -nce */
                word.UnsafeWrite('e');
                k = j + 3;
                Lookup();
            }
            return;
        }

        private void BleEndings()
        {
            int old_k = k;
            char word_char;

            if (EndsIn('b', 'l', 'e'))
            {
                if (!((word[j] == 'a') || (word[j] == 'i'))) return;
                word_char = word[j];
                word.Length = j; /* try just removing the ending */
                k = j - 1;
                if (Lookup()) return;
                if (DoubleC(k))
                { /* allow for a doubled consonant */
                    word.Length = k;
                    k--;
                    if (Lookup()) return;
                    k++;
                    word.UnsafeWrite(word[k - 1]);
                }
                word.Length = j;
                word.UnsafeWrite('e'); /* try removing -a/ible and adding -e */
                k = j;
                if (Lookup()) return;
                word.Length = j;
                word.Append("ate".AsCharSequence()); /* try removing -able and adding -ate */
                /* (e.g., compensable/compensate) */
                k = j + 2;
                if (Lookup()) return;
                word.Length = j;
                word.UnsafeWrite(word_char); /* restore the original values */
                word.Append("ble".AsCharSequence());
                k = old_k;
                // nolookup()
            }
            return;
        }

        private void IcEndings()
        {
            if (EndsIn('i', 'c'))
            {
                word.Length = j + 3;
                word.Append("al".AsCharSequence()); /* try converting -ic to -ical */
                k = j + 4;
                if (Lookup()) return;

                word[j + 1] = 'y'; /* try converting -ic to -y */
                word.Length = j + 2;
                k = j + 1;
                if (Lookup()) return;

                word[j + 1] = 'e'; /* try converting -ic to -e */
                if (Lookup()) return;

                word.Length = j + 1; /* try removing -ic altogether */
                k = j;
                if (Lookup()) return;
                word.Append("ic".AsCharSequence()); /* restore the original ending */
                k = j + 2;
                // nolookup()
            }
            return;
        }

        private static readonly char[] Ization = "ization".ToCharArray();
        private static readonly char[] Ition = "ition".ToCharArray();
        private static readonly char[] Ation = "ation".ToCharArray();
        private static readonly char[] Ication = "ication".ToCharArray();

        private void IonEndings()
        {
            int old_k = k;
            if (!EndsIn('i', 'o', 'n'))
            {
                return;
            }

            if (EndsIn(Ization))
            { /*
                            * the -ize ending is very productive, so simply
                            * accept it as the root
                            */
                word.Length = j + 3;
                word.UnsafeWrite('e');
                k = j + 3;
                Lookup();
                return;
            }

            if (EndsIn(Ition))
            {
                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                if (Lookup()) /*
                     * remove -ition and add `e', and check against the
                     * dictionary
                     */
                    return; /* (e.g., definition->define, opposition->oppose) */

                /* restore original values */
                word.Length = j + 1;
                word.Append("ition".AsCharSequence());
                k = old_k;
                // nolookup()
            }
            else if (EndsIn(Ation))
            {
                word.Length = j + 3;
                word.UnsafeWrite('e');
                k = j + 3;
                if (Lookup()) /* remove -ion and add `e', and check against the dictionary */
                    return; /* (elmination -> eliminate) */

                word.Length = j + 1;
                word.UnsafeWrite('e'); /*
                              * remove -ation and add `e', and check against the
                              * dictionary
                              */
                k = j + 1;
                if (Lookup()) return;

                word.Length = j + 1;/*
                             * just remove -ation (resignation->resign) and
                             * check dictionary
                             */
                k = j;
                if (Lookup()) return;

                /* restore original values */
                word.Length = j + 1;
                word.Append("ation".AsCharSequence());
                k = old_k;
                // nolookup()

            }

            /*
             * test -ication after -ation is attempted (e.g., `complication->complicate'
             * rather than `complication->comply')
             */

            if (EndsIn(Ication))
            {
                word.Length = j + 1;
                word.UnsafeWrite('y');
                k = j + 1;
                if (Lookup()) /*
                     * remove -ication and add `y', and check against the
                     * dictionary
                     */
                    return; /* (e.g., amplification -> amplify) */

                /* restore original values */
                word.Length = j + 1;
                word.Append("ication".AsCharSequence());
                k = old_k;
                // nolookup()
            }

            // if (EndsIn(ion)) {
            if (true)
            { // we checked for this earlier... just need to set "j"
                j = k - 3; // YCS

                word.Length = j + 1;
                word.UnsafeWrite('e');
                k = j + 1;
                if (Lookup()) /* remove -ion and add `e', and check against the dictionary */
                    return;

                word.Length = j + 1;
                k = j;
                if (Lookup()) /* remove -ion, and if it's found, treat that as the root */
                    return;

                /* restore original values */
                word.Length = j + 1;
                word.Append("ion".AsCharSequence());
                k = old_k;
                // nolookup()
            }

            // nolookup(); all of the other paths restored original values
            return;
        }

        private void ErAndOrEndings()
        {
            int old_k = k;

            if (word[k] != 'r') return; // YCS

            char word_char; /* so we can remember if it was -er or -or */

            if (EndsIn('i', 'z', 'e', 'r'))
            { /*
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
                    if (Lookup()) return;
                    word.UnsafeWrite(word[j - 1]); /* restore the doubled consonant */
                }

                if (word[j] == 'i')
                { /* do we have a -ier ending? */
                    word[j] = 'y';
                    word.Length = j + 1;
                    k = j;
                    if (Lookup()) /* yes, so check against the dictionary */
                        return;
                    word[j] = 'i'; /* restore the endings */
                    word.UnsafeWrite('e');
                }

                if (word[j] == 'e')
                { /* handle -eer */
                    word.Length = j;
                    k = j - 1;
                    if (Lookup()) return;
                    word.UnsafeWrite('e');
                }

                word.Length = j + 2; /* remove the -r ending */
                k = j + 1;
                if (Lookup()) return;
                word.Length = j + 1; /* try removing -er/-or */
                k = j;
                if (Lookup()) return;
                word.UnsafeWrite('e'); /* try removing -or and adding -e */
                k = j + 1;
                if (Lookup()) return;
                word.Length = j + 1;
                word.UnsafeWrite(word_char);
                word.UnsafeWrite('r'); /* restore the word to the way it was */
                k = old_k;
                // nolookup()
            }
        }

        private void LyEndings()
        {
            int old_k = k;

            if (EndsIn('l', 'y'))
            {

                word[j + 2] = 'e'; /* try converting -ly to -le */

                if (Lookup()) return;
                word[j + 2] = 'y';

                word.Length = j + 1; /* try just removing the -ly */
                k = j;

                if (Lookup()) return;

                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 'l')) /*
                                                                              * always
                                                                              * convert
                                                                              * -
                                                                              * ally
                                                                              * to
                                                                              * -
                                                                              * al
                                                                              */
                    return;
                word.Append("ly".AsCharSequence());
                k = old_k;

                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 'b'))
                { /*
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

                if (word[j] == 'i')
                { /* e.g., militarily -> military */
                    word.Length = j;
                    word.UnsafeWrite('y');
                    k = j;
                    if (Lookup()) return;
                    word.Length = j;
                    word.Append("ily".AsCharSequence());
                    k = old_k;
                }

                word.Length = j + 1; /* the default is to remove -ly */

                k = j;
                // nolookup()... we already tried removing the "ly" variant
            }
            return;
        }

        private void AlEndings()
        {
            int old_k = k;

            if (word.Length < 4) return;
            if (EndsIn('a', 'l'))
            {
                word.Length = j + 1;
                k = j;
                if (Lookup()) /* try just removing the -al */
                    return;

                if (DoubleC(j))
                { /* allow for a doubled consonant */
                    word.Length = j;
                    k = j - 1;
                    if (Lookup()) return;
                    word.UnsafeWrite(word[j - 1]);
                }

                word.Length = j + 1;
                word.UnsafeWrite('e'); /* try removing the -al and adding -e */
                k = j + 1;
                if (Lookup()) return;

                word.Length = j + 1;
                word.Append("um".AsCharSequence()); /* try converting -al to -um */
                /* (e.g., optimal - > optimum ) */
                k = j + 2;
                if (Lookup()) return;

                word.Length = j + 1;
                word.Append("al".AsCharSequence()); /* restore the ending to the way it was */
                k = old_k;

                if ((j > 0) && (word[j - 1] == 'i') && (word[j] == 'c'))
                {
                    word.Length = j - 1; /* try removing -ical */
                    k = j - 2;
                    if (Lookup()) return;

                    word.Length = j - 1;
                    word.UnsafeWrite('y');/* try turning -ical to -y (e.g., bibliographical) */
                    k = j - 1;
                    if (Lookup()) return;

                    word.Length = j - 1;
                    word.Append("ic".AsCharSequence()); /* the default is to convert -ical to -ic */
                    k = j;
                    // nolookup() ... converting ical to ic means removing "al" which we
                    // already tried
                    // ERROR
                    Lookup();
                    return;
                }

                if (word[j] == 'i')
                { /* sometimes -ial endings should be removed */
                    word.Length = j; /* (sometimes it gets turned into -y, but we */
                    k = j - 1; /* aren't dealing with that case for now) */
                    if (Lookup()) return;
                    word.Append("ial".AsCharSequence());
                    k = old_k;
                    Lookup();
                }

            }
            return;
        }

        private void IveEndings()
        {
            int old_k = k;

            if (EndsIn('i', 'v', 'e'))
            {
                word.Length = j + 1; /* try removing -ive entirely */
                k = j;
                if (Lookup()) return;

                word.UnsafeWrite('e'); /* try removing -ive and adding -e */
                k = j + 1;
                if (Lookup()) return;
                word.Length = j + 1;
                word.Append("ive".AsCharSequence());
                if ((j > 0) && (word[j - 1] == 'a') && (word[j] == 't'))
                {
                    word[j - 1] = 'e'; /* try removing -ative and adding -e */
                    word.Length = j; /* (e.g., determinative -> determine) */
                    k = j - 1;
                    if (Lookup()) return;
                    word.Length = j - 1; /* try just removing -ative */
                    if (Lookup()) return;

                    word.Append("ative".AsCharSequence());
                    k = old_k;
                }

                /* try mapping -ive to -ion (e.g., injunctive/injunction) */
                word[j + 2] = 'o';
                word[j + 3] = 'n';
                if (Lookup()) return;

                word[j + 2] = 'v'; /* restore the original values */
                word[j + 3] = 'e';
                k = old_k;
                // nolookup()
            }
            return;
        }

        protected internal KStemmer()
        {
        }

        protected internal virtual string Stem(string term)
        {
            var changed = Stem(term.ToCharArray(), term.Length);
            return !changed ? term : AsString;
        }

        protected internal virtual string AsString
        {
            get
            {
                string s = String;
                return s ?? word.ToString();
            }
        }

        protected internal virtual ICharSequence AsCharSequence
        {
            get { return result.AsCharSequence() ?? word; }
        }

        protected internal virtual string String
        {
            get { return result; }
        }

        protected internal virtual char[] Chars
        {
            get { return word.GetArray(); }
        }

        protected internal virtual int Length
        {
            get { return word.Length; }
        }

        protected internal string result;

        private bool Matched()
        {
            return matchedEntry != null;
        }

        protected internal virtual bool Stem(char[] term, int len)
        {
            result = null;

            k = len - 1;
            if ((k <= 1) || (k >= MaxWordLen - 1))
            {
                return false; // don't stem
            }

            // first check the stemmer dictionaries, and avoid using the cache if if it's in there
            var entry = dict_ht.Get(term, 0, len);
            if (entry != null)
            {
                if (entry.root != null)
                {
                    result = entry.root;
                    return true;
                }
                return false;
            }

            word.Reset();
            word.Reserve(len + 10);
            for (var i = 0; i < len; i++)
            {
                var ch = term[i];
                if (!IsAlpha(ch)) return false;
                word.UnsafeWrite(ch);
            }

            matchedEntry = null;

            while (true)
            {
                Plural();
                if (Matched()) break;
                PastTense();
                if (Matched()) break;
                Aspect();
                if (Matched()) break;
                ItyEndings();
                if (Matched()) break;
                NessEndings();
                if (Matched()) break;
                IonEndings();
                if (Matched()) break;
                ErAndOrEndings();
                if (Matched()) break;
                LyEndings();
                if (Matched()) break;
                AlEndings();
                if (Matched()) break;
                entry = WordInDict();
                IveEndings();
                if (Matched()) break;
                IzeEndings();
                if (Matched()) break;
                MentEndings();
                if (Matched()) break;
                BleEndings();
                if (Matched()) break;
                IsmEndings();
                if (Matched()) break;
                IcEndings();
                if (Matched()) break;
                NcyEndings();
                if (Matched()) break;
                NceEndings();
                if (Matched()) break;
                Matched();
                break;
            }

            entry = matchedEntry;
            if (entry != null)
            {
                result = entry.root;
            }

            return true;
        }
    }
}
