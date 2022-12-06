// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Provides the ability to override any <see cref="IKeywordAttribute"/> aware stemmer
    /// with custom dictionary-based stemming.
    /// </summary>
    public sealed class StemmerOverrideFilter : TokenFilter
    {
        private readonly StemmerOverrideMap stemmerOverrideMap;

        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAtt;
        private readonly FST.BytesReader fstReader;
        private readonly FST.Arc<BytesRef> scratchArc = new FST.Arc<BytesRef>();
        private readonly CharsRef spare = new CharsRef();

        /// <summary>
        /// Create a new <see cref="StemmerOverrideFilter"/>, performing dictionary-based stemming
        /// with the provided dictionary (<paramref name="stemmerOverrideMap"/>).
        /// <para>
        /// Any dictionary-stemmed terms will be marked with <see cref="IKeywordAttribute"/>
        /// so that they will not be stemmed with stemmers down the chain.
        /// </para>
        /// </summary>
        public StemmerOverrideFilter(TokenStream input, StemmerOverrideMap stemmerOverrideMap)
              : base(input)
        {
            this.stemmerOverrideMap = stemmerOverrideMap;
            fstReader = stemmerOverrideMap.GetBytesReader();
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (fstReader is null)
                {
                    // No overrides
                    return true;
                }
                if (!keywordAtt.IsKeyword) // don't muck with already-keyworded terms
                {
                    BytesRef stem = stemmerOverrideMap.Get(termAtt.Buffer, termAtt.Length, scratchArc, fstReader);
                    if (stem != null)
                    {
                        char[] buffer = spare.Chars = termAtt.Buffer;
                        UnicodeUtil.UTF8toUTF16(stem.Bytes, stem.Offset, stem.Length, spare);
                        if (spare.Chars != buffer)
                        {
                            termAtt.CopyBuffer(spare.Chars, spare.Offset, spare.Length);
                        }
                        termAtt.Length = spare.Length;
                        keywordAtt.IsKeyword = true;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// A read-only 4-byte FST backed map that allows fast case-insensitive key
        /// value lookups for <see cref="StemmerOverrideFilter"/>
        /// </summary>
        // TODO maybe we can generalize this and reuse this map somehow?
        public sealed class StemmerOverrideMap
        {
            private readonly FST<BytesRef> fst;
            private readonly bool ignoreCase;

            /// <summary>
            /// Creates a new <see cref="StemmerOverrideMap"/> </summary>
            /// <param name="fst"> the fst to lookup the overrides </param>
            /// <param name="ignoreCase"> if the keys case should be ingored </param>
            public StemmerOverrideMap(FST<BytesRef> fst, bool ignoreCase)
            {
                this.fst = fst;
                this.ignoreCase = ignoreCase;
            }

            /// <summary>
            /// Returns a <see cref="FST.BytesReader"/> to pass to the <see cref="Get(char[], int, FST.Arc{BytesRef}, FST.BytesReader)"/> method.
            /// </summary>
            public FST.BytesReader GetBytesReader()
            {
                if (fst is null)
                {
                    return null;
                }
                else
                {
                    return fst.GetBytesReader();
                }
            }

            /// <summary>
            /// Returns the value mapped to the given key or <code>null</code> if the key is not in the FST dictionary.
            /// </summary>
            public BytesRef Get(char[] buffer, int bufferLen, FST.Arc<BytesRef> scratchArc, FST.BytesReader fstReader)
            {
                BytesRef pendingOutput = fst.Outputs.NoOutput;
                BytesRef matchOutput = null;
                int bufUpto = 0;
                fst.GetFirstArc(scratchArc);
                while (bufUpto < bufferLen)
                {
                    int codePoint = Character.CodePointAt(buffer, bufUpto, bufferLen);
                    if (fst.FindTargetArc(ignoreCase ? Character.ToLower(codePoint, CultureInfo.InvariantCulture) : codePoint, scratchArc, scratchArc, fstReader) is null)
                    {
                        return null;
                    }
                    pendingOutput = fst.Outputs.Add(pendingOutput, scratchArc.Output);
                    bufUpto += Character.CharCount(codePoint);
                }
                if (scratchArc.IsFinal)
                {
                    matchOutput = fst.Outputs.Add(pendingOutput, scratchArc.NextFinalOutput);
                }
                return matchOutput;
            }
        }

        /// <summary>
        /// This builder builds an <see cref="FST"/> for the <see cref="StemmerOverrideFilter"/>
        /// </summary>
        public class Builder
        {
            private readonly BytesRefHash hash = new BytesRefHash();
            private readonly BytesRef spare = new BytesRef();
            private readonly IList<string> outputValues = new JCG.List<string>();
            private readonly bool ignoreCase;
            private readonly CharsRef charsSpare = new CharsRef();

            /// <summary>
            /// Creates a new <see cref="Builder"/> with <see cref="ignoreCase"/> set to <c>false</c> 
            /// </summary>
            public Builder()
                : this(false)
            {
            }

            /// <summary>
            /// Creates a new <see cref="Builder"/> </summary>
            /// <param name="ignoreCase"> if the input case should be ignored. </param>
            public Builder(bool ignoreCase)
            {
                this.ignoreCase = ignoreCase;
            }

            /// <summary>
            /// Adds an input string and it's stemmer override output to this builder.
            /// </summary>
            /// <param name="input"> the input char sequence </param>
            /// <param name="output"> the stemmer override output char sequence </param>
            /// <returns> <c>false</c> if the input has already been added to this builder otherwise <c>true</c>. </returns>
            /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
            // LUCENENET specific overload of ICharSequence
            public virtual bool Add(string input, string output)
            {
                // LUCENENET: Added guard clauses
                if (input is null)
                    throw new ArgumentNullException(nameof(input));
                if (output is null)
                    throw new ArgumentNullException(nameof(output));

                int length = input.Length;
                if (ignoreCase)
                {
                    // convert on the fly to lowercase

                    // LUCENENET: Reduce allocations/improve throughput by using stack and spans
                    var source = input.AsSpan();
                    if (length * sizeof(char) <= Constants.MaxStackByteLimit)
                    {
                        // Fast path - use the stack
                        Span<char> buffer = stackalloc char[length];
                        source.ToLowerInvariant(buffer);

                        UnicodeUtil.UTF16toUTF8(buffer, spare);
                    }
                    else
                    {
                        // Slow path - use the heap
                        charsSpare.Grow(length);
                        char[] buffer = charsSpare.Chars;

                        var destination = buffer.AsSpan(0, length);
                        source.ToLowerInvariant(destination);

                        UnicodeUtil.UTF16toUTF8(buffer, 0, length, spare);
                    }
                }
                else
                {
                    UnicodeUtil.UTF16toUTF8(input, 0, length, spare);
                }
                if (hash.Add(spare) >= 0)
                {
                    outputValues.Add(output);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Adds an input string and it's stemmer override output to this builder.
            /// </summary>
            /// <param name="input"> the input char sequence </param>
            /// <param name="output"> the stemmer override output char sequence </param>
            /// <returns> <c>false</c> if the input has already been added to this builder otherwise <c>true</c>. </returns>
            /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
            // LUCENENET specific overload of ICharSequence
            public virtual bool Add(char[] input, string output)
            {
                // LUCENENET: Added guard clauses
                if (input is null)
                    throw new ArgumentNullException(nameof(input));
                if (output is null)
                    throw new ArgumentNullException(nameof(output));

                int length = input.Length;
                if (ignoreCase)
                {
                    // convert on the fly to lowercase

                    // LUCENENET: Reduce allocations/improve throughput by using stack and spans
                    var source = new ReadOnlySpan<char>(input);
                    if (length * sizeof(char) <= Constants.MaxStackByteLimit)
                    {
                        // Fast path - use the stack
                        Span<char> buffer = stackalloc char[length];
                        source.ToLowerInvariant(buffer);

                        UnicodeUtil.UTF16toUTF8(buffer, spare);
                    }
                    else
                    {
                        // Slow path - use the heap
                        charsSpare.Grow(length);
                        char[] buffer = charsSpare.Chars;

                        var destination = buffer.AsSpan(0, length);
                        source.ToLowerInvariant(destination);

                        UnicodeUtil.UTF16toUTF8(buffer, 0, length, spare);
                    }
                }
                else
                {
                    UnicodeUtil.UTF16toUTF8(input, 0, length, spare);
                }
                if (hash.Add(spare) >= 0)
                {
                    outputValues.Add(output);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Adds an input string and it's stemmer override output to this builder.
            /// </summary>
            /// <param name="input"> the input char sequence </param>
            /// <param name="output"> the stemmer override output char sequence </param>
            /// <returns> <c>false</c> if the input has already been added to this builder otherwise <c>true</c>. </returns>
            /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
            // LUCENENET specific overload of ICharSequence
            public virtual bool Add(ICharSequence input, string output)
            {
                // LUCENENET: Added guard clauses
                if (input is null)
                    throw new ArgumentNullException(nameof(input));
                if (output is null)
                    throw new ArgumentNullException(nameof(output));

                if (input is CharArrayCharSequence charArrayCharSequence && charArrayCharSequence.HasValue)
                    return Add(charArrayCharSequence.Value, output);

                // LUCENENET: In .NET, the indexer for StringBuilder is slow, so we are better off
                // converting to a string in all other cases.
                return Add(input.ToString(), output);
            }

            /// <summary>
            /// Returns a <see cref="StemmerOverrideMap"/> to be used with the <see cref="StemmerOverrideFilter"/> </summary>
            /// <returns> a <see cref="StemmerOverrideMap"/> to be used with the <see cref="StemmerOverrideFilter"/> </returns>
            /// <exception cref="IOException"> if an <see cref="IOException"/> occurs; </exception>
            public virtual StemmerOverrideMap Build()
            {
                ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                Builder<BytesRef> builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE4, outputs);
                int[] sort = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
                Int32sRef intsSpare = new Int32sRef();
                int size = hash.Count;
                for (int i = 0; i < size; i++)
                {
                    int id = sort[i];
                    BytesRef bytesRef = hash.Get(id, spare);
                    UnicodeUtil.UTF8toUTF32(bytesRef, intsSpare);
                    builder.Add(intsSpare, new BytesRef(outputValues[id]));
                }
                return new StemmerOverrideMap(builder.Finish(), ignoreCase);
            }
        }
    }
}