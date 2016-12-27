using Lucene.Net.Randomized.Generators;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with this
     * work for additional information regarding copyright ownership. The ASF
     * licenses this file to You under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
     * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
     * License for the specific language governing permissions and limitations under
     * the License.
     */

    [TestFixture]
    public class TestBytesRefArray : LuceneTestCase
    {
        [Test]
        public virtual void TestAppend()
        {
            Random random = Random();
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            IList<string> stringList = new List<string>();
            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Size;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(random);
                    spare.CopyChars(randomRealisticUnicodeString);
                    Assert.AreEqual(i + initSize, list.Append(spare));
                    stringList.Add(randomRealisticUnicodeString);
                }
                for (int i = 0; i < entries; i++)
                {
                    Assert.IsNotNull(list.Get(spare, i));
                    Assert.AreEqual(stringList[i], spare.Utf8ToString(), "entry " + i + " doesn't match");
                }

                // check random
                for (int i = 0; i < entries; i++)
                {
                    int e = random.Next(entries);
                    Assert.IsNotNull(list.Get(spare, e));
                    Assert.AreEqual(stringList[e], spare.Utf8ToString(), "entry " + i + " doesn't match");
                }
                for (int i = 0; i < 2; i++)
                {
                    IBytesRefIterator iterator = list.Iterator();
                    foreach (string @string in stringList)
                    {
                        Assert.AreEqual(@string, iterator.Next().Utf8ToString());
                    }
                }
            }
        }

        [Test]
        public virtual void TestSort()
        {
            Random random = Random();
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            List<string> stringList = new List<string>();

            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Size;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(random);
                    spare.CopyChars(randomRealisticUnicodeString);
                    Assert.AreEqual(initSize + i, list.Append(spare));
                    stringList.Add(randomRealisticUnicodeString);
                }

                stringList.Sort();
                IBytesRefIterator iter = list.Iterator(BytesRef.UTF8SortedAsUTF16Comparer);
                int a = 0;
                while ((spare = iter.Next()) != null)
                {
                    Assert.AreEqual(stringList[a], spare.Utf8ToString(), "entry " + a + " doesn't match");
                    a++;
                }
                Assert.IsNull(iter.Next());
                Assert.AreEqual(a, stringList.Count);
            }
        }
    }
}