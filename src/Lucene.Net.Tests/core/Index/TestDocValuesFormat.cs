using NUnit.Framework;

namespace Lucene.Net.Index
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

    using Codec = Lucene.Net.Codecs.Codec;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests the codec configuration defined by LuceneTestCase randomly
    ///  (typically a mix across different fields).
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    public class TestDocValuesFormat : BaseDocValuesFormatTestCase
    {
        protected override Codec Codec
        {
            get
            {
                return Codec.Default;
            }
        }

        protected internal override bool CodecAcceptsHugeBinaryValues(string field)
        {
            return TestUtil.FieldSupportsHugeBinaryDocValues(field);
        }


        #region BaseDocValuesFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestOneNumber()
        {
            base.TestOneNumber();
        }

        [Test]
        public override void TestOneFloat()
        {
            base.TestOneFloat();
        }

        [Test]
        public override void TestTwoNumbers()
        {
            base.TestTwoNumbers();
        }

        [Test]
        public override void TestTwoBinaryValues()
        {
            base.TestTwoBinaryValues();
        }

        [Test]
        public override void TestTwoFieldsMixed()
        {
            base.TestTwoFieldsMixed();
        }

        [Test]
        public override void TestThreeFieldsMixed()
        {
            base.TestThreeFieldsMixed();
        }

        [Test]
        public override void TestThreeFieldsMixed2()
        {
            base.TestThreeFieldsMixed2();
        }

        [Test]
        public override void TestTwoDocumentsNumeric()
        {
            base.TestTwoDocumentsNumeric();
        }

        [Test]
        public override void TestTwoDocumentsMerged()
        {
            base.TestTwoDocumentsMerged();
        }

        [Test]
        public override void TestBigNumericRange()
        {
            base.TestBigNumericRange();
        }

        [Test]
        public override void TestBigNumericRange2()
        {
            base.TestBigNumericRange2();
        }

        [Test]
        public override void TestBytes()
        {
            base.TestBytes();
        }

        [Test]
        public override void TestBytesTwoDocumentsMerged()
        {
            base.TestBytesTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedBytes()
        {
            base.TestSortedBytes();
        }

        [Test]
        public override void TestSortedBytesTwoDocuments()
        {
            base.TestSortedBytesTwoDocuments();
        }

        [Test]
        public override void TestSortedBytesThreeDocuments()
        {
            base.TestSortedBytesThreeDocuments();
        }

        [Test]
        public override void TestSortedBytesTwoDocumentsMerged()
        {
            base.TestSortedBytesTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedMergeAwayAllValues()
        {
            base.TestSortedMergeAwayAllValues();
        }

        [Test]
        public override void TestBytesWithNewline()
        {
            base.TestBytesWithNewline();
        }

        [Test]
        public override void TestMissingSortedBytes()
        {
            base.TestMissingSortedBytes();
        }

        [Test]
        public override void TestSortedTermsEnum()
        {
            base.TestSortedTermsEnum();
        }

        [Test]
        public override void TestEmptySortedBytes()
        {
            base.TestEmptySortedBytes();
        }

        [Test]
        public override void TestEmptyBytes()
        {
            base.TestEmptyBytes();
        }

        [Test]
        public override void TestVeryLargeButLegalBytes()
        {
            base.TestVeryLargeButLegalBytes();
        }

        [Test]
        public override void TestVeryLargeButLegalSortedBytes()
        {
            base.TestVeryLargeButLegalSortedBytes();
        }

        [Test]
        public override void TestCodecUsesOwnBytes()
        {
            base.TestCodecUsesOwnBytes();
        }

        [Test]
        public override void TestCodecUsesOwnSortedBytes()
        {
            base.TestCodecUsesOwnSortedBytes();
        }

        [Test]
        public override void TestCodecUsesOwnBytesEachTime()
        {
            base.TestCodecUsesOwnBytesEachTime();
        }

        [Test]
        public override void TestCodecUsesOwnSortedBytesEachTime()
        {
            base.TestCodecUsesOwnSortedBytesEachTime();
        }

        /*
         * Simple test case to show how to use the API
         */
        [Test]
        public override void TestDocValuesSimple()
        {
            base.TestDocValuesSimple();
        }

        [Test]
        public override void TestRandomSortedBytes()
        {
            base.TestRandomSortedBytes();
        }

        [Test]
        public override void TestBooleanNumericsVsStoredFields()
        {
            base.TestBooleanNumericsVsStoredFields();
        }

        [Test]
        public override void TestByteNumericsVsStoredFields()
        {
            base.TestByteNumericsVsStoredFields();
        }

        [Test]
        public override void TestByteMissingVsFieldCache()
        {
            base.TestByteMissingVsFieldCache();
        }

        [Test]
        public override void TestShortNumericsVsStoredFields()
        {
            base.TestShortNumericsVsStoredFields();
        }

        [Test]
        public override void TestShortMissingVsFieldCache()
        {
            base.TestShortMissingVsFieldCache();
        }

        [Test]
        public override void TestIntNumericsVsStoredFields()
        {
            base.TestIntNumericsVsStoredFields();
        }

        [Test]
        public override void TestIntMissingVsFieldCache()
        {
            base.TestIntMissingVsFieldCache();
        }

        [Test]
        public override void TestLongNumericsVsStoredFields()
        {
            base.TestLongNumericsVsStoredFields();
        }

        [Test]
        public override void TestLongMissingVsFieldCache()
        {
            base.TestLongMissingVsFieldCache();
        }

        [Test]
        public override void TestBinaryFixedLengthVsStoredFields()
        {
            base.TestBinaryFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestBinaryVariableLengthVsStoredFields()
        {
            base.TestBinaryVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedFixedLengthVsStoredFields()
        {
            base.TestSortedFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedFixedLengthVsFieldCache()
        {
            base.TestSortedFixedLengthVsFieldCache();
        }

        [Test]
        public override void TestSortedVariableLengthVsFieldCache()
        {
            base.TestSortedVariableLengthVsFieldCache();
        }

        [Test]
        public override void TestSortedVariableLengthVsStoredFields()
        {
            base.TestSortedVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetOneValue()
        {
            base.TestSortedSetOneValue();
        }

        [Test]
        public override void TestSortedSetTwoFields()
        {
            base.TestSortedSetTwoFields();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsMerged()
        {
            base.TestSortedSetTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedSetTwoValues()
        {
            base.TestSortedSetTwoValues();
        }

        [Test]
        public override void TestSortedSetTwoValuesUnordered()
        {
            base.TestSortedSetTwoValuesUnordered();
        }

        [Test]
        public override void TestSortedSetThreeValuesTwoDocs()
        {
            base.TestSortedSetThreeValuesTwoDocs();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsLastMissing()
        {
            base.TestSortedSetTwoDocumentsLastMissing();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsLastMissingMerge()
        {
            base.TestSortedSetTwoDocumentsLastMissingMerge();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsFirstMissing()
        {
            base.TestSortedSetTwoDocumentsFirstMissing();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsFirstMissingMerge()
        {
            base.TestSortedSetTwoDocumentsFirstMissingMerge();
        }

        [Test]
        public override void TestSortedSetMergeAwayAllValues()
        {
            base.TestSortedSetMergeAwayAllValues();
        }

        [Test]
        public override void TestSortedSetTermsEnum()
        {
            base.TestSortedSetTermsEnum();
        }

        [Test]
        public override void TestSortedSetFixedLengthVsStoredFields()
        {
            base.TestSortedSetFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetVariableLengthVsStoredFields()
        {
            base.TestSortedSetVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetFixedLengthSingleValuedVsStoredFields()
        {
            base.TestSortedSetFixedLengthSingleValuedVsStoredFields();
        }

        [Test]
        public override void TestSortedSetVariableLengthSingleValuedVsStoredFields()
        {
            base.TestSortedSetVariableLengthSingleValuedVsStoredFields();
        }

        [Test]
        public override void TestSortedSetFixedLengthVsUninvertedField()
        {
            base.TestSortedSetFixedLengthVsUninvertedField();
        }

        [Test]
        public override void TestSortedSetVariableLengthVsUninvertedField()
        {
            base.TestSortedSetVariableLengthVsUninvertedField();
        }

        [Test]
        public override void TestGCDCompression()
        {
            base.TestGCDCompression();
        }

        [Test]
        public override void TestZeros()
        {
            base.TestZeros();
        }

        [Test]
        public override void TestZeroOrMin()
        {
            base.TestZeroOrMin();
        }

        [Test]
        public override void TestTwoNumbersOneMissing()
        {
            base.TestTwoNumbersOneMissing();
        }

        [Test]
        public override void TestTwoNumbersOneMissingWithMerging()
        {
            base.TestTwoNumbersOneMissingWithMerging();
        }

        [Test]
        public override void TestThreeNumbersOneMissingWithMerging()
        {
            base.TestThreeNumbersOneMissingWithMerging();
        }

        [Test]
        public override void TestTwoBytesOneMissing()
        {
            base.TestTwoBytesOneMissing();
        }

        [Test]
        public override void TestTwoBytesOneMissingWithMerging()
        {
            base.TestTwoBytesOneMissingWithMerging();
        }

        [Test]
        public override void TestThreeBytesOneMissingWithMerging()
        {
            base.TestThreeBytesOneMissingWithMerging();
        }

        // LUCENE-4853
        [Test]
        public override void TestHugeBinaryValues()
        {
            base.TestHugeBinaryValues();
        }

        // TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
        [Test]
        public override void TestHugeBinaryValueLimit()
        {
            base.TestHugeBinaryValueLimit();
        }

        /// <summary>
        /// Tests dv against stored fields with threads (binary/numeric/sorted, no missing)
        /// </summary>
        [Test]
        public override void TestThreads()
        {
            base.TestThreads();
        }

        /// <summary>
        /// Tests dv against stored fields with threads (all types + missing)
        /// </summary>
        [Test]
        public override void TestThreads2()
        {
            base.TestThreads2();
        }

        // LUCENE-5218
        [Test]
        public override void TestEmptyBinaryValueOnPageSizes()
        {
            base.TestEmptyBinaryValueOnPageSizes();
        }

        #endregion

        #region BaseIndexFileFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestMergeStability()
        {
            base.TestMergeStability();
        }

        #endregion
    }
}