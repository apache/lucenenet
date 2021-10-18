using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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
            Random random = Random;
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            IList<string> stringList = new JCG.List<string>();
            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Length;
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
                    IBytesRefEnumerator iterator = list.GetEnumerator();
                    foreach (string @string in stringList)
                    {
                        iterator.MoveNext();
                        Assert.AreEqual(@string, iterator.Current.Utf8ToString());
                    }
                }
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestAppendIterator()
        {
            Random random = Random;
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            IList<string> stringList = new JCG.List<string>();
            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Length;
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
                    IBytesRefEnumerator iterator = list.GetEnumerator();
                    foreach (string @string in stringList)
                    {
                        Assert.IsTrue(iterator.MoveNext());
                        Assert.AreEqual(@string, iterator.Current.Utf8ToString());
                    }
                }
            }
        }

        [Test]
        public virtual void TestSort()
        {
            Random random = Random;
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            IList<string> stringList = new JCG.List<string>();

            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Length;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(random);
                    spare.CopyChars(randomRealisticUnicodeString);
                    Assert.AreEqual(initSize + i, list.Append(spare));
                    stringList.Add(randomRealisticUnicodeString);
                }

                // LUCENENET NOTE: Must sort using ArrayUtil.GetNaturalComparator<T>()
                // to ensure culture isn't taken into consideration during the sort,
                // which will match the sort order of BytesRef.UTF8SortedAsUTF16Comparer.
                CollectionUtil.TimSort(stringList);
#pragma warning disable 612, 618
                IBytesRefEnumerator iter = list.GetEnumerator(BytesRef.UTF8SortedAsUTF16Comparer);
#pragma warning restore 612, 618
                int a = 0;
                while (iter.MoveNext())
                {
                    Assert.AreEqual(stringList[a], iter.Current.Utf8ToString(), "entry " + a + " doesn't match");
                    a++;
                }
                Assert.IsFalse(iter.MoveNext());
                Assert.AreEqual(a, stringList.Count);
            }
        }

        [Test]
        [Obsolete("This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void TestSortIterator()
        {
            Random random = Random;
            BytesRefArray list = new BytesRefArray(Util.Counter.NewCounter());
            IList<string> stringList = new JCG.List<string>();

            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = AtLeast(500);
                BytesRef spare = new BytesRef();
                int initSize = list.Length;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(random);
                    spare.CopyChars(randomRealisticUnicodeString);
                    Assert.AreEqual(initSize + i, list.Append(spare));
                    stringList.Add(randomRealisticUnicodeString);
                }

                // LUCENENET NOTE: Must sort using ArrayUtil.GetNaturalComparator<T>()
                // to ensure culture isn't taken into consideration during the sort,
                // which will match the sort order of BytesRef.UTF8SortedAsUTF16Comparer.
                CollectionUtil.TimSort(stringList);
#pragma warning disable 612, 618
                IBytesRefIterator iter = list.GetIterator(BytesRef.UTF8SortedAsUTF16Comparer);
#pragma warning restore 612, 618
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