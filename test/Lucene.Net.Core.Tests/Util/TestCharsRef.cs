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

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Lucene.Net.Random;
    using System.Text;


    public class TestCharsRef : LuceneTestCase
    {
        [Test]
        public void testUTF16InUTF8Order()
        {
            int iterations = this.AtLeast(1000);
            BytesRef[] utf8 = new BytesRef[iterations];
            CharsRef[] utf16 = new CharsRef[iterations];

            iterations.Times((i) =>
            {
                var s = this.Random.RandomUnicodeString();
                utf8[i] = new BytesRef(s);
                utf16[i] = new CharsRef(s);
            });

            Array.Sort(utf8);
#pragma warning disable 0612, 0618
            Array.Sort(utf16, CharsRef.UTF16SortedAsUTF8Comparer);
#pragma warning restore 0612, 0618

            iterations.Times((i) => Equal(utf8[i].Utf8ToString(), utf16[i].ToString()));
        }


        [Test]
        public void TestAppend() {
            var builder = new CharsRefBuilder();
            var sb = new StringBuilder();
            int iterations = this.AtLeast(10);

            iterations.Times((i) => {
                var charArray = this.Random.RandomRealisticUnicodeString(1, 100).ToCharArray();
                int offset = this.Random.Next(charArray.Length);
                int length = charArray.Length - offset;
                sb.Append(charArray, offset, length);
                builder.Append(charArray, offset, length);  
            });
   
    
            Equal(sb.ToString(), builder.CharRef.ToString());
        }

        [Test]
        public void TestCopy()
        {
            var iterations = this.AtLeast(10);
            iterations.Times((i) => {
                var builder = new CharsRefBuilder();
                var charArray = this.Random.RandomRealisticUnicodeString(1, 100).ToCharArray();
                int offset = this.Random.Next(charArray.Length),
                    length = charArray.Length - offset;

               String str = new String(charArray, offset, length);
               builder.CopyChars(charArray, offset, length);
             
               Equals(str, builder.ToString());  
            });
        }

        [Test]
        [Ticket("LUCENE-3590", "fix charsequence to fully obey interface")]
        public void TestCharSequenceAt()
        {
            var c = new CharsRef("abc");

            Equal('b', c.CharAt(1));
            Equal('b', c.ElementAt(1));

            Throws<IndexOutOfRangeException>(() =>
            {
                c.CharAt(-1);
            });

            Throws<IndexOutOfRangeException>(() =>
            {
                c.CharAt(3);
            });
        }

    
        [Ticket("LUCENE-3590", " fix off-by-one in subsequence, and fully obey interface")]
        [Ticket("LUCENE-4671", " fix subSequence")]
        public void TestCharSequenceSubSequence() {
            CharsRef[] sequences =  {
                new CharsRef("abc"),
                new CharsRef("0abc".ToCharArray(), 1, 3),
                new CharsRef("abc0".ToCharArray(), 0, 3),
                new CharsRef("0abc0".ToCharArray(), 1, 3)
            };
    
            foreach(var c in sequences) {
               AssertSequence(c);
            }
        }

        private void AssertSequence(CharsRef c)
        {
            // 

            Equal("a", c.SubSequence(0, 1).ToString());
            
            var sub1 = string.Join("", c.Take(1));
            Ok("a" == sub1, "sub1 should be 'a' but was '{0}' from ref {1}", sub1, c.ToString());
            
            // mid subsequence
            Equal("b", c.SubSequence(1, 2).ToString());

            var sub2 = string.Join("", c.Skip(1).Take(1));
            Ok("b" == sub2, "sub1 should be 'b' but was '{0}' from ref {1}", sub2, c.ToString());
            
            // end subsequence
            Equal("bc", c.SubSequence(1, 3).ToString());

            var sub3 = string.Join("", c.Skip(1).Take(2));
            Ok("bc" == sub3, "sub3 should be 'bc' but was '{0}' from ref {1}", sub3, c.ToString());
            
            // empty subsequence
            Equal("", c.SubSequence(0, 0).ToString());
            Equal("", string.Join("", c.Skip(0).Take(0)));
            
            Throws<IndexOutOfRangeException>(() =>
            {
                c.SubSequence(-1, 1);
            });

            Throws<IndexOutOfRangeException>(() =>
            {
                c.SubSequence(0, -1);
            });

            Throws<IndexOutOfRangeException>(() =>
            {
                c.SubSequence(0, 4);
            });

            Throws<IndexOutOfRangeException>(() =>
            {
                c.SubSequence(2, 1);
            });
            
        }
    }
}
