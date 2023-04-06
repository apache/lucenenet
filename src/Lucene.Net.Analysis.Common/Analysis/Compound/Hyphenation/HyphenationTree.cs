// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     * 
     *      http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This tree structure stores the hyphenation patterns in an efficient way for
    /// fast lookup. It provides the provides the method to hyphenate a word.
    /// <para/>
    /// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
    ///
    /// Lucene.NET specific note:
    /// If you are going to extend this class by inheriting from it, you should be aware that the
    /// base class TernaryTree initializes its state in the constructor by calling its protected Init() method.
    /// If your subclass needs to initialize its own state, you add your own "Initialize()" method
    /// and call it both from the inside of your constructor and you will need to override the Balance() method
    /// and call "Initialize()" before the call to base.Balance().
    /// Your class can use the data that is initialized in the base class after the call to base.Balance().
    ///
    /// </summary>
    public class HyphenationTree : TernaryTree, IPatternConsumer
    {
        /// <summary>
        /// value space: stores the interletter values
        /// </summary>
        protected ByteVector m_vspace;

        /// <summary>
        /// This map stores hyphenation exceptions
        /// </summary>
        protected IDictionary<string, IList<object>> m_stoplist;

        /// <summary>
        /// This map stores the character classes
        /// </summary>
        protected TernaryTree m_classmap;

        /// <summary>
        /// Temporary map to store interletter values on pattern loading.
        /// </summary>
        private TernaryTree ivalues;

        public HyphenationTree()
        {
            m_stoplist = new JCG.Dictionary<string, IList<object>>(23); // usually a small table
            m_classmap = new TernaryTree();
            m_vspace = new ByteVector();
            m_vspace.Alloc(1); // this reserves index 0, which we don't use
        }

        /// <summary>
        /// Packs the values by storing them in 4 bits, two values into a byte Values
        /// range is from 0 to 9. We use zero as terminator, so we'll add 1 to the
        /// value.
        /// </summary>
        /// <param name="values"> a string of digits from '0' to '9' representing the
        ///        interletter values. </param>
        /// <returns> the index into the vspace array where the packed values are stored. </returns>
        protected virtual int PackValues(string values)
        {
            int i, n = values.Length;
            int m = (n & 1) == 1 ? (n >> 1) + 2 : (n >> 1) + 1;
            int offset = m_vspace.Alloc(m);
            byte[] va = m_vspace.Array;
            for (i = 0; i < n; i++)
            {
                int j = i >> 1;
                byte v = (byte)((values[i] - '0' + 1) & 0x0f);
                if ((i & 1) == 1)
                {
                    va[j + offset] = (byte)(va[j + offset] | v);
                }
                else
                {
                    va[j + offset] = (byte)(v << 4); // big endian
                }
            }
            va[m - 1 + offset] = 0; // terminator
            return offset;
        }

        protected virtual string UnpackValues(int k)
        {
            StringBuilder buf = new StringBuilder();
            byte v = m_vspace[k++];
            while (v != 0)
            {
                char c = (char)(v.TripleShift(4) - 1 + '0');
                buf.Append(c);
                c = (char)(v & 0x0f);
                if (c == 0)
                {
                    break;
                }
                c = (char)(c - 1 + '0');
                buf.Append(c);
                v = m_vspace[k++];
            }
            return buf.ToString();
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="filename"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(string filename)
        {
            LoadPatterns(filename, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="filename"> the filename </param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(string filename, Encoding encoding)
        {
            var src = new FileStream(filename, FileMode.Open, FileAccess.Read);
            LoadPatterns(src, encoding);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> a <see cref="FileInfo"/> object representing the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(FileInfo f)
        {
            LoadPatterns(f, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> a <see cref="FileInfo"/> object representing the file </param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(FileInfo f, Encoding encoding)
        {
            var src = new FileStream(f.FullName, FileMode.Open, FileAccess.Read);
            LoadPatterns(src, encoding);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="source"> <see cref="Stream"/> input source for the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(Stream source)
        {
            LoadPatterns(source, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="source"> <see cref="Stream"/> input source for the file </param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(Stream source, Encoding encoding)
        {
            var xmlReaderSettings =
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new PatternParser.DtdResolver()
                };

            using var reader = XmlReader.Create(new StreamReader(source, encoding), xmlReaderSettings);
            LoadPatterns(reader);
        }

        /// <summary>
        /// Read hyphenation patterns from an <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="source"> <see cref="XmlReader"/> input source for the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(XmlReader source)
        {
            PatternParser pp = new PatternParser(this);
            ivalues = new TernaryTree();

            pp.Parse(source);

            // patterns/values should be now in the tree
            // let's optimize a bit
            TrimToSize();
            m_vspace.TrimToSize();
            m_classmap.TrimToSize();

            // get rid of the auxiliary map
            ivalues = null;
        }

        public virtual string FindPattern(string pat)
        {
            int k = base.Find(pat);
            if (k >= 0)
            {
                return UnpackValues(k);
            }
            return "";
        }

        /// <summary>
        /// String compare, returns 0 if equal or t is a substring of s
        /// </summary>
        protected virtual int HStrCmp(char[] s, int si, char[] t, int ti)
        {
            for (; s[si] == t[ti]; si++, ti++)
            {
                if (s[si] == 0)
                {
                    return 0;
                }
            }
            if (t[ti] == 0)
            {
                return 0;
            }
            return s[si] - t[ti];
        }

        protected virtual byte[] GetValues(int k)
        {
            StringBuilder buf = new StringBuilder();
            byte v = m_vspace[k++];
            while (v != 0)
            {
                char c = (char)(v.TripleShift(4) - 1);
                buf.Append(c);
                c = (char)(v & 0x0f);
                if (c == 0)
                {
                    break;
                }
                c = (char)(c - 1);
                buf.Append(c);
                v = m_vspace[k++];
            }
            byte[] res = new byte[buf.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (byte)buf[i];
            }
            return res;
        }

        /// <summary>
        /// <para>
        /// Search for all possible partial matches of word starting at index an update
        /// interletter values. In other words, it does something like:
        /// </para>
        /// <code>
        /// for (i=0; i&lt;patterns.Length; i++) 
        /// {
        ///     if (word.Substring(index).StartsWith(patterns[i], StringComparison.Ordinal))
        ///         update_interletter_values(patterns[i]);
        /// }
        /// </code>
        /// <para>
        /// But it is done in an efficient way since the patterns are stored in a
        /// ternary tree. In fact, this is the whole purpose of having the tree: doing
        /// this search without having to test every single pattern. The number of
        /// patterns for languages such as English range from 4000 to 10000. Thus,
        /// doing thousands of string comparisons for each word to hyphenate would be
        /// really slow without the tree. The tradeoff is memory, but using a ternary
        /// tree instead of a trie, almost halves the the memory used by Lout or TeX.
        /// It's also faster than using a hash table
        /// </para>
        /// </summary>
        /// <param name="word"> null terminated word to match </param>
        /// <param name="index"> start index from word </param>
        /// <param name="il"> interletter values array to update </param>
        protected virtual void SearchPatterns(char[] word, int index, byte[] il)
        {
            byte[] values;
            int i = index;
            char p, q;
            char sp = word[i];
            p = m_root;

            while (p > 0 && p < m_sc.Length)
            {
                if (m_sc[p] == 0xFFFF)
                {
                    if (HStrCmp(word, i, m_kv.Array, m_lo[p]) == 0)
                    {
                        values = GetValues(m_eq[p]); // data pointer is in eq[]
                        int j = index;
                        for (int k = 0; k < values.Length; k++)
                        {
                            if (j < il.Length && values[k] > il[j])
                            {
                                il[j] = values[k];
                            }
                            j++;
                        }
                    }
                    return;
                }
                int d = sp - m_sc[p];
                if (d == 0)
                {
                    if (sp == 0)
                    {
                        break;
                    }
                    sp = word[++i];
                    p = m_eq[p];
                    q = p;

                    // look for a pattern ending at this position by searching for
                    // the null char ( splitchar == 0 )
                    while (q > 0 && q < m_sc.Length)
                    {
                        if (m_sc[q] == 0xFFFF) // stop at compressed branch
                        {
                            break;
                        }
                        if (m_sc[q] == 0)
                        {
                            values = GetValues(m_eq[q]);
                            int j = index;
                            for (int k = 0; k < values.Length; k++)
                            {
                                if (j < il.Length && values[k] > il[j])
                                {
                                    il[j] = values[k];
                                }
                                j++;
                            }
                            break;
                        }
                        else
                        {
                            q = m_lo[q];

                            // actually the code should be: q = sc[q] < 0 ? hi[q] : lo[q]; but
                            // java chars are unsigned
                        }
                    }
                }
                else
                {
                    p = d < 0 ? m_lo[p] : m_hi[p];
                }
            }
        }

        /// <summary>
        /// Hyphenate word and return a <see cref="Hyphenation"/> object.
        /// </summary>
        /// <param name="word"> the word to be hyphenated </param>
        /// <param name="remainCharCount"> Minimum number of characters allowed before the
        ///        hyphenation point. </param>
        /// <param name="pushCharCount"> Minimum number of characters allowed after the
        ///        hyphenation point. </param>
        /// <returns> a <see cref="Hyphenation"/> object representing the
        ///         hyphenated word or null if word is not hyphenated. </returns>
        public virtual Hyphenation Hyphenate(string word, int remainCharCount, int pushCharCount)
        {
            char[] w = word.ToCharArray();
            return Hyphenate(w, 0, w.Length, remainCharCount, pushCharCount);
        }



        /// <summary>
        /// Hyphenate word and return an array of hyphenation points.
        /// </summary>
        /// <remarks>
        /// w = "****nnllllllnnn*****", where n is a non-letter, l is a letter, all n
        /// may be absent, the first n is at offset, the first l is at offset +
        /// iIgnoreAtBeginning; word = ".llllll.'\0'***", where all l in w are copied
        /// into word. In the first part of the routine len = w.length, in the second
        /// part of the routine len = word.length. Three indices are used: index(w),
        /// the index in w, index(word), the index in word, letterindex(word), the
        /// index in the letter part of word. The following relations exist: index(w) =
        /// offset + i - 1 index(word) = i - iIgnoreAtBeginning letterindex(word) =
        /// index(word) - 1 (see first loop). It follows that: index(w) - index(word) =
        /// offset - 1 + iIgnoreAtBeginning index(w) = letterindex(word) + offset +
        /// iIgnoreAtBeginning
        /// </remarks>
        /// <param name="w"> char array that contains the word </param>
        /// <param name="offset"> Offset to first character in word </param>
        /// <param name="len"> Length of word </param>
        /// <param name="remainCharCount"> Minimum number of characters allowed before the
        ///        hyphenation point. </param>
        /// <param name="pushCharCount"> Minimum number of characters allowed after the
        ///        hyphenation point. </param>
        /// <returns> a <see cref="Hyphenation"/> object representing the
        ///         hyphenated word or null if word is not hyphenated. </returns>
        public virtual Hyphenation Hyphenate(char[] w, int offset, int len, int remainCharCount, int pushCharCount)
        {
            int i;
            char[] word = new char[len + 3];

            // normalize word
            char[] c = new char[2];
            int iIgnoreAtBeginning = 0;
            int iLength = len;
            bool bEndOfLetters = false;
            for (i = 1; i <= len; i++)
            {
                c[0] = w[offset + i - 1];
                int nc = m_classmap.Find(c, 0);
                if (nc < 0) // found a non-letter character ...
                {
                    if (i == (1 + iIgnoreAtBeginning))
                    {
                        // ... before any letter character
                        iIgnoreAtBeginning++;
                    }
                    else
                    {
                        // ... after a letter character
                        bEndOfLetters = true;
                    }
                    iLength--;
                }
                else
                {
                    if (!bEndOfLetters)
                    {
                        word[i - iIgnoreAtBeginning] = (char)nc;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            len = iLength;
            if (len < (remainCharCount + pushCharCount))
            {
                // word is too short to be hyphenated
                return null;
            }
            int[] result = new int[len + 1];
            int k = 0;

            // check exception list first
            string sw = new string(word, 1, len);
            // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
            if (m_stoplist.TryGetValue(sw, out IList<object> hw))
            {
                // assume only simple hyphens (Hyphen.pre="-", Hyphen.post = Hyphen.no =
                // null)
                int j = 0;
                for (i = 0; i < hw.Count; i++)
                {
                    object o = hw[i];
                    // j = index(sw) = letterindex(word)?
                    // result[k] = corresponding index(w)
                    if (o is string)
                    {
                        j += ((string)o).Length;
                        if (j >= remainCharCount && j < (len - pushCharCount))
                        {
                            result[k++] = j + iIgnoreAtBeginning;
                        }
                    }
                }
            }
            else
            {
                // use algorithm to get hyphenation points
                word[0] = '.'; // word start marker
                word[len + 1] = '.'; // word end marker
                word[len + 2] = (char)0; // null terminated
                byte[] il = new byte[len + 3]; // initialized to zero
                for (i = 0; i < len + 1; i++)
                {
                    SearchPatterns(word, i, il);
                }

                // hyphenation points are located where interletter value is odd
                // i is letterindex(word),
                // i + 1 is index(word),
                // result[k] = corresponding index(w)
                for (i = 0; i < len; i++)
                {
                    if (((il[i + 1] & 1) == 1) && i >= remainCharCount && i <= (len - pushCharCount))
                    {
                        result[k++] = i + iIgnoreAtBeginning;
                    }
                }
            }

            if (k > 0)
            {
                // trim result array
                int[] res = new int[k + 2];
                Arrays.Copy(result, 0, res, 1, k);
                // We add the synthetical hyphenation points
                // at the beginning and end of the word
                res[0] = 0;
                res[k + 1] = len;
                return new Hyphenation(res);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Add a character class to the tree. It is used by
        /// <see cref="PatternParser"/> as callback to add character classes.
        /// Character classes define the valid word characters for hyphenation. If a
        /// word contains a character not defined in any of the classes, it is not
        /// hyphenated. It also defines a way to normalize the characters in order to
        /// compare them with the stored patterns. Usually pattern files use only lower
        /// case characters, in this case a class for letter 'a', for example, should
        /// be defined as "aA", the first character being the normalization char.
        /// </summary>
        public virtual void AddClass(string chargroup)
        {
            if (chargroup.Length > 0)
            {
                char equivChar = chargroup[0];
                char[] key = new char[2];
                key[1] = (char)0;
                for (int i = 0; i < chargroup.Length; i++)
                {
                    key[0] = chargroup[i];
                    m_classmap.Insert(key, 0, equivChar);
                }
            }
        }

        /// <summary>
        /// Add an exception to the tree. It is used by
        /// <see cref="PatternParser"/> class as callback to store the
        /// hyphenation exceptions.
        /// </summary>
        /// <param name="word"> normalized word </param>
        /// <param name="hyphenatedword"> a vector of alternating strings and
        ///        <see cref="Hyphen"/> objects. </param>
        public virtual void AddException(string word, IList<object> hyphenatedword)
        {
            m_stoplist[word] = hyphenatedword;
        }

        /// <summary>
        /// Add a pattern to the tree. Mainly, to be used by
        /// <see cref="PatternParser"/> class as callback to add a pattern to
        /// the tree.
        /// </summary>
        /// <param name="pattern"> the hyphenation pattern </param>
        /// <param name="ivalue"> interletter weight values indicating the desirability and
        ///        priority of hyphenating at a given point within the pattern. It
        ///        should contain only digit characters. (i.e. '0' to '9'). </param>
        public virtual void AddPattern(string pattern, string ivalue)
        {
            int k = ivalues.Find(ivalue);
            if (k <= 0)
            {
                k = PackValues(ivalue);
                ivalues.Insert(ivalue, (char)k);
            }
            Insert(pattern, (char)k);
        }

        // public override void printStats(PrintStream @out)
        // {
        //@out.println("Value space size = " + Convert.ToString(vspace.length(), CultureInfo.InvariantCulture));
        //base.printStats(@out);

        // }
    }
}