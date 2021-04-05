// Lucene version compatibility level 8.2.0
using ICU4N.Support.Text;
using ICU4N.Text;
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.OpenNlp
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

    public class TestOpenNLPSentenceBreakIterator : LuceneTestCase
    {
        private const String TEXT
            //                                                                                                     111
            //           111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999000
            // 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012
            = "Sentence number 1 has 6 words. Sentence number 2, 5 words. And finally, sentence number 3 has 8 words.";
        private static readonly String[] SENTENCES = new String[] {
            "Sentence number 1 has 6 words. ", "Sentence number 2, 5 words. ", "And finally, sentence number 3 has 8 words." };
        private const String PADDING = " Word. Word. ";
        private const String sentenceModelFile = "en-test-sent.bin";

        public override void BeforeClass()
        {
            base.BeforeClass();
            PopulateCache();
        }

        public static void PopulateCache()
        {
            OpenNLPOpsFactory.GetSentenceModel(sentenceModelFile, new ClasspathResourceLoader(typeof(TestOpenNLPSentenceBreakIterator)));
        }

        [Test]
        public void TestThreeSentences()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(TEXT); // String is converted to StringCharacterIterator

            Do3SentenceTest(bi);

            bi.SetText(GetCharArrayIterator(TEXT));
            Do3SentenceTest(bi);
        }

        private CharacterIterator GetCharArrayIterator(String text)
        {
            return GetCharArrayIterator(text, 0, text.Length);
        }

        private class WorkaroundCharArrayIterator : CharArrayIterator
        {
            // Lie about all surrogates to the sentence tokenizer,
            // instead we treat them all as SContinue so we won't break around them.
            protected override char JreBugWorkaround(char ch)
            {
                return ch >= 0xD800 && ch <= 0xDFFF ? (char)0x002C : ch;
            }
        }

        private CharacterIterator GetCharArrayIterator(String text, int start, int length)
        {
            //    CharArrayIterator charArrayIterator = new CharArrayIterator() {
            //      // Lie about all surrogates to the sentence tokenizer,
            //      // instead we treat them all as SContinue so we won't break around them.
            //      protected override char JreBugWorkaround(char ch)
            //    {
            //        return ch >= 0xD800 && ch <= 0xDFFF ? 0x002C : ch;
            //    }
            //};
            CharArrayIterator charArrayIterator = new WorkaroundCharArrayIterator();
            charArrayIterator.SetText(text.ToCharArray(), start, length);
            return charArrayIterator;
        }

        private void Do3SentenceTest(BreakIterator bi) // LUCENENET NOTE: Refactored a bit because Substring in .NET requires some light math to match Java
        {
            assertEquals(0, bi.Current);
            assertEquals(0, bi.First());
            int current = bi.Current;
            assertEquals(SENTENCES[0], TEXT.Substring(current, bi.Next() - current)); // LUCNENENET: Corrected 2nd parameter
            current = bi.Current;
            assertEquals(SENTENCES[1], TEXT.Substring(current, bi.Next() - current)); // LUCNENENET: Corrected 2nd parameter
            current = bi.Current;
            assertEquals(bi.Text.EndIndex, bi.Next());
            int next = bi.Current;
            assertEquals(SENTENCES[2], TEXT.Substring(current, next - current)); // LUCNENENET: Corrected 2nd parameter
            assertEquals(BreakIterator.Done, bi.Next());

            assertEquals(TEXT.Length, bi.Last());
            int end = bi.Current;
            int prev = bi.Previous();
            assertEquals(SENTENCES[2], TEXT.Substring(prev, end - prev)); // LUCNENENET: Corrected 2nd parameter
            end = bi.Current;
            prev = bi.Previous();
            assertEquals(SENTENCES[1], TEXT.Substring(prev, end - prev)); // LUCNENENET: Corrected 2nd parameter
            end = bi.Current;
            prev = bi.Previous();
            assertEquals(SENTENCES[0], TEXT.Substring(prev, end - prev)); // LUCNENENET: Corrected 2nd parameter
            assertEquals(BreakIterator.Done, bi.Previous());
            assertEquals(0, bi.Current);

            assertEquals(59, bi.Following(39));
            assertEquals(59, bi.Following(31));
            assertEquals(31, bi.Following(30));

            assertEquals(0, bi.Preceding(57));
            assertEquals(0, bi.Preceding(58));
            assertEquals(31, bi.Preceding(59));

            assertEquals(0, bi.First());
            assertEquals(59, bi.Next(2));
            assertEquals(0, bi.Next(-2));
        }

        [Test]
        public void TestSingleSentence()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(GetCharArrayIterator(SENTENCES[0]));
            Test1Sentence(bi, SENTENCES[0]);
        }

        private void Test1Sentence(BreakIterator bi, String text)
        {
            int start = bi.Text.BeginIndex;
            assertEquals(start, bi.First());
            int current = bi.Current;
            assertEquals(bi.Text.EndIndex, bi.Next());
            int end = bi.Current - start;
            assertEquals(text, text.Substring(current - start, end - start));

            assertEquals(text.Length, bi.Last() - start);
            end = bi.Current;
            bi.Previous();
            assertEquals(BreakIterator.Done, bi.Previous());
            int previous = bi.Current;
            assertEquals(text, text.Substring(previous - start, end - start));
            assertEquals(start, bi.Current);

            assertEquals(BreakIterator.Done, bi.Following(bi.Last() / 2 + start));

            assertEquals(BreakIterator.Done, bi.Preceding(bi.Last() / 2 + start));

            assertEquals(start, bi.First());
            assertEquals(BreakIterator.Done, bi.Next(13));
            assertEquals(BreakIterator.Done, bi.Next(-8));
        }

        [Test]
        public void TestSliceEnd()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(GetCharArrayIterator(SENTENCES[0] + PADDING, 0, SENTENCES[0].Length));

            Test1Sentence(bi, SENTENCES[0]);
        }

        [Test]
        public void TestSliceStart()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(GetCharArrayIterator(PADDING + SENTENCES[0], PADDING.Length, SENTENCES[0].Length));
            Test1Sentence(bi, SENTENCES[0]);
        }

        [Test]
        public void TestSliceMiddle()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(GetCharArrayIterator(PADDING + SENTENCES[0] + PADDING, PADDING.Length, SENTENCES[0].Length));

            Test1Sentence(bi, SENTENCES[0]);
        }

        /** the current position must be ignored, initial position is always first() */
        [Test]
        public void TestFirstPosition()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText(GetCharArrayIterator(SENTENCES[0]));
            assertEquals(SENTENCES[0].Length, bi.Last()); // side-effect: set current position to last()
            Test1Sentence(bi, SENTENCES[0]);
        }

        [Test]
        public void TestWhitespaceOnly()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText("   \n \n\n\r\n\t  \n");
            Test0Sentences(bi);
        }

        [Test]
        public void TestEmptyString()
        {
            NLPSentenceDetectorOp sentenceDetectorOp = OpenNLPOpsFactory.GetSentenceDetector(sentenceModelFile);
            BreakIterator bi = new OpenNLPSentenceBreakIterator(sentenceDetectorOp);
            bi.SetText("");
            Test0Sentences(bi);
        }

        private void Test0Sentences(BreakIterator bi)
        {
            assertEquals(0, bi.Current);
            assertEquals(0, bi.First());
            assertEquals(BreakIterator.Done, bi.Next());
            assertEquals(0, bi.Last());
            assertEquals(BreakIterator.Done, bi.Previous());
            assertEquals(BreakIterator.Done, bi.Following(0));
            assertEquals(BreakIterator.Done, bi.Preceding(0));
            assertEquals(0, bi.First());
            assertEquals(BreakIterator.Done, bi.Next(13));
            assertEquals(BreakIterator.Done, bi.Next(-8));
        }
    }
}
