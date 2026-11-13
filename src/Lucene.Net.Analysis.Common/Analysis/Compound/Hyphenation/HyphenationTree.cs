// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using Lucene.Net.Support.Buffers;
using Lucene.Net.Text;
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
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
        protected virtual int PackValues(ReadOnlySpan<char> values)
        {
            // LUCENENET specific - track the maximum length of all packed values for a quick max allocation reference
            MaxValueByteCount = Math.Max(MaxValueByteCount, values.Length);
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

        /// <summary>
        /// Attempts to unpack the values into the provided character span.
        /// </summary>
        /// <param name="k">The offset into the packed values buffer where the encoded interletter values begin.</param>
        /// <param name="destination">The character span to place the output in.</param>
        /// <param name="charsWritten">Upon return, contains the number of characters that were placed in <paramref name="destination"/>.</param>
        /// <returns><c>true</c> if the operation was successful; otherwise <c>false</c>.</returns>
        /// <seealso cref="GetMaxValueCharCount(int)"/>
        // LUCENENET specific - added allocation-free unpack
        protected virtual bool TryUnpackValues(int k, Span<char> destination, out int charsWritten)
        {
            ValueStringBuilder buf = new(destination);
            try
            {
                UnpackValues(ref buf, k);
                bool success = buf.FitsInitialBuffer(out _);
                charsWritten = success ? buf.Length : 0;
                return success;
            }
            finally
            {
                buf.Dispose();
            }
        }

        protected virtual string UnpackValues(int k)
        {
            // LUCENENET: Estimate the max number of chars that will be needed and allocate up front
            int bufferLength = (m_vspace.Length - k - 1) * 2;
            ValueStringBuilder buf = bufferLength * sizeof(char) <= Constants.MaxStackByteLimit
                ? new(stackalloc char[bufferLength])
                : new(bufferLength);
            try
            {
                UnpackValues(ref buf, k);
                return buf.ToString();
            }
            finally
            {
                buf.Dispose();
            }
        }

        private void UnpackValues(ref ValueStringBuilder destination, int k)
        {
            byte v = m_vspace[k++];
            while (v != 0)
            {
                char c = (char)((v >>> 4) - 1 + '0');
                destination.Append(c);
                c = (char)(v & 0x0f);
                if (c == 0)
                {
                    break;
                }
                c = (char)(c - 1 + '0');
                destination.Append(c);
                v = m_vspace[k++];
            }
        }

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="filename"> the filename </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(string filename)
            => LoadPatterns(filename, Encoding.UTF8);

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
            => LoadPatterns(f.FullName, Encoding.UTF8);

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="f"> a <see cref="FileInfo"/> object representing the file </param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(FileInfo f, Encoding encoding)
            => LoadPatterns(f.FullName, encoding);

        /// <summary>
        /// Read hyphenation patterns from an XML file.
        /// </summary>
        /// <param name="source"> <see cref="Stream"/> input source for the file </param>
        /// <exception cref="IOException"> In case the parsing fails </exception>
        public virtual void LoadPatterns(Stream source)
            => LoadPatterns(source, Encoding.UTF8);

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

        /// <summary>
        /// Attempts to find a pattern in the current tree.
        /// </summary>
        /// <param name="pat">The pattern to find.</param>
        /// <param name="destination">Upon successful return, contains the found pattern.</param>
        /// <param name="charsWritten">Upon successful return, contains the number of characters written
        /// to <paramref name="destination"/>.</param>
        /// <returns><c>true</c> if a matching pattern was found and its values were successfully
        /// written to the destination; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">The pattern was found, but the provided <paramref name="destination"/>
        /// wasn't long enough for it to be written.</exception>
        /// <remarks>
        /// Use a <paramref name="destination"/> length that is at least <see cref="MaxValueByteCount"/>
        /// if the size of pattern that is being searched for is unknown.
        /// </remarks>
        /// <seealso cref="MaxValueByteCount"/>
        public virtual bool TryFindPattern(ReadOnlySpan<char> pat, Span<char> destination, out int charsWritten)
        {
            int k = base.Find(pat);
            if (k < 0)
            {
                charsWritten = 0;
                return false;
            }

            if (!TryUnpackValues(k, destination, out charsWritten))
                throw new ArgumentException($"The pattern was found, but the provided destination buffer was not long enough. " +
                    "To ensure any value can be found, use a buffer size of at least MaxValueByteCount.");

            return true;
        }

        public virtual string FindPattern(ReadOnlySpan<char> pat)
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
        protected virtual int HStrCmp(ReadOnlySpan<char> s, int si, ReadOnlySpan<char> t, int ti)
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

#nullable enable

        /// <summary>
        /// The maximum number of bytes (or chars) required to search for a specific value.
        /// </summary>
        public int MaxValueByteCount { get; protected set; }

        /// <summary>
        /// Calculates the maximum number of bytes produced by unpacking a specific packed values offset.
        /// </summary>
        /// <param name="k">The offset into the packed values buffer where the encoded interletter values begin.</param>
        /// <returns>The maximum number of bytes produced by unpacking a specific packed values offset.</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual int GetMaxValueByteCount(int k)
        {
            return Math.Max(0, (m_vspace.Length - k - 1) * 2);
        }

        /// <summary>
        /// Calculates the maximum number of characters produced by unpacking a specific packed values offset.
        /// </summary>
        /// <param name="k">The offset into the packed values buffer where the encoded interletter values begin.</param>
        /// <returns>The maximum number of characters produced by unpacking a specific packed values offset.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual int GetMaxValueCharCount(int k)
        {
            return Math.Max(0, (m_vspace.Length - k - 1) * 2);
        }


        // LUCENENET specific overload to avoid allocating on each call
        protected virtual bool TryGetValues(int k, Span<byte> destination, out int bytesWritten)
        {
            // LUCENENET specific - Lucene uses a StringBuilder as a growable buffer, so it isn't
            // actually needed if we pass in a large enough buffer up front.
            bytesWritten = 0;

            byte v = m_vspace[k++];

            while (v != 0)
            {
                if (bytesWritten >= destination.Length)
                    return false;

                destination[bytesWritten++] = (byte)((v >>> 4) - 1);

                byte low = (byte)(v & 0x0f);
                if (low == 0)
                    break;

                if (bytesWritten >= destination.Length)
                    return false;

                destination[bytesWritten++] = (byte)(low - 1);
                v = m_vspace[k++];
            }

            return true;
        }

        protected virtual byte[] GetValues(int k)
        {
            // LUCENENET specific - Lucene used a StringBuilder as a growable buffer and then converted
            // the result chars to bytes a the end. The StringBuilder was not necessary, we just need
            // a reasonable way to calculate the buffer to allocate. We do that with GetMaxValueByteCount(),
            // which is a slight over-estimate. But, we only allocate once and do not immediately release the
            // allocated memory to the GC.

            // LUCENENET: Estimate the maximum number of bytes needed and allocate up front.
            int bufferLength = GetMaxValueByteCount(k);
            byte[]? arrayToReturnToPool = null;
            Span<byte> values = bufferLength <= Constants.MaxStackByteLimit
                ? stackalloc byte[bufferLength]
                : (arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bufferLength)).AsSpan(0, bufferLength);
            try
            {
                if (!TryGetValues(k, values, out int bytesWritten))
                    throw new InvalidOperationException("The values buffer is too small.");

                return values.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
            }
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
        protected virtual void SearchPatterns(ReadOnlySpan<char> word, int index, Span<byte> il)
        {
            int i = index;
            char p, q;
            char sp = word[i];
            p = m_root;
            byte[]? arrayToReturnToPool = null;
            int bufferLength = MaxValueByteCount;
            Span<byte> values = bufferLength <= Constants.MaxStackByteLimit
                ? stackalloc byte[bufferLength]
                : (arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bufferLength)).AsSpan(0, bufferLength);
            try
            {
                while (p > 0 && p < m_sc.Length)
                {
                    if (m_sc[p] == 0xFFFF)
                    {
                        if (HStrCmp(word, i, m_kv.Array, m_lo[p]) == 0)
                        {
                            // LUCENENET: Refactored GetValues into TryGetValues to eliminate the allocation per loop
                            if (!TryGetValues(m_eq[p], values, out int valuesLength)) // data pointer is in eq[]
                                throw new InvalidOperationException("The values buffer is too small."); // LUCENENET: This should never happen, but may if a subclass calculates the buffer length wrong.

                            int j = index;
                            for (int k = 0; k < valuesLength; k++)
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
                                // LUCENENET: Refactored GetValues into TryGetValues to eliminate the allocation per loop
                                if (!TryGetValues(m_eq[q], values, out int valuesLength))
                                    throw new InvalidOperationException("The values buffer is too small."); // LUCENENET: This should never happen, but may if a subclass calculates the buffer length wrong.

                                int j = index;
                                for (int k = 0; k < valuesLength; k++)
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
            finally
            {
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
            }
        }

        /// <summary>
        /// Hyphenate word and return an array of hyphenation points.
        /// </summary>
        /// <remarks>
        /// w = "****nnllllllnnn*****".AsSpan(offset, len), where n is a non-letter,
        /// l is a letter, all n may be absent, the first n is at offset, the first l is
        /// at offset + iIgnoreAtBeginning; word = ".llllll.'\0'***", where all l in w are
        /// copied into word. In the first part of the routine len = w.length, in the second
        /// part of the routine len = word.length. Three indices are used: index(w),
        /// the index in w, index(word), the index in word, letterindex(word), the
        /// index in the letter part of word. The following relations exist: index(w) =
        /// offset + i - 1 index(word) = i - iIgnoreAtBeginning letterindex(word) =
        /// index(word) - 1 (see first loop). It follows that: index(w) - index(word) =
        /// offset - 1 + iIgnoreAtBeginning index(w) = letterindex(word) + offset +
        /// iIgnoreAtBeginning
        /// </remarks>
        /// <param name="w">
        /// <see cref="ReadOnlySpan{T}"/> that contains the word. This should be sliced
        /// to the desired offset and length before passing in if necessary.
        /// </param>
        /// <param name="remainCharCount">
        /// Minimum number of characters allowed before the hyphenation point.
        /// </param>
        /// <param name="pushCharCount">
        /// Minimum number of characters allowed after the hyphenation point.
        /// </param>
        /// <returns>
        /// a <see cref="Hyphenation"/> object representing the hyphenated word or null if word is not hyphenated.
        /// </returns>
        public virtual Hyphenation? Hyphenate(ReadOnlySpan<char> w, int remainCharCount, int pushCharCount)
        {
            int i, len = w.Length;
            char[]? rentedWord = null;
            int[]? rentedResult = null;
            byte[]? rentedIl = null;

            // LUCENENET: optimized method for Span and stackalloc
            int wordBufferLength = len + 3;
            Span<char> word = wordBufferLength * sizeof(char) <= Constants.MaxStackByteLimit
                ? stackalloc char[wordBufferLength]
                : (rentedWord = ArrayPool<char>.Shared.Rent(wordBufferLength)).AsSpan(0, wordBufferLength);
            try
            {
                // normalize word
                Span<char> c = stackalloc char[2];
                int iIgnoreAtBeginning = 0;
                int iLength = len;
                bool bEndOfLetters = false;
                for (i = 1; i <= len; i++)
                {
                    c[0] = w[i - 1];
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
                int resultBufferLength = len + 1;
                Span<int> result = resultBufferLength * sizeof(int) <= Constants.MaxStackByteLimit
                    ? stackalloc int[resultBufferLength]
                    : (rentedResult = ArrayPool<int>.Shared.Rent(resultBufferLength)).AsSpan(0, resultBufferLength);
                int k = 0;

                // check exception list first
                ReadOnlySpan<char> sw = word.Slice(1, len);
                //string sw = word.Slice(1, len).ToString();
                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                //if (m_stoplist.TryGetValue(sw, out IList<object>? hw))
                if (TryGetStopListValue(sw, out IList<object>? hw))
                {
                    // assume only simple hyphens (Hyphen.pre="-", Hyphen.post = Hyphen.no =
                    // null)
                    int j = 0;
                    for (i = 0; i < hw!.Count; i++)
                    {
                        object o = hw[i];
                        // j = index(sw) = letterindex(word)?
                        // result[k] = corresponding index(w)
                        if (o is string os)
                        {
                            j += os.Length;
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
                    int ilBufferLength = len + 3;
                    Span<byte> il = ilBufferLength <= Constants.MaxStackByteLimit
                        ? stackalloc byte[ilBufferLength] // initialized to zero
                        : (rentedIl = ArrayPool<byte>.Shared.Rent(ilBufferLength)).AsSpan(0, ilBufferLength);
                    if (rentedIl is not null)
                    {
                        // initialize to zero
                        il.Clear();
                    }

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
                    result.Slice(0, k).CopyTo(res.AsSpan(1, k));
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
            finally
            {
                ArrayPool<char>.Shared.ReturnIfNotNull(rentedWord);
                ArrayPool<int>.Shared.ReturnIfNotNull(rentedResult);
                ArrayPool<byte>.Shared.ReturnIfNotNull(rentedIl);
            }
        }

#nullable restore

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
        public virtual void AddClass(ReadOnlySpan<char> chargroup)
        {
            if (chargroup.Length > 0)
            {
                char equivChar = chargroup[0];
                Span<char> key = stackalloc char[2];
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
        public virtual void AddException(ReadOnlySpan<char> word, IList<object> hyphenatedword)
        {
            // LUCENENET specific - favor alternate lookup, when available
            if (m_stoplist is JCG.Dictionary<string, IList<object>> jcgStopList &&
                jcgStopList.TryGetSpanAlternateLookup<char>(out var jcgLookup))
            {
                jcgLookup[word] = hyphenatedword;
                return;
            }
#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            else if (m_stoplist is Dictionary<string, IList<object>> scgStopList &&
                scgStopList.TryGetAlternateLookup<ReadOnlySpan<char>>(out var scgLookup))
            {
                scgLookup[word] = hyphenatedword;
                return;
            }
#endif

            m_stoplist[word.ToString()] = hyphenatedword;
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
        public virtual void AddPattern(ReadOnlySpan<char> pattern, ReadOnlySpan<char> ivalue)
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

#nullable enable

        // LUCENENET specific - favor alternate lookup, when available
        private bool TryGetStopListValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out IList<object>? value)
        {
            if (m_stoplist is JCG.Dictionary<string, IList<object>> jcgStopList &&
                jcgStopList.TryGetSpanAlternateLookup<char>(out var jcgLookup))
            {
                return jcgLookup.TryGetValue(key, out value);
            }
#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            else if (m_stoplist is Dictionary<string, IList<object>> scgStopList &&
                scgStopList.TryGetAlternateLookup<ReadOnlySpan<char>>(out var scgLookup))
            {
                return scgLookup.TryGetValue(key, out value);
            }
#endif

            return m_stoplist.TryGetValue(key.ToString(), out value);
        }
    }
}
