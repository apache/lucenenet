using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

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
	/// 
	/// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
	/// </summary>
	public class HyphenationTree : TernaryTree, IPatternConsumer
    {

        /// <summary>
        /// value space: stores the interletter values
        /// </summary>
        protected internal ByteVector vspace;

        /// <summary>
        /// This map stores hyphenation exceptions
        /// </summary>
        protected internal IDictionary<string, IList<object>> stoplist;

        /// <summary>
        /// This map stores the character classes
        /// </summary>
        protected internal TernaryTree classmap;

        /// <summary>
        /// Temporary map to store interletter values on pattern loading.
        /// </summary>
#if !NETSTANDARD
        [NonSerialized]
#endif
        private TernaryTree ivalues;

        public HyphenationTree()
        {
            stoplist = new HashMap<string, IList<object>>(23); // usually a small table
            classmap = new TernaryTree();
            vspace = new ByteVector();
            vspace.Alloc(1); // this reserves index 0, which we don't use
        }

        /// <summary>
        /// Packs the values by storing them in 4 bits, two values into a byte Values
        /// range is from 0 to 9. We use zero as terminator, so we'll add 1 to the
        /// value.
        /// </summary>
        /// <param name="values"> a string of digits from '0' to '9' representing the
        ///        interletter values. </param>
        /// <returns> the index into the vspace array where the packed values are stored. </returns>
        protected internal virtual int PackValues(string values)
        {
            int i, n = values.Length;
            int m = (n & 1) == 1 ? (n >> 1) + 2 : (n >> 1) + 1;
            int offset = vspace.Alloc(m);
            sbyte[] va = vspace.Array;
            for (i = 0; i < n; i++)
            {
                int j = i >> 1;
                sbyte v = (sbyte)((values[i] - '0' + 1) & 0x0f);
                if ((i & 1) == 1)
                {
                    va[j + offset] = (sbyte)(va[j + offset] | v);
                }
                else
                {
                    va[j + offset] = (sbyte)(v << 4); // big endian
                }
            }
            va[m - 1 + offset] = 0; // terminator
            return offset;
        }

        protected internal virtual string UnpackValues(int k)
        {
            StringBuilder buf = new StringBuilder();
            sbyte v = vspace[k++];
            while (v != 0)
            {
                char c = (char)(((int)((uint)v >> 4)) - 1 + '0');
                buf.Append(c);
                c = (char)(v & 0x0f);
                if (c == 0)
                {
                    break;
                }
                c = (char)(c - 1 + '0');
                buf.Append(c);
                v = vspace[k++];
            }
            return buf.ToString();
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(string filename)
        {
            LoadPatterns(filename, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(string filename, Encoding encoding)
        {
            var src = new FileStream(filename, FileMode.Open, FileAccess.Read);
            LoadPatterns(src, encoding);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(FileInfo f)
        {
            LoadPatterns(f, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(FileInfo f, Encoding encoding)
        {
            var src = new FileStream(f.FullName, FileMode.Open, FileAccess.Read);
            LoadPatterns(src, encoding);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="source"> the InputSource for the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(Stream source)
        {
            LoadPatterns(source, Encoding.UTF8);
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="source"> the InputSource for the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(Stream source, Encoding encoding)
        {
            // LUCENENET TODO: Create overloads that allow XmlReaderSettings to be passed in.
            using (var reader = XmlReader.Create(new StreamReader(source, encoding), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new PatternParser.DtdResolver()
            }))
            {
                LoadPatterns(reader);
            }
        }

        public virtual void LoadPatterns(XmlReader source)
        {
            PatternParser pp = new PatternParser(this);
            ivalues = new TernaryTree();

            pp.Parse(source);

            // patterns/values should be now in the tree
            // let's optimize a bit
            TrimToSize();
            vspace.TrimToSize();
            classmap.TrimToSize();

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
        protected internal virtual int HStrCmp(char[] s, int si, char[] t, int ti)
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

        protected internal virtual sbyte[] GetValues(int k)
        {
            StringBuilder buf = new StringBuilder();
            sbyte v = vspace[k++];
            while (v != 0)
            {
                char c = (char)((((int)((uint)v >> 4))) - 1);
                buf.Append(c);
                c = (char)(v & 0x0f);
                if (c == 0)
                {
                    break;
                }
                c = (char)(c - 1);
                buf.Append(c);
                v = vspace[k++];
            }
            sbyte[] res = new sbyte[buf.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (sbyte)buf[i];
            }
            return res;
        }

        /// <summary>
        /// <para>
        /// Search for all possible partial matches of word starting at index an update
        /// interletter values. In other words, it does something like:
        /// </para>
        /// <code>
        /// for(i=0; i&lt;patterns.length; i++) {
        /// if ( word.substring(index).startsWidth(patterns[i]) )
        /// update_interletter_values(patterns[i]);
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
        protected internal virtual void SearchPatterns(char[] word, int index, sbyte[] il)
        {
            sbyte[] values;
            int i = index;
            char p, q;
            char sp = word[i];
            p = root;

            while (p > 0 && p < sc.Length)
            {
                if (sc[p] == 0xFFFF)
                {
                    if (HStrCmp(word, i, kv.Array, lo[p]) == 0)
                    {
                        values = GetValues(eq[p]); // data pointer is in eq[]
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
                int d = sp - sc[p];
                if (d == 0)
                {
                    if (sp == 0)
                    {
                        break;
                    }
                    sp = word[++i];
                    p = eq[p];
                    q = p;

                    // look for a pattern ending at this position by searching for
                    // the null char ( splitchar == 0 )
                    while (q > 0 && q < sc.Length)
                    {
                        if (sc[q] == 0xFFFF) // stop at compressed branch
                        {
                            break;
                        }
                        if (sc[q] == 0)
                        {
                            values = GetValues(eq[q]);
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
                            q = lo[q];

                            /// <summary>
                            /// actually the code should be: q = sc[q] < 0 ? hi[q] : lo[q]; but
                            /// java chars are unsigned
                            /// </summary>
                        }
                    }
                }
                else
                {
                    p = d < 0 ? lo[p] : hi[p];
                }
            }
        }

        /// <summary>
        /// Hyphenate word and return a Hyphenation object.
        /// </summary>
        /// <param name="word"> the word to be hyphenated </param>
        /// <param name="remainCharCount"> Minimum number of characters allowed before the
        ///        hyphenation point. </param>
        /// <param name="pushCharCount"> Minimum number of characters allowed after the
        ///        hyphenation point. </param>
        /// <returns> a <seealso cref="Hyphenation Hyphenation"/> object representing the
        ///         hyphenated word or null if word is not hyphenated. </returns>
        public virtual Hyphenation Hyphenate(string word, int remainCharCount, int pushCharCount)
        {
            char[] w = word.ToCharArray();
            return Hyphenate(w, 0, w.Length, remainCharCount, pushCharCount);
        }

        /// <summary>
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
        /// </summary>

        /// <summary>
        /// Hyphenate word and return an array of hyphenation points.
        /// </summary>
        /// <param name="w"> char array that contains the word </param>
        /// <param name="offset"> Offset to first character in word </param>
        /// <param name="len"> Length of word </param>
        /// <param name="remainCharCount"> Minimum number of characters allowed before the
        ///        hyphenation point. </param>
        /// <param name="pushCharCount"> Minimum number of characters allowed after the
        ///        hyphenation point. </param>
        /// <returns> a <seealso cref="Hyphenation Hyphenation"/> object representing the
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
                int nc = classmap.Find(c, 0);
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
            if (stoplist.ContainsKey(sw))
            {
                // assume only simple hyphens (Hyphen.pre="-", Hyphen.post = Hyphen.no =
                // null)
                IList<object> hw = stoplist[sw];
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
                sbyte[] il = new sbyte[len + 3]; // initialized to zero
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
                Array.Copy(result, 0, res, 1, k);
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
        /// <seealso cref="PatternParser PatternParser"/> as callback to add character classes.
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
                    classmap.Insert(key, 0, equivChar);
                }
            }
        }

        /// <summary>
        /// Add an exception to the tree. It is used by
        /// <seealso cref="PatternParser PatternParser"/> class as callback to store the
        /// hyphenation exceptions.
        /// </summary>
        /// <param name="word"> normalized word </param>
        /// <param name="hyphenatedword"> a vector of alternating strings and
        ///        <seealso cref="Hyphen hyphen"/> objects. </param>
        public virtual void AddException(string word, List<object> hyphenatedword)
        {
            stoplist[word] = hyphenatedword;
        }

        /// <summary>
        /// Add a pattern to the tree. Mainly, to be used by
        /// <seealso cref="PatternParser PatternParser"/> class as callback to add a pattern to
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
        //@out.println("Value space size = " + Convert.ToString(vspace.length()));
        //base.printStats(@out);

        // }
    }
}