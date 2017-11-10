// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Lucene.Net.Analysis.CharFilters;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// Normalize token text with ICU's <see cref="Normalizer2"/>.
//    /// </summary>
//    public sealed class ICUNormalizer2CharFilter : BaseCharFilter
//    {
//        private static readonly int IO_BUFFER_SIZE = 128;

//        private readonly Normalizer2 normalizer;
//        private readonly StringBuilder inputBuffer = new StringBuilder();
//        private readonly StringBuilder resultBuffer = new StringBuilder();

//        private bool inputFinished;
//        private bool afterQuickCheckYes;
//        private int checkedInputBoundary;
//        private int charCount;


//        /**
//         * Create a new Normalizer2CharFilter that combines NFKC normalization, Case
//         * Folding, and removes Default Ignorables (NFKC_Casefold)
//         */
//        public ICUNormalizer2CharFilter(TextReader input)
//            : this(input, new Normalizer2(Icu.Normalizer.UNormalizationMode.UNORM_NFKC) /*Normalizer2.getInstance(null, "nfkc_cf", Normalizer2.Mode.COMPOSE)*/)
//        {
//        }

//        /**
//         * Create a new Normalizer2CharFilter with the specified Normalizer2
//         * @param in text
//         * @param normalizer normalizer to use
//         */
//        public ICUNormalizer2CharFilter(TextReader input, Normalizer2 normalizer)
//            : base(input)
//        {
//            if (normalizer == null)
//            {
//                throw new ArgumentNullException("normalizer");
//            }
//            this.normalizer = normalizer;
//        }

//        public override int Read(char[] cbuf, int off, int len)
//        {
//            if (off < 0) throw new ArgumentException("off < 0");
//            if (off >= cbuf.Length) throw new ArgumentException("off >= cbuf.length");
//            if (len <= 0) throw new ArgumentException("len <= 0");

//            while (!inputFinished || inputBuffer.Length > 0 || resultBuffer.Length > 0)
//            {
//                int retLen;

//                if (resultBuffer.Length > 0)
//                {
//                    retLen = OutputFromResultBuffer(cbuf, off, len);
//                    if (retLen > 0)
//                    {
//                        return retLen;
//                    }
//                }

//                int resLen = ReadAndNormalizeFromInput();
//                if (resLen > 0)
//                {
//                    retLen = OutputFromResultBuffer(cbuf, off, len);
//                    if (retLen > 0)
//                    {
//                        return retLen;
//                    }
//                }

//                ReadInputToBuffer();
//            }

//            return -1;
//        }

//        private readonly char[] tmpBuffer = new char[IO_BUFFER_SIZE];

//        private int ReadInputToBuffer()
//        {
//            int len = m_input.Read(tmpBuffer, 0, tmpBuffer.Length);
//            if (len == -1)
//            {
//                inputFinished = true;
//                return 0;
//            }
//            inputBuffer.Append(tmpBuffer, 0, len);

//            // if checkedInputBoundary was at the end of a buffer, we need to check that char again
//            checkedInputBoundary = Math.Max(checkedInputBoundary - 1, 0);
//            // this loop depends on 'isInert' (changes under normalization) but looks only at characters.
//            // so we treat all surrogates as non-inert for simplicity
//            if (normalizer.IsInert(tmpBuffer[len - 1]) && !char.IsSurrogate(tmpBuffer[len - 1]))
//            {
//                return len;
//            }
//            else return len + ReadInputToBuffer();
//        }

//        private int ReadAndNormalizeFromInput()
//        {
//            if (inputBuffer.Length <= 0)
//            {
//                afterQuickCheckYes = false;
//                return 0;
//            }
//            if (!afterQuickCheckYes)
//            {
//                int resLen2 = ReadFromInputWhileSpanQuickCheckYes();
//                afterQuickCheckYes = true;
//                if (resLen2 > 0) return resLen2;
//            }
//            int resLen = ReadFromIoNormalizeUptoBoundary();
//            if (resLen > 0)
//            {
//                afterQuickCheckYes = false;
//            }
//            return resLen;
//        }

//        private int ReadFromInputWhileSpanQuickCheckYes()
//        {
//            int end = normalizer.spanQuickCheckYes(inputBuffer);
//            if (end > 0)
//            {
//                //resultBuffer.Append(inputBuffer.subSequence(0, end));
//                resultBuffer.Append(inputBuffer.ToString(0, end));
//                //inputBuffer.delete(0, end);
//                inputBuffer.Remove(0, end);
//                checkedInputBoundary = Math.Max(checkedInputBoundary - end, 0);
//                charCount += end;
//            }
//            return end;
//        }

//        private int ReadFromIoNormalizeUptoBoundary()
//        {
//            // if there's no buffer to normalize, return 0
//            if (inputBuffer.Length <= 0)
//            {
//                return 0;
//            }

//            bool foundBoundary = false;
//            int bufLen = inputBuffer.Length;

//            while (checkedInputBoundary <= bufLen - 1)
//            {
//                int charLen = Character.CharCount(inputBuffer.CodePointAt(checkedInputBoundary));
//                checkedInputBoundary += charLen;
//                if (checkedInputBoundary < bufLen && normalizer.HasBoundaryBefore(inputBuffer
//                  .CodePointAt(checkedInputBoundary)))
//                {
//                    foundBoundary = true;
//                    break;
//                }
//            }
//            if (!foundBoundary && checkedInputBoundary >= bufLen && inputFinished)
//            {
//                foundBoundary = true;
//                checkedInputBoundary = bufLen;
//            }

//            if (!foundBoundary)
//            {
//                return 0;
//            }

//            return NormalizeInputUpto(checkedInputBoundary);
//        }

//        private int NormalizeInputUpto(int length)
//        {
//            int destOrigLen = resultBuffer.Length;
//            normalizer.NormalizeSecondAndAppend(resultBuffer, inputBuffer.ToString(0, length));
//              //inputBuffer.SubSequence(0, length));

//            //inputBuffer.Delete(0, length);
//            inputBuffer.Remove(0, length);
//            checkedInputBoundary = Math.Max(checkedInputBoundary - length, 0);
//            int resultLength = resultBuffer.Length - destOrigLen;
//            RecordOffsetDiff(length, resultLength);
//            return resultLength;
//        }

//        private void RecordOffsetDiff(int inputLength, int outputLength)
//        {
//            if (inputLength == outputLength)
//            {
//                charCount += outputLength;
//                return;
//            }
//            int diff = inputLength - outputLength;
//            int cumuDiff = LastCumulativeDiff;
//            if (diff < 0)
//            {
//                for (int i = 1; i <= -diff; ++i)
//                {
//                    AddOffCorrectMap(charCount + i, cumuDiff - i);
//                }
//            }
//            else
//            {
//                AddOffCorrectMap(charCount + outputLength, cumuDiff + diff);
//            }
//            charCount += outputLength;
//        }

//        private int OutputFromResultBuffer(char[] cbuf, int begin, int len)
//        {
//            len = Math.Min(resultBuffer.Length, len);
//            //resultBuffer.GetChars(0, len, cbuf, begin);
//            resultBuffer.CopyTo(0, cbuf, begin, len);
//            if (len > 0)
//            {
//                //resultBuffer.delete(0, len);
//                resultBuffer.Remove(0, len);
//            }
//            return len;
//        }
//    }
//}
