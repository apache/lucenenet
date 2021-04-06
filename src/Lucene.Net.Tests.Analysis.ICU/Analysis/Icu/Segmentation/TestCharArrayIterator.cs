// Lucene version compatibility level 7.1.0
#if FEATURE_BREAKITERATOR
using ICU4N.Support.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Icu.Segmentation
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

    public class TestCharArrayIterator : LuceneTestCase
    {
        [Test]
        public void TestBasicUsage()
        {
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText("testing".toCharArray(), 0, "testing".Length);
            assertEquals(0, ci.BeginIndex);
            assertEquals(7, ci.EndIndex);
            assertEquals(0, ci.Index);
            assertEquals('t', ci.Current);
            assertEquals('e', ci.Next());
            assertEquals('g', ci.Last());
            assertEquals('n', ci.Previous());
            assertEquals('t', ci.First());
            assertEquals(CharacterIterator.Done, ci.Previous());
        }

        [Test]
        public void TestFirst()
        {
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText("testing".toCharArray(), 0, "testing".Length);
            ci.Next();
            // Sets the position to getBeginIndex() and returns the character at that position. 
            assertEquals('t', ci.First());
            assertEquals(ci.BeginIndex, ci.Index);
            // or DONE if the text is empty
            ci.SetText(new char[] { }, 0, 0);
            assertEquals(CharacterIterator.Done, ci.First());
        }

        [Test]
        public void TestLast()
        {
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText("testing".toCharArray(), 0, "testing".Length);
            // Sets the position to getEndIndex()-1 (getEndIndex() if the text is empty) 
            // and returns the character at that position. 
            assertEquals('g', ci.Last());
            assertEquals(ci.Index, ci.EndIndex - 1);
            // or DONE if the text is empty
            ci.SetText(new char[] { }, 0, 0);
            assertEquals(CharacterIterator.Done, ci.Last());
            assertEquals(ci.EndIndex, ci.Index);
        }

        [Test]
        public void TestCurrent()
        {
            CharArrayIterator ci = new CharArrayIterator();
            // Gets the character at the current position (as returned by getIndex()). 
            ci.SetText("testing".toCharArray(), 0, "testing".Length);
            assertEquals('t', ci.Current);
            ci.Last();
            ci.Next();
            // or DONE if the current position is off the end of the text.
            assertEquals(CharacterIterator.Done, ci.Current);
        }

        [Test]
        public void TestNext()
        {
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText("te".toCharArray(), 0, 2);
            // Increments the iterator's index by one and returns the character at the new index.
            assertEquals('e', ci.Next());
            assertEquals(1, ci.Index);
            // or DONE if the new position is off the end of the text range.
            assertEquals(CharacterIterator.Done, ci.Next());
            assertEquals(ci.EndIndex, ci.Index);
        }

        [Test]
        public void TestSetIndex()
        {
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText("test".toCharArray(), 0, "test".Length);
            try
            {
                ci.SetIndex(5);
                fail();
            }
            catch (Exception e) when (e.IsException())
            {
                assertTrue(e is ArgumentException);
            }
        }

        [Test]
        public void TestClone()
        {
            char[] text = "testing".toCharArray();
            CharArrayIterator ci = new CharArrayIterator();
            ci.SetText(text, 0, text.Length);
            ci.Next();
            CharArrayIterator ci2 = (CharArrayIterator)ci.Clone();
            assertEquals(ci.Index, ci2.Index);
            assertEquals(ci.Next(), ci2.Next());
            assertEquals(ci.Last(), ci2.Last());
        }
    }
}
#endif