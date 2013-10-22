using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.En
{
    /// <summary>
    /// Stemmer, implementing the Porter Stemming Algorithm
    /// 
    /// The Stemmer class transforms a word into its root form.  The input
    /// word can be provided a character at time (by calling add()), or at once
    /// by calling one of the various stem(something) methods.
    /// </summary>
    internal class PorterStemmer
    {
        private char[] b;
        private int i, /* offset into b */
            j, k, k0;
        private bool dirty = false;
        private const int INITIAL_SIZE = 50;


        public PorterStemmer()
        {
            b = new char[INITIAL_SIZE];
            i = 0;
        }


        /// <summary>
        /// Reset() resets the stemmer so it can stem another word. If you invoke
        /// the stemmer by calling Add(char) and then stem(), you must call Reset()
        /// before starting another word.
        /// </summary>
        public virtual void Reset()
        {
            i = 0;
            dirty = false;
        }


        /// <summary>
        /// Add a character to the word being stemmed. When you are finished
        /// adding characters, you can call Stem(void) to process the word.
        /// </summary>
        /// <param name="ch"></param>
        public virtual void Add(char ch)
        {
            if (b.Length <= i)
            {
                b = ArrayUtil.Grow(b, i + i);
            }
            b[i++] = ch;
        }


        /// <summary>
        /// After a word has been stemmed, it can be retrieved by ToString(),
        /// or a reference to the internal buffer can be retrieved by ResultBuffer
        /// and ResultLength (which is generally more efficient.)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new string(b, 0, i);
        }


        /// <summary>
        /// Returns the length of the word resulting from the stemming process.
        /// </summary>
        public virtual int ResultLength
        {
            get { return i; }
        }


        /// <summary>
        /// Returns a reference to a character buffer containing the results of
        /// the stemming process. You also need to consult ResultLength
        /// to determine the length of the result.
        /// </summary>
        public virtual int ResultBuffer
        {
            get { return b; }
        }


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
                    return (i == k0) || !Cons(i - 1);
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
              .... */
        private int M()
        {
            var n = 0;
            var i = k0;
            while (true)
            {
                if (i > j)
                    return n;
                if (!Cons(i))
                    break;
                i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j)
                        return n;
                    if (Cons(i))
                        break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j)
                        return n;
                    if (!Cons(i))
                        break;
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
                if (!Cons(i))
                    return true;
            return false;
        }


        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool DoubleC(int j)
        {
            if (j < k0 + 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return Cons(j);
        }


        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
         and also if the second c is not w,x or y. this is used when trying to
         restore an e at the end of a short word. e.g.

              cav(e), lov(e), hop(e), crim(e), but
              snow, box, tray. */
        private bool Cvc(int i)
        {
            if (i < k0 + 2 || !Cons(i) || Cons(i - 1) || !Cons(i - 2))
                return false;
            else
            {
                int ch = b[i];
                if (ch == 'w' || ch == 'x' || ch == 'y') return false;
            }
            return true;
        }



        private bool Ends(string s)
        {
            var l = s.Length;
            var o = k - l + 1;
            if (o < k0)
                return false;
            for (var i = 0; i < l; i++)
                if (b[o + i] != s[i])
                    return false;
            j = k - l;
            return true;
        }


        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting k. */
        protected internal void SetTo(string s)
        {
            var l = s.Length;
            var o = j + 1;
            for (var i = 0; i < l; i++)
                b[o + i] = s[i];
            k = j + l;
            dirty = true;
        }


        /* r(s) is used further down. */
        protected internal void R(string s)
        {
            if (M() > 0) SetTo(s);
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

        }


        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void Step2()
        {

        }


        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
             -ation) maps to -ize etc. note that the string before the suffix must give
             m() > 0. */
        private void Step3()
        {

        }


        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void Step4()
        {

        }


        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void Step5()
        {

        }


        /* step6() removes a final -e if m() > 1. */
        private void Step6()
        {

        }


        /// <summary>
        /// Stem a word provided as a string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>The result as a string.</returns>
        public string Stem(string s)
        {

        }


        /// <summary>
        /// Stem a word contained in a char[].  Returns true if the stemming process
        /// resulted in a word different from the input.  You can retrieve the
        /// result with getResultLength()/getResultBuffer() or toString().
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool Stem(char[] word)
        {

        }


        /// <summary>
        /// Stem a word contained in a portion of a char[] array.  Returns
        /// true if the stemming process resulted in a word different from
        /// the input.  You can retrieve the result with
        /// getResultLength()/getResultBuffer() or toString().
        /// </summary>
        /// <param name="wordBuffer"></param>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        /// <returns></returns>
        public bool Stem(char[] wordBuffer, int offset, int wordLen)
        {

        }


        /// <summary>
        /// Stem a word contained in a leading portion of a char[] array.
        /// Returns true if the stemming process resulted in a word different
        /// from the input.  You can retrieve the result with
        /// getResultLength()/getResultBuffer() or toString().
        /// </summary>
        /// <param name="word"></param>
        /// <param name="wordLen"></param>
        /// <returns></returns>
        public bool Stem(char[] word, int wordLen)
        {

        }


        /// <summary>
        /// Stem the word placed into the Stemmer buffer through calls to add().
        /// Returns true if the stemming process resulted in a word different
        /// from the input.  You can retrieve the result with
        /// getResultLength()/getResultBuffer() or toString().
        /// </summary>
        /// <returns></returns>
        public bool Stem()
        {

        }


        public bool Stem(int i0)
        {

        }



    }
}
