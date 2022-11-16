// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Analysis.En
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

    /*
    
       Porter stemmer in .NET. The original paper is in
    
           Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
           no. 3, pp 130-137,
    
       See also http://www.tartarus.org/~martin/PorterStemmer/index.html
    
       Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
       Tthe words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
       is then out outside the bounds of b.
    
       Similarly,
    
       Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
       'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
       b[j] is then outside the bounds of b.
    
       Release 3.
    
       [ This version is derived from Release 3, modified by Brian Goetz to
         optimize for fewer object creations.  ]
    
    */

    /// 
    /// <summary>
    /// Stemmer, implementing the Porter Stemming Algorithm
    /// 
    /// The Stemmer class transforms a word into its root form.  The input
    /// word can be provided a character at time (by calling <see cref="Add"/>), or at once
    /// by calling one of the various Stem methods, such as <see cref="Stem(string)"/>.
    /// </summary>
    internal class PorterStemmer
    {
        private char[] b;
        private int i, j, k, k0; // offset into b
        private bool dirty = false;
        private const int INITIAL_SIZE = 50;

        public PorterStemmer()
        {
            b = new char[INITIAL_SIZE];
            i = 0;
        }

        /// <summary>
        /// <see cref="Reset"/> resets the stemmer so it can stem another word.  If you invoke
        /// the stemmer by calling <see cref="Add(char)"/> and then <see cref="Stem()"/>, you must call <see cref="Reset"/>
        /// before starting another word.
        /// </summary>
        public virtual void Reset()
        {
            i = 0;
            dirty = false;
        }

        /// <summary>
        /// Add a character to the word being stemmed.  When you are finished
        /// adding characters, you can call <see cref="Stem()"/> to process the word.
        /// </summary>
        public virtual void Add(char ch)
        {
            if (b.Length <= i)
            {
                b = ArrayUtil.Grow(b, i + 1);
            }
            b[i++] = ch;
        }

        /// <summary>
        /// After a word has been stemmed, it can be retrieved by <see cref="ToString"/>,
        /// or a reference to the internal buffer can be retrieved by <see cref="ResultBuffer"/>
        /// and <see cref="ResultLength"/> (which is generally more efficient.)
        /// </summary>
        public override string ToString()
        {
            return new string(b, 0, i);
        }

        /// <summary>
        /// Returns the length of the word resulting from the stemming process.
        /// </summary>
        public virtual int ResultLength => i;

        /// <summary>
        /// Returns a reference to a character buffer containing the results of
        /// the stemming process.  You also need to consult <see cref="ResultLength"/>
        /// to determine the length of the result.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] ResultBuffer => b;

        /* cons(i) is true <=> b[i] is a consonant. */

        private bool Cons(int i)
        {
            switch (b[i])
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    return false;
                case 'y':
                    return (i == k0) ? true : !Cons(i - 1);
                default:
                    return true;
            }
        }

        /* m() measures the number of consonant sequences between k0 and j. if c is
           a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
           presence,

                <c><v>       gives 0
                <c>vc<v>     gives 1
                <c>vcvc<v>   gives 2
                <c>vcvcvc<v> gives 3
                ....
        */

        private int M()
        {
            int n = 0;
            int i = k0;
            while (true)
            {
                if (i > j)
                {
                    return n;
                }
                if (!Cons(i))
                {
                    break;
                }
                i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j)
                    {
                        return n;
                    }
                    if (Cons(i))
                    {
                        break;
                    }
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j)
                    {
                        return n;
                    }
                    if (!Cons(i))
                    {
                        break;
                    }
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> k0,...j contains a vowel */

        private bool VowelInStem()
        {
            int i;
            for (i = k0; i <= j; i++)
            {
                if (!Cons(i))
                {
                    return true;
                }
            }
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */

        private bool DoubleC(int j)
        {
            if (j < k0 + 1)
            {
                return false;
            }
            if (b[j] != b[j - 1])
            {
                return false;
            }
            return Cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
           and also if the second c is not w,x or y. this is used when trying to
           restore an e at the end of a short word. e.g.

                cav(e), lov(e), hop(e), crim(e), but
                snow, box, tray.

        */

        private bool Cvc(int i)
        {
            if (i < k0 + 2 || !Cons(i) || Cons(i - 1) || !Cons(i - 2))
            {
                return false;
            }
            else
            {
                int ch = b[i];
                if (ch == 'w' || ch == 'x' || ch == 'y')
                {
                    return false;
                }
            }
            return true;
        }

        private bool Ends(string s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < k0)
            {
                return false;
            }
            for (int i = 0; i < l; i++)
            {
                if (b[o + i] != s[i])
                {
                    return false;
                }
            }
            j = k - l;
            return true;
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
           k. */

        internal virtual void SetTo(string s)
        {
            int l = s.Length;
            int o = j + 1;
            for (int i = 0; i < l; i++)
            {
                b[o + i] = s[i];
            }
            k = j + l;
            dirty = true;
        }

        /* r(s) is used further down. */

        internal virtual void r(string s)
        {
            if (M() > 0)
            {
                SetTo(s);
            }
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.

                 caresses  ->  caress
                 ponies    ->  poni
                 ties      ->  ti
                 caress    ->  caress
                 cats      ->  cat

                 feed      ->  feed
                 agreed    ->  agree
                 disabled  ->  disable

                 matting   ->  mat
                 mating    ->  mate
                 meeting   ->  meet
                 milling   ->  mill
                 messing   ->  mess

                 meetings  ->  meet

        */

        private void Step1()
        {
            if (b[k] == 's')
            {
                if (Ends("sses"))
                {
                    k -= 2;
                }
                else if (Ends("ies"))
                {
                    SetTo("i");
                }
                else if (b[k - 1] != 's')
                {
                    k--;
                }
            }
            if (Ends("eed"))
            {
                if (M() > 0)
                {
                    k--;
                }
            }
            else if ((Ends("ed") || Ends("ing")) && VowelInStem())
            {
                k = j;
                if (Ends("at"))
                {
                    SetTo("ate");
                }
                else if (Ends("bl"))
                {
                    SetTo("ble");
                }
                else if (Ends("iz"))
                {
                    SetTo("ize");
                }
                else if (DoubleC(k))
                {
                    int ch = b[k--];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                    {
                        k++;
                    }
                }
                else if (M() == 1 && Cvc(k))
                {
                    SetTo("e");
                }
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */

        private void Step2()
        {
            if (Ends("y") && VowelInStem())
            {
                b[k] = 'i';
                dirty = true;
            }
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
           -ation) maps to -ize etc. note that the string before the suffix must give
           m() > 0. */

        private void Step3()
        {
            if (k == k0) // For Bug 1
            {
                return;
            }
            switch (b[k - 1])
            {
                case 'a':
                    if (Ends("ational"))
                    {
                        r("ate");
                        break;
                    }
                    if (Ends("tional"))
                    {
                        r("tion");
                        break;
                    }
                    break;
                case 'c':
                    if (Ends("enci"))
                    {
                        r("ence");
                        break;
                    }
                    if (Ends("anci"))
                    {
                        r("ance");
                        break;
                    }
                    break;
                case 'e':
                    if (Ends("izer"))
                    {
                        r("ize");
                        break;
                    }
                    break;
                case 'l':
                    if (Ends("bli"))
                    {
                        r("ble");
                        break;
                    }
                    if (Ends("alli"))
                    {
                        r("al");
                        break;
                    }
                    if (Ends("entli"))
                    {
                        r("ent");
                        break;
                    }
                    if (Ends("eli"))
                    {
                        r("e");
                        break;
                    }
                    if (Ends("ousli"))
                    {
                        r("ous");
                        break;
                    }
                    break;
                case 'o':
                    if (Ends("ization"))
                    {
                        r("ize");
                        break;
                    }
                    if (Ends("ation"))
                    {
                        r("ate");
                        break;
                    }
                    if (Ends("ator"))
                    {
                        r("ate");
                        break;
                    }
                    break;
                case 's':
                    if (Ends("alism"))
                    {
                        r("al");
                        break;
                    }
                    if (Ends("iveness"))
                    {
                        r("ive");
                        break;
                    }
                    if (Ends("fulness"))
                    {
                        r("ful");
                        break;
                    }
                    if (Ends("ousness"))
                    {
                        r("ous");
                        break;
                    }
                    break;
                case 't':
                    if (Ends("aliti"))
                    {
                        r("al");
                        break;
                    }
                    if (Ends("iviti"))
                    {
                        r("ive");
                        break;
                    }
                    if (Ends("biliti"))
                    {
                        r("ble");
                        break;
                    }
                    break;
                case 'g':
                    if (Ends("logi"))
                    {
                        r("log");
                        break;
                    }
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */

        private void Step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (Ends("icate"))
                    {
                        r("ic");
                        break;
                    }
                    if (Ends("ative"))
                    {
                        r("");
                        break;
                    }
                    if (Ends("alize"))
                    {
                        r("al");
                        break;
                    }
                    break;
                case 'i':
                    if (Ends("iciti"))
                    {
                        r("ic");
                        break;
                    }
                    break;
                case 'l':
                    if (Ends("ical"))
                    {
                        r("ic");
                        break;
                    }
                    if (Ends("ful"))
                    {
                        r("");
                        break;
                    }
                    break;
                case 's':
                    if (Ends("ness"))
                    {
                        r("");
                        break;
                    }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */

        private void Step5()
        {
            if (k == k0) // for Bug 1
            {
                return;
            }
            switch (b[k - 1])
            {
                case 'a':
                    if (Ends("al"))
                    {
                        break;
                    }
                    return;
                case 'c':
                    if (Ends("ance"))
                    {
                        break;
                    }
                    if (Ends("ence"))
                    {
                        break;
                    }
                    return;
                case 'e':
                    if (Ends("er"))
                    {
                        break;
                    }
                    return;
                case 'i':
                    if (Ends("ic"))
                    {
                        break;
                    }
                    return;
                case 'l':
                    if (Ends("able"))
                    {
                        break;
                    }
                    if (Ends("ible"))
                    {
                        break;
                    }
                    return;
                case 'n':
                    if (Ends("ant"))
                    {
                        break;
                    }
                    if (Ends("ement"))
                    {
                        break;
                    }
                    if (Ends("ment"))
                    {
                        break;
                    }
                    /* element etc. not stripped before the m */
                    if (Ends("ent"))
                    {
                        break;
                    }
                    return;
                case 'o':
                    if (Ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't'))
                    {
                        break;
                    }
                    /* j >= 0 fixes Bug 2 */
                    if (Ends("ou"))
                    {
                        break;
                    }
                    return;
                /* takes care of -ous */
                case 's':
                    if (Ends("ism"))
                    {
                        break;
                    }
                    return;
                case 't':
                    if (Ends("ate"))
                    {
                        break;
                    }
                    if (Ends("iti"))
                    {
                        break;
                    }
                    return;
                case 'u':
                    if (Ends("ous"))
                    {
                        break;
                    }
                    return;
                case 'v':
                    if (Ends("ive"))
                    {
                        break;
                    }
                    return;
                case 'z':
                    if (Ends("ize"))
                    {
                        break;
                    }
                    return;
                default:
                    return;
            }
            if (M() > 1)
            {
                k = j;
            }
        }

        /* step6() removes a final -e if m() > 1. */

        private void Step6()
        {
            j = k;
            if (b[k] == 'e')
            {
                int a = M();
                if (a > 1 || a == 1 && !Cvc(k - 1))
                {
                    k--;
                }
            }
            if (b[k] == 'l' && DoubleC(k) && M() > 1)
            {
                k--;
            }
        }


        /// <summary>
        /// Stem a word provided as a <see cref="string"/>.  Returns the result as a <see cref="string"/>.
        /// </summary>
        public virtual string Stem(string s)
        {
            if (Stem(s.ToCharArray(), s.Length))
            {
                return ToString();
            }
            else
            {
                return s;
            }
        }

        /// <summary>
        /// Stem a word contained in a <see cref="T:char[]"/>.  Returns true if the stemming process
        /// resulted in a word different from the input.  You can retrieve the
        /// result with <see cref="ResultLength"/>/<see cref="ResultBuffer"/> or <see cref="ToString"/>.
        /// </summary>
        public virtual bool Stem(char[] word)
        {
            return Stem(word, word.Length);
        }

        /// <summary>
        /// Stem a word contained in a portion of a <see cref="T:char[]"/> array.  Returns
        /// true if the stemming process resulted in a word different from
        /// the input.  You can retrieve the result with
        /// <see cref="ResultLength"/>/<see cref="ResultBuffer"/> or <see cref="ToString"/>.
        /// </summary>
        public virtual bool Stem(char[] wordBuffer, int offset, int wordLen)
        {
            Reset();
            if (b.Length < wordLen)
            {
                b = new char[ArrayUtil.Oversize(wordLen, RamUsageEstimator.NUM_BYTES_CHAR)];
            }
            Arrays.Copy(wordBuffer, offset, b, 0, wordLen);
            i = wordLen;
            return Stem(0);
        }

        /// <summary>
        /// Stem a word contained in a leading portion of a <see cref="T:char[]"/> array.
        /// Returns true if the stemming process resulted in a word different
        /// from the input.  You can retrieve the result with
        /// <see cref="ResultLength"/>/<see cref="ResultBuffer"/> or <see cref="ToString"/>.
        /// </summary>
        public virtual bool Stem(char[] word, int wordLen)
        {
            return Stem(word, 0, wordLen);
        }

        /// <summary>
        /// Stem the word placed into the Stemmer buffer through calls to <see cref="Add"/>.
        /// Returns true if the stemming process resulted in a word different
        /// from the input.  You can retrieve the result with
        /// <see cref="ResultLength"/>/<see cref="ResultBuffer"/> or <see cref="ToString"/>.
        /// </summary>
        public virtual bool Stem()
        {
            return Stem(0);
        }

        public virtual bool Stem(int i0)
        {
            k = i - 1;
            k0 = i0;
            if (k > k0 + 1)
            {
                Step1();
                Step2();
                Step3();
                Step4();
                Step5();
                Step6();
            }
            // Also, a word is considered dirty if we lopped off letters
            // Thanks to Ifigenia Vairelles for pointing this out.
            if (i != k + 1)
            {
                dirty = true;
            }
            i = k + 1;
            return dirty;
        }

        //// using Lucene.Net.Analysis.En.PorterStemmer
        //// using System;
        //// using System.Globalization;
        //// using System.IO;
        ///// <summary>
        ///// Test program for demonstrating the Stemmer.  It reads a file and
        ///// stems each word, writing the result to standard out.
        ///// </summary>
        ///// <param name="args">Usage: Stemmer file-name</param>
        //public static void Main(string[] args)
        //{
        //    PorterStemmer s = new PorterStemmer();

        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        try
        //        {
        //            using (Stream input = new FileStream(args[i], FileMode.Open, FileAccess.Read))
        //            {
        //                byte[] buffer = new byte[1024];
        //                int bufferLen, offset, ch;

        //                bufferLen = input.Read(buffer, 0, buffer.Length);
        //                offset = 0;
        //                s.Reset();

        //                while (true)
        //                {
        //                    if (offset < bufferLen)
        //                        ch = buffer[offset++];
        //                    else
        //                    {
        //                        bufferLen = input.Read(buffer, 0, buffer.Length);
        //                        offset = 0;
        //                        if (bufferLen < 0)
        //                            ch = -1;
        //                        else
        //                            ch = buffer[offset++];
        //                    }

        //                    if (Character.IsLetter(ch))
        //                    {
        //                        s.Add(Character.ToLower(ch, CultureInfo.InvariantCulture));
        //                    }
        //                    else
        //                    {
        //                        s.Stem();
        //                        Console.WriteLine(s.ToString());
        //                        s.Reset();
        //                        if (ch < 0)
        //                            break;
        //                        else
        //                        {
        //                            Console.Write((char)ch);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e) when (ex.IsIOException())
        //        {
        //            Console.WriteLine("error reading " + args[i]);
        //            Console.WriteLine(e.ToString());
        //        }
        //    }
        //}
    }
}