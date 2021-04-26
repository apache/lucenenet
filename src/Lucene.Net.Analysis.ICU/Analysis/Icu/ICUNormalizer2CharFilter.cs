// Lucene version compatibility level < 7.1.0
using J2N;
using ICU4N.Text;
using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ExceptionToClassNameConventionAttribute = Lucene.Net.Support.ExceptionToClassNameConventionAttribute;
using Lucene.Net.Diagnostics;

namespace Lucene.Net.Analysis.Icu
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
    /// Normalize token text with ICU's <see cref="Normalizer2"/>.
    /// </summary>
    [ExceptionToClassNameConvention]
    public sealed class ICUNormalizer2CharFilter : BaseCharFilter
    {
        private readonly Normalizer2 normalizer;
        private readonly StringBuilder inputBuffer = new StringBuilder();
        private readonly StringBuilder resultBuffer = new StringBuilder();

        private bool inputFinished;
        private bool afterQuickCheckYes;
        private int checkedInputBoundary;
        private int charCount;

        /// <summary>
        /// Create a new <see cref="ICUNormalizer2CharFilter"/> that combines NFKC normalization, Case
        /// Folding, and removes Default Ignorables (NFKC_Casefold).
        /// </summary>
        /// <param name="input"></param>
        public ICUNormalizer2CharFilter(TextReader input)
            : this(input, Normalizer2.GetInstance(null, "nfkc_cf", Normalizer2Mode.Compose))
        {
        }

        /// <summary>
        /// Create a new <see cref="ICUNormalizer2CharFilter"/> with the specified <see cref="Normalizer2"/>.
        /// </summary>
        /// <param name="input">Input text.</param>
        /// <param name="normalizer">Normalizer to use.</param>
        public ICUNormalizer2CharFilter(TextReader input, Normalizer2 normalizer)
            : this(input, normalizer, 128)
        {
            this.normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        // for testing ONLY
        internal ICUNormalizer2CharFilter(TextReader input, Normalizer2 normalizer, int bufferSize)
            : base(input)
        {
            this.normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.tmpBuffer = CharacterUtils.NewCharacterBuffer(bufferSize);
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            if (off < 0) throw new ArgumentOutOfRangeException(nameof(off), "off < 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            if (off >= cbuf.Length) throw new ArgumentOutOfRangeException(nameof(off), "off >= cbuf.length"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            if (len <= 0) throw new ArgumentOutOfRangeException(nameof(len), "len <= 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

            while (!inputFinished || inputBuffer.Length > 0 || resultBuffer.Length > 0)
            {
                int retLen;

                if (resultBuffer.Length > 0)
                {
                    retLen = OutputFromResultBuffer(cbuf, off, len);
                    if (retLen > 0)
                    {
                        return retLen;
                    }
                }

                int resLen = ReadAndNormalizeFromInput();
                if (resLen > 0)
                {
                    retLen = OutputFromResultBuffer(cbuf, off, len);
                    if (retLen > 0)
                    {
                        return retLen;
                    }
                }

                ReadInputToBuffer();
            }

            return 0; // .NET semantics - return 0, not -1
        }

        private readonly CharacterUtils.CharacterBuffer tmpBuffer;

        private void ReadInputToBuffer()
        {
            while (true)
            {
                // CharacterUtils.fill is supplementary char aware
#pragma warning disable 612, 618
                bool hasRemainingChars = CharacterUtils.GetInstance(LuceneVersion.LUCENE_CURRENT).Fill(tmpBuffer, m_input);
#pragma warning restore 612, 618

                if (Debugging.AssertsEnabled) Debugging.Assert(tmpBuffer.Offset == 0);
                inputBuffer.Append(tmpBuffer.Buffer, 0, tmpBuffer.Length);

                if (hasRemainingChars == false)
                {
                    inputFinished = true;
                    break;
                }

                int lastCodePoint = Character.CodePointBefore(tmpBuffer.Buffer, tmpBuffer.Length , 0);
                if (normalizer.IsInert(lastCodePoint))
                {
                    // we require an inert char so that we can normalize content before and
                    // after this character independently
                    break;
                }
            }
        }

        private int ReadAndNormalizeFromInput()
        {
            if (inputBuffer.Length <= 0)
            {
                afterQuickCheckYes = false;
                return 0;
            }
            if (!afterQuickCheckYes)
            {
                int resLen2 = ReadFromInputWhileSpanQuickCheckYes();
                afterQuickCheckYes = true;
                if (resLen2 > 0) return resLen2;
            }
            int resLen = ReadFromIoNormalizeUptoBoundary();
            if (resLen > 0)
            {
                afterQuickCheckYes = false;
            }
            return resLen;
        }

        private int ReadFromInputWhileSpanQuickCheckYes()
        {
            int end = normalizer.SpanQuickCheckYes(inputBuffer);
            if (end > 0)
            {
                resultBuffer.Append(inputBuffer.ToString(0, end));
                inputBuffer.Remove(0, end);
                checkedInputBoundary = Math.Max(checkedInputBoundary - end, 0);
                charCount += end;
            }
            return end;
        }

        private int ReadFromIoNormalizeUptoBoundary()
        {
            // if there's no buffer to normalize, return 0
            if (inputBuffer.Length <= 0)
            {
                return 0;
            }

            bool foundBoundary = false;
            int bufLen = inputBuffer.Length;

            while (checkedInputBoundary <= bufLen - 1)
            {
                int charLen = Character.CharCount(inputBuffer.CodePointAt(checkedInputBoundary));
                checkedInputBoundary += charLen;
                if (checkedInputBoundary < bufLen && normalizer.HasBoundaryBefore(inputBuffer
                  .CodePointAt(checkedInputBoundary)))
                {
                    foundBoundary = true;
                    break;
                }
            }
            if (!foundBoundary && checkedInputBoundary >= bufLen && inputFinished)
            {
                foundBoundary = true;
                checkedInputBoundary = bufLen;
            }

            if (!foundBoundary)
            {
                return 0;
            }

            return NormalizeInputUpto(checkedInputBoundary);
        }

        private int NormalizeInputUpto(int length)
        {
            int destOrigLen = resultBuffer.Length;
            normalizer.NormalizeSecondAndAppend(resultBuffer, inputBuffer.ToString(0, length));

            inputBuffer.Remove(0, length);
            checkedInputBoundary = Math.Max(checkedInputBoundary - length, 0);
            int resultLength = resultBuffer.Length - destOrigLen;
            RecordOffsetDiff(length, resultLength);
            return resultLength;
        }

        private void RecordOffsetDiff(int inputLength, int outputLength)
        {
            if (inputLength == outputLength)
            {
                charCount += outputLength;
                return;
            }
            int diff = inputLength - outputLength;
            int cumuDiff = LastCumulativeDiff;
            if (diff < 0)
            {
                for (int i = 1; i <= -diff; ++i)
                {
                    AddOffCorrectMap(charCount + i, cumuDiff - i);
                }
            }
            else
            {
                AddOffCorrectMap(charCount + outputLength, cumuDiff + diff);
            }
            charCount += outputLength;
        }

        private int OutputFromResultBuffer(char[] cbuf, int begin, int len)
        {
            len = Math.Min(resultBuffer.Length, len);
            resultBuffer.CopyTo(0, cbuf, begin, len);
            if (len > 0)
            {
                resultBuffer.Remove(0, len);
            }
            return len;
        }
    }
}
