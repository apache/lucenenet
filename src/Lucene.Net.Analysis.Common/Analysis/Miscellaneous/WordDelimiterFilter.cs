// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Text;

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
    /// Configuration options for the <see cref="WordDelimiterFilter"/>.
    /// <para/>
    /// LUCENENET specific - these options were passed as int constant flags in Lucene.
    /// </summary>
    [System.Flags]
    public enum WordDelimiterFlags
    {
        /// <summary>
        /// Causes parts of words to be generated:
        /// <para/>
        /// "PowerShot" => "Power" "Shot"
        /// </summary>
        GENERATE_WORD_PARTS = 1,

        /// <summary>
        /// Causes number subwords to be generated:
        /// <para/>
        /// "500-42" => "500" "42"
        /// </summary>
        GENERATE_NUMBER_PARTS = 2,

        /// <summary>
        /// Causes maximum runs of word parts to be catenated:
        /// <para/>
        /// "wi-fi" => "wifi"
        /// </summary>
        CATENATE_WORDS = 4,

        /// <summary>
        /// Causes maximum runs of word parts to be catenated:
        /// <para/>
        /// "wi-fi" => "wifi"
        /// </summary>
        CATENATE_NUMBERS = 8,

        /// <summary>
        /// Causes all subword parts to be catenated:
        /// <para/>
        /// "wi-fi-4000" => "wifi4000"
        /// </summary>
        CATENATE_ALL = 16,

        /// <summary>
        /// Causes original words are preserved and added to the subword list (Defaults to false)
        /// <para/>
        /// "500-42" => "500" "42" "500-42"
        /// </summary>
        PRESERVE_ORIGINAL = 32,

        /// <summary>
        /// If not set, causes case changes to be ignored (subwords will only be generated
        /// given SUBWORD_DELIM tokens)
        /// </summary>
        SPLIT_ON_CASE_CHANGE = 64,

        /// <summary>
        /// If not set, causes numeric changes to be ignored (subwords will only be generated
        /// given SUBWORD_DELIM tokens).
        /// </summary>
        SPLIT_ON_NUMERICS = 128,

        /// <summary>
        /// Causes trailing "'s" to be removed for each subword
        /// <para/>
        /// "O'Neil's" => "O", "Neil"
        /// </summary>
        STEM_ENGLISH_POSSESSIVE = 256
    }

    /// <summary>
    /// Splits words into subwords and performs optional transformations on subword
    /// groups. Words are split into subwords with the following rules:
    /// <list type="bullet">
    ///     <item><description>split on intra-word delimiters (by default, all non alpha-numeric
    ///         characters): <c>"Wi-Fi"</c> &#8594; <c>"Wi", "Fi"</c></description></item>
    ///     <item><description>split on case transitions: <c>"PowerShot"</c> &#8594;
    ///         <c>"Power", "Shot"</c></description></item>
    ///     <item><description>split on letter-number transitions: <c>"SD500"</c> &#8594;
    ///         <c>"SD", "500"</c></description></item>
    ///     <item><description>leading and trailing intra-word delimiters on each subword are ignored:
    ///         <c>"//hello---there, 'dude'"</c> &#8594;
    ///         <c>"hello", "there", "dude"</c></description></item>
    ///     <item><description>trailing "'s" are removed for each subword: <c>"O'Neil's"</c>
    ///         &#8594; <c>"O", "Neil"</c>
    ///         <ul>
    ///             <item><description>Note: this step isn't performed in a separate filter because of possible
    ///                 subword combinations.</description></item>
    ///         </ul>
    ///     </description></item>
    /// </list>
    /// <para/>
    /// The <b>combinations</b> parameter affects how subwords are combined:
    /// <list type="bullet">
    ///     <item><description>combinations="0" causes no subword combinations: <code>"PowerShot"</code>
    ///         &#8594; <c>0:"Power", 1:"Shot"</c> (0 and 1 are the token positions)</description></item>
    ///     <item><description>combinations="1" means that in addition to the subwords, maximum runs of
    ///         non-numeric subwords are catenated and produced at the same position of the
    ///         last subword in the run:
    ///         <ul>
    ///             <item><description><c>"PowerShot"</c> &#8594;
    ///                 <c>0:"Power", 1:"Shot" 1:"PowerShot"</c></description></item>
    ///             <item><description><c>"A's+B's&amp;C's"</c> -gt; <c>0:"A", 1:"B", 2:"C", 2:"ABC"</c>
    ///             </description></item>
    ///             <item><description><c>"Super-Duper-XL500-42-AutoCoder!"</c> &#8594;
    ///                 <c>0:"Super", 1:"Duper", 2:"XL", 2:"SuperDuperXL", 3:"500" 4:"42", 5:"Auto", 6:"Coder", 6:"AutoCoder"</c>
    ///             </description></item>
    ///         </ul>
    ///     </description></item>
    /// </list>
    /// <para/>
    /// One use for <see cref="WordDelimiterFilter"/> is to help match words with different
    /// subword delimiters. For example, if the source text contained "wi-fi" one may
    /// want "wifi" "WiFi" "wi-fi" "wi+fi" queries to all match. One way of doing so
    /// is to specify combinations="1" in the analyzer used for indexing, and
    /// combinations="0" (the default) in the analyzer used for querying. Given that
    /// the current <see cref="Standard.StandardTokenizer"/> immediately removes many intra-word
    /// delimiters, it is recommended that this filter be used after a tokenizer that
    /// does not do this (such as <see cref="Core.WhitespaceTokenizer"/>).
    /// </summary>
    public sealed class WordDelimiterFilter : TokenFilter
    {
        // LUCENENET: Added as a replacement for null in Java
        internal const int NOT_SET = 0x00;
        public const int LOWER = 0x01;
        public const int UPPER = 0x02;
        public const int DIGIT = 0x04;
        public const int SUBWORD_DELIM = 0x08;

        // combinations: for testing, not for setting bits
        public const int ALPHA = 0x03;
        public const int ALPHANUM = 0x07;


        // LUCENENET specific - made flags into their own [Flags] enum named WordDelimiterFlags and de-nested from this type


        /// <summary>
        /// If not null is the set of tokens to protect from being delimited
        /// 
        /// </summary>
        private readonly CharArraySet protWords;

        private readonly WordDelimiterFlags flags;

        private readonly ICharTermAttribute termAttribute;
        private readonly IOffsetAttribute offsetAttribute;
        private readonly IPositionIncrementAttribute posIncAttribute;
        private readonly ITypeAttribute typeAttribute;

        // used for iterating word delimiter breaks
        private readonly WordDelimiterIterator iterator;

        // used for concatenating runs of similar typed subwords (word,number)
        private readonly WordDelimiterConcatenation concat;
        // number of subwords last output by concat.
        private int lastConcatCount = 0;

        // used for catenate all
        private readonly WordDelimiterConcatenation concatAll;

        // used for accumulating position increment gaps
        private int accumPosInc = 0;

        private char[] savedBuffer = new char[1024];
        private int savedStartOffset;
        private int savedEndOffset;
        private string savedType;
        private bool hasSavedState = false;
        // if length by start + end offsets doesn't match the term text then assume
        // this is a synonym and don't adjust the offsets.
        private bool hasIllegalOffsets = false;

        // for a run of the same subword type within a word, have we output anything?
        private bool hasOutputToken = false;
        // when preserve original is on, have we output any token following it?
        // this token must have posInc=0!
        private bool hasOutputFollowingOriginal = false;

        /// <summary>
        /// Creates a new WordDelimiterFilter
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="in"> TokenStream to be filtered </param>
        /// <param name="charTypeTable"> table containing character types </param>
        /// <param name="configurationFlags"> Flags configuring the filter </param>
        /// <param name="protWords"> If not null is the set of tokens to protect from being delimited </param>
        public WordDelimiterFilter(LuceneVersion matchVersion, TokenStream @in, byte[] charTypeTable, WordDelimiterFlags configurationFlags, CharArraySet protWords)
            : base(@in)
        {
            this.termAttribute = AddAttribute<ICharTermAttribute>();
            this.offsetAttribute = AddAttribute<IOffsetAttribute>();
            this.posIncAttribute = AddAttribute<IPositionIncrementAttribute>();
            this.typeAttribute = AddAttribute<ITypeAttribute>();
            concat = new WordDelimiterConcatenation(this);
            concatAll = new WordDelimiterConcatenation(this);
            sorter = new OffsetSorter(this);

            if (!matchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                throw new ArgumentException("This class only works with Lucene 4.8+. To emulate the old (broken) behavior of WordDelimiterFilter, use Lucene47WordDelimiterFilter");
            }
            this.flags = configurationFlags;
            this.protWords = protWords;
            this.iterator = new WordDelimiterIterator(charTypeTable, 
                Has(WordDelimiterFlags.SPLIT_ON_CASE_CHANGE), 
                Has(WordDelimiterFlags.SPLIT_ON_NUMERICS), 
                Has(WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE));
        }

        /// <summary>
        /// Creates a new WordDelimiterFilter using <see cref="WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE"/>
        /// as its charTypeTable
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="in"> <see cref="TokenStream"/> to be filtered </param>
        /// <param name="configurationFlags"> Flags configuring the filter </param>
        /// <param name="protWords"> If not null is the set of tokens to protect from being delimited </param>
        public WordDelimiterFilter(LuceneVersion matchVersion, TokenStream @in, WordDelimiterFlags configurationFlags, CharArraySet protWords)
            : this(matchVersion, @in, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, configurationFlags, protWords)
        {
        }

        public override bool IncrementToken()
        {
            while (true)
            {
                if (!hasSavedState)
                {
                    // process a new input word
                    if (!m_input.IncrementToken())
                    {
                        return false;
                    }

                    int termLength = termAttribute.Length;
                    char[] termBuffer = termAttribute.Buffer;

                    accumPosInc += posIncAttribute.PositionIncrement;

                    iterator.SetText(termBuffer, termLength);
                    iterator.Next();

                    // word of no delimiters, or protected word: just return it
                    if ((iterator.current == 0 && iterator.end == termLength) || (protWords != null && protWords.Contains(termBuffer, 0, termLength)))
                    {
                        posIncAttribute.PositionIncrement = accumPosInc;
                        accumPosInc = 0;
                        first = false;
                        return true;
                    }

                    // word of simply delimiters
                    if (iterator.end == WordDelimiterIterator.DONE && !Has(WordDelimiterFlags.PRESERVE_ORIGINAL))
                    {
                        // if the posInc is 1, simply ignore it in the accumulation
                        // TODO: proper hole adjustment (FilteringTokenFilter-like) instead of this previous logic!
                        if (posIncAttribute.PositionIncrement == 1 && !first)
                        {
                            accumPosInc--;
                        }
                        continue;
                    }

                    SaveState();

                    hasOutputToken = false;
                    hasOutputFollowingOriginal = !Has(WordDelimiterFlags.PRESERVE_ORIGINAL);
                    lastConcatCount = 0;

                    if (Has(WordDelimiterFlags.PRESERVE_ORIGINAL))
                    {
                        posIncAttribute.PositionIncrement = accumPosInc;
                        accumPosInc = 0;
                        first = false;
                        return true;
                    }
                }

                // at the end of the string, output any concatenations
                if (iterator.end == WordDelimiterIterator.DONE)
                {
                    if (!concat.IsEmpty)
                    {
                        if (FlushConcatenation(concat))
                        {
                            Buffer();
                            continue;
                        }
                    }

                    if (!concatAll.IsEmpty)
                    {
                        // only if we haven't output this same combo above!
                        if (concatAll.subwordCount > lastConcatCount)
                        {
                            concatAll.WriteAndClear();
                            Buffer();
                            continue;
                        }
                        concatAll.Clear();
                    }

                    if (bufferedPos < bufferedLen)
                    {
                        if (bufferedPos == 0)
                        {
                            sorter.Sort(0, bufferedLen);
                        }
                        ClearAttributes();
                        RestoreState(buffered[bufferedPos++]);
                        if (first && posIncAttribute.PositionIncrement == 0)
                        {
                            // can easily happen with strange combinations (e.g. not outputting numbers, but concat-all)
                            posIncAttribute.PositionIncrement = 1;
                        }
                        first = false;
                        return true;
                    }

                    // no saved concatenations, on to the next input word
                    bufferedPos = bufferedLen = 0;
                    hasSavedState = false;
                    continue;
                }

                // word surrounded by delimiters: always output
                if (iterator.IsSingleWord())
                {
                    GeneratePart(true);
                    iterator.Next();
                    first = false;
                    return true;
                }

                int wordType = iterator.Type;

                // do we already have queued up incompatible concatenations?
                if (!concat.IsEmpty && (concat.type & wordType) == 0)
                {
                    if (FlushConcatenation(concat))
                    {
                        hasOutputToken = false;
                        Buffer();
                        continue;
                    }
                    hasOutputToken = false;
                }

                // add subwords depending upon options
                if (ShouldConcatenate(wordType))
                {
                    if (concat.IsEmpty)
                    {
                        concat.type = wordType;
                    }
                    Concatenate(concat);
                }

                // add all subwords (catenateAll)
                if (Has(WordDelimiterFlags.CATENATE_ALL))
                {
                    Concatenate(concatAll);
                }

                // if we should output the word or number part
                if (ShouldGenerateParts(wordType))
                {
                    GeneratePart(false);
                    Buffer();
                }

                iterator.Next();
            }
        }

        public override void Reset()
        {
            base.Reset();
            hasSavedState = false;
            concat.Clear();
            concatAll.Clear();
            accumPosInc = bufferedPos = bufferedLen = 0;
            first = true;
        }

        // ================================================= Helper Methods ================================================


        private AttributeSource.State[] buffered = new AttributeSource.State[8];
        private int[] startOff = new int[8];
        private int[] posInc = new int[8];
        private int bufferedLen = 0;
        private int bufferedPos = 0;
        private bool first;

        internal class OffsetSorter : InPlaceMergeSorter // LUCENENET NOTE: Changed from private to internal because exposed by internal member
        {
            private readonly WordDelimiterFilter outerInstance;

            public OffsetSorter(WordDelimiterFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override int Compare(int i, int j)
            {
                int cmp = outerInstance.startOff[i].CompareTo(outerInstance.startOff[j]);
                if (cmp == 0)
                {
                    cmp = outerInstance.posInc[j].CompareTo(outerInstance.posInc[i]);
                }
                return cmp;
            }

            protected override void Swap(int i, int j)
            {
                AttributeSource.State tmp = outerInstance.buffered[i];
                outerInstance.buffered[i] = outerInstance.buffered[j];
                outerInstance.buffered[j] = tmp;

                int tmp2 = outerInstance.startOff[i];
                outerInstance.startOff[i] = outerInstance.startOff[j];
                outerInstance.startOff[j] = tmp2;

                tmp2 = outerInstance.posInc[i];
                outerInstance.posInc[i] = outerInstance.posInc[j];
                outerInstance.posInc[j] = tmp2;
            }
        }

        private readonly OffsetSorter sorter;

        private void Buffer()
        {
            if (bufferedLen == buffered.Length)
            {
                int newSize = ArrayUtil.Oversize(bufferedLen + 1, 8);
                buffered = Arrays.CopyOf(buffered, newSize);
                startOff = Arrays.CopyOf(startOff, newSize);
                posInc = Arrays.CopyOf(posInc, newSize);
            }
            startOff[bufferedLen] = offsetAttribute.StartOffset;
            posInc[bufferedLen] = posIncAttribute.PositionIncrement;
            buffered[bufferedLen] = CaptureState();
            bufferedLen++;
        }

        /// <summary>
        /// Saves the existing attribute states
        /// </summary>
        private void SaveState()
        {
            // otherwise, we have delimiters, save state
            savedStartOffset = offsetAttribute.StartOffset;
            savedEndOffset = offsetAttribute.EndOffset;
            // if length by start + end offsets doesn't match the term text then assume this is a synonym and don't adjust the offsets.
            hasIllegalOffsets = (savedEndOffset - savedStartOffset != termAttribute.Length);
            savedType = typeAttribute.Type;

            if (savedBuffer.Length < termAttribute.Length)
            {
                savedBuffer = new char[ArrayUtil.Oversize(termAttribute.Length, RamUsageEstimator.NUM_BYTES_CHAR)];
            }

            Arrays.Copy(termAttribute.Buffer, 0, savedBuffer, 0, termAttribute.Length);
            iterator.text = savedBuffer;

            hasSavedState = true;
        }

        /// <summary>
        /// Flushes the given <see cref="WordDelimiterConcatenation"/> by either writing its concat and then clearing, or just clearing.
        /// </summary>
        /// <param name="concatenation"> <see cref="WordDelimiterConcatenation"/> that will be flushed </param>
        /// <returns> <c>true</c> if the concatenation was written before it was cleared, <c>false</c> otherwise </returns>
        private bool FlushConcatenation(WordDelimiterConcatenation concatenation)
        {
            lastConcatCount = concatenation.subwordCount;
            if (concatenation.subwordCount != 1 || !ShouldGenerateParts(concatenation.type))
            {
                concatenation.WriteAndClear();
                return true;
            }
            concatenation.Clear();
            return false;
        }

        /// <summary>
        /// Determines whether to concatenate a word or number if the current word is the given type
        /// </summary>
        /// <param name="wordType"> Type of the current word used to determine if it should be concatenated </param>
        /// <returns> <c>true</c> if concatenation should occur, <c>false</c> otherwise </returns>
        private bool ShouldConcatenate(int wordType)
        {
            return (Has(WordDelimiterFlags.CATENATE_WORDS) && IsAlpha(wordType)) || (Has(WordDelimiterFlags.CATENATE_NUMBERS) && IsDigit(wordType));
        }

        /// <summary>
        /// Determines whether a word/number part should be generated for a word of the given type
        /// </summary>
        /// <param name="wordType"> Type of the word used to determine if a word/number part should be generated </param>
        /// <returns> <c>true</c> if a word/number part should be generated, <c>false</c> otherwise </returns>
        private bool ShouldGenerateParts(int wordType)
        {
            return (Has(WordDelimiterFlags.GENERATE_WORD_PARTS) && IsAlpha(wordType)) || 
                (Has(WordDelimiterFlags.GENERATE_NUMBER_PARTS) && IsDigit(wordType));
        }

        /// <summary>
        /// Concatenates the saved buffer to the given <see cref="WordDelimiterConcatenation"/>
        /// </summary>
        /// <param name="concatenation"> <see cref="WordDelimiterConcatenation"/> to concatenate the buffer to </param>
        private void Concatenate(WordDelimiterConcatenation concatenation)
        {
            if (concatenation.IsEmpty)
            {
                concatenation.startOffset = savedStartOffset + iterator.current;
            }
            concatenation.Append(savedBuffer, iterator.current, iterator.end - iterator.current);
            concatenation.endOffset = savedStartOffset + iterator.end;
        }

        /// <summary>
        /// Generates a word/number part, updating the appropriate attributes
        /// </summary>
        /// <param name="isSingleWord"> <c>true</c> if the generation is occurring from a single word, <c>false</c> otherwise </param>
        private void GeneratePart(bool isSingleWord)
        {
            ClearAttributes();
            termAttribute.CopyBuffer(savedBuffer, iterator.current, iterator.end - iterator.current);

            int startOffset = savedStartOffset + iterator.current;
            int endOffset = savedStartOffset + iterator.end;

            if (hasIllegalOffsets)
            {
                // historically this filter did this regardless for 'isSingleWord', 
                // but we must do a sanity check:
                if (isSingleWord && startOffset <= savedEndOffset)
                {
                    offsetAttribute.SetOffset(startOffset, savedEndOffset);
                }
                else
                {
                    offsetAttribute.SetOffset(savedStartOffset, savedEndOffset);
                }
            }
            else
            {
                offsetAttribute.SetOffset(startOffset, endOffset);
            }
            posIncAttribute.PositionIncrement = Position(false);
            typeAttribute.Type = savedType;
        }

        /// <summary>
        /// Get the position increment gap for a subword or concatenation
        /// </summary>
        /// <param name="inject"> true if this token wants to be injected </param>
        /// <returns> position increment gap </returns>
        private int Position(bool inject)
        {
            int posInc = accumPosInc;

            if (hasOutputToken)
            {
                accumPosInc = 0;
                return inject ? 0 : Math.Max(1, posInc);
            }

            hasOutputToken = true;

            if (!hasOutputFollowingOriginal)
            {
                // the first token following the original is 0 regardless
                hasOutputFollowingOriginal = true;
                return 0;
            }
            // clear the accumulated position increment
            accumPosInc = 0;
            return Math.Max(1, posInc);
        }

        /// <summary>
        /// Checks if the given word type includes <see cref="ALPHA"/>
        /// </summary>
        /// <param name="type"> Word type to check </param>
        /// <returns> <c>true</c> if the type contains <see cref="ALPHA"/>, <c>false</c> otherwise </returns>
        internal static bool IsAlpha(int type)
        {
            return (type & ALPHA) != 0;
        }

        /// <summary>
        /// Checks if the given word type includes <see cref="DIGIT"/>
        /// </summary>
        /// <param name="type"> Word type to check </param>
        /// <returns> <c>true</c> if the type contains <see cref="DIGIT"/>, <c>false</c> otherwise </returns>
        internal static bool IsDigit(int type)
        {
            return (type & DIGIT) != 0;
        }

        /// <summary>
        /// Checks if the given word type includes <see cref="SUBWORD_DELIM"/>
        /// </summary>
        /// <param name="type"> Word type to check </param>
        /// <returns> <c>true</c> if the type contains <see cref="SUBWORD_DELIM"/>, <c>false</c> otherwise </returns>
        internal static bool IsSubwordDelim(int type)
        {
            return (type & SUBWORD_DELIM) != 0;
        }

        /// <summary>
        /// Checks if the given word type includes <see cref="UPPER"/>
        /// </summary>
        /// <param name="type"> Word type to check </param>
        /// <returns> <c>true</c> if the type contains <see cref="UPPER"/>, <c>false</c> otherwise </returns>
        internal static bool IsUpper(int type)
        {
            return (type & UPPER) != 0;
        }

        /// <summary>
        /// Determines whether the given flag is set
        /// </summary>
        /// <param name="flag"> Flag to see if set </param>
        /// <returns> <c>true</c> if flag is set </returns>
        private bool Has(WordDelimiterFlags flag)
        {
            return (flags & flag) != 0;
        }

        // ================================================= Inner Classes =================================================

        /// <summary>
        /// A WDF concatenated 'run'
        /// </summary>
        internal sealed class WordDelimiterConcatenation
        {
            private readonly WordDelimiterFilter outerInstance;

            public WordDelimiterConcatenation(WordDelimiterFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private readonly StringBuilder buffer = new StringBuilder();
            internal int startOffset;
            internal int endOffset;
            internal int type;
            internal int subwordCount;

            /// <summary>
            /// Appends the given text of the given length, to the concetenation at the given offset
            /// </summary>
            /// <param name="text"> Text to append </param>
            /// <param name="offset"> Offset in the concetenation to add the text </param>
            /// <param name="length"> Length of the text to append </param>
            internal void Append(char[] text, int offset, int length)
            {
                buffer.Append(text, offset, length);
                subwordCount++;
            }

            /// <summary>
            /// Writes the concatenation to the attributes
            /// </summary>
            internal void Write()
            {
                outerInstance.ClearAttributes();
                if (outerInstance.termAttribute.Length < buffer.Length)
                {
                    outerInstance.termAttribute.ResizeBuffer(buffer.Length);
                }
                char[] termbuffer = outerInstance.termAttribute.Buffer;

                buffer.CopyTo(0, termbuffer, 0, buffer.Length);
                outerInstance.termAttribute.Length = buffer.Length;

                if (outerInstance.hasIllegalOffsets)
                {
                    outerInstance.offsetAttribute.SetOffset(outerInstance.savedStartOffset, outerInstance.savedEndOffset);
                }
                else
                {
                    outerInstance.offsetAttribute.SetOffset(startOffset, endOffset);
                }
                outerInstance.posIncAttribute.PositionIncrement = outerInstance.Position(true);
                outerInstance.typeAttribute.Type = outerInstance.savedType;
                outerInstance.accumPosInc = 0;
            }

            /// <summary>
            /// Determines if the concatenation is empty
            /// </summary>
            /// <returns> <c>true</c> if the concatenation is empty, <c>false</c> otherwise </returns>
            internal bool IsEmpty => buffer.Length == 0;

            /// <summary>
            /// Clears the concatenation and resets its state
            /// </summary>
            internal void Clear()
            {
                buffer.Length = 0;
                startOffset = endOffset = type = subwordCount = 0;
            }

            /// <summary>
            /// Convenience method for the common scenario of having to write the concetenation and then clearing its state
            /// </summary>
            internal void WriteAndClear()
            {
                Write();
                Clear();
            }
        }
        // questions:
        // negative numbers?  -42 indexed as just 42?
        // dollar sign?  $42
        // percent sign?  33%
        // downsides:  if source text is "powershot" then a query of "PowerShot" won't match!
    }
}