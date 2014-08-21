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

namespace Lucene.Net.Util
{
    using System.Linq;
    using Lucene.Net.Random;
    using System;
    using System.Collections.Generic;
   

    public class TestBytesRefArray : LuceneTestCase
    {
        [Test]
        public virtual void TestAppend()
        {
            var random = this.Random;
            var  list = new BytesRefArray(Counter.NewCounter());
            IList<string> stringList = new List<string>();
            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = this.AtLeast(500);
                var spare = new BytesRefBuilder();
                int initSize = list.Length;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = random.RandomRealisticUnicodeString();
                    spare.CopyChars(randomRealisticUnicodeString);
                    Equal(i + initSize, list.Append(spare.ToBytesRef()));
                    stringList.Add(randomRealisticUnicodeString);
                }
                for (int i = 0; i < entries; i++)
                {
                    var bytesRef = list.Retrieve(spare, i);
                    NotNull(bytesRef);
                    Equal(stringList[i], bytesRef.Utf8ToString(), "entry " + i + " doesn't match");
                }

                // check random
                for (int i = 0; i < entries; i++)
                {
                    int e = random.Next(entries);
                    var bytesRef = list.Retrieve(spare, e);
                    NotNull(bytesRef);
                    Equal(stringList[e], bytesRef.Utf8ToString(), "entry " + i + " doesn't match");
                }
                for (int i = 0; i < 2; i++)
                {
                    var iterator = list.GetEnumerator();
                    foreach (string @string in stringList)
                    {
                        iterator.MoveNext();
                        Equal(@string, iterator.Current.Utf8ToString());
                    }
                }
            }
        }

        [Test]
        public virtual void TestSort()
        {
            var random = this.Random;
            var list = new BytesRefArray(Util.Counter.NewCounter());
            var stringList = new List<string>();

            for (int j = 0; j < 2; j++)
            {
                if (j > 0 && random.NextBoolean())
                {
                    list.Clear();
                    stringList.Clear();
                }
                int entries = this.AtLeast(500);
                var spare = new BytesRef();
                int initSize = list.Length;
                for (int i = 0; i < entries; i++)
                {
                    string randomRealisticUnicodeString = random.RandomRealisticUnicodeString();
                    spare.CopyChars(randomRealisticUnicodeString);
                    Equal(initSize + i, list.Append(spare));
                    stringList.Add(randomRealisticUnicodeString);
                }


#pragma warning disable 0612,  0618

                // requires a custom comparison
                stringList.Sort(new Comparison<string>((left, right) =>
                {
                    if (left == right)
                        return 0;

                    var limit = left.Length;
                    if (right.Length < limit)
                        limit = right.Length;

                    var i = 0;
                    while (i < limit)
                    {
                        char x = left[i],
                            y = right[i];

                        if (x == y)
                        {
                            i++;
                            continue;
                        }
                            

                        if (x > y)
                            return 1;
                        
                        if(y > x)
                            return -1;  
                    }

                    return left.Length - right.Length;
                }));
               // var items = list.OrderBy(o => o, BytesRef.Utf8SortedAsUtf16Comparer).ToList();
                var iterator = list.GetEnumerator(BytesRef.Utf8SortedAsUtf16Comparer);
                var items = new List<BytesRef>();
                while(iterator.MoveNext())
                    items.Add(iterator.Current);

#pragma warning restore 0612, 0618
                int a = 0;
                foreach (var item
                    in items)
                {
                    Equal(stringList[a], item.Utf8ToString(), "entry " + a + " doesn't match");
                    a++;
                }
                Null(iterator.Current);
                Equal(a, stringList.Count);
            }
        }
    }
}