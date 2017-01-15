using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Suggest
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

    public class TestInputIterator : LuceneTestCase
    {
        [Test]
        public void TestEmpty()
        {
            InputArrayIterator iterator = new InputArrayIterator(new Input[0]);
            IInputIterator wrapper = new SortedInputIterator(iterator, BytesRef.UTF8SortedAsUnicodeComparer);
            assertNull(wrapper.Next());
            wrapper = new UnsortedInputIterator(iterator);
            assertNull(wrapper.Next());
        }

        [Test]
        public void TestTerms()
        {
            Random random = Random();
            int num = AtLeast(10000);
#pragma warning disable 612, 618
            IComparer<BytesRef> comparer = random.nextBoolean() ? BytesRef.UTF8SortedAsUnicodeComparer : BytesRef.UTF8SortedAsUTF16Comparer;
#pragma warning restore 612, 618
            IDictionary<BytesRef, KeyValuePair<long, BytesRef>> sorted = new SortedDictionary<BytesRef, KeyValuePair<long, BytesRef>>(comparer); //new TreeMap<>(comparer);
            IDictionary<BytesRef, long> sortedWithoutPayload = new SortedDictionary<BytesRef, long>(comparer); //new TreeMap<>(comparer);
            IDictionary<BytesRef, KeyValuePair<long, ISet<BytesRef>>> sortedWithContext = new SortedDictionary<BytesRef, KeyValuePair<long, ISet<BytesRef>>>(comparer); //new TreeMap<>(comparer);
            IDictionary<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>> sortedWithPayloadAndContext = new SortedDictionary<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>>(comparer); //new TreeMap<>(comparer);
            Input[] unsorted = new Input[num];
            Input[] unsortedWithoutPayload = new Input[num];
            Input[] unsortedWithContexts = new Input[num];
            Input[] unsortedWithPayloadAndContext = new Input[num];
            ISet<BytesRef> ctxs;
            for (int i = 0; i < num; i++)
            {
                BytesRef key2;
                BytesRef payload;
                ctxs = new HashSet<BytesRef>();
                do
                {
                    key2 = new BytesRef(TestUtil.RandomUnicodeString(random));
                    payload = new BytesRef(TestUtil.RandomUnicodeString(random));
                    for (int j = 0; j < AtLeast(2); j++)
                    {
                        ctxs.add(new BytesRef(TestUtil.RandomUnicodeString(random)));
                    }
                } while (sorted.ContainsKey(key2));
                long value = random.Next();
                sortedWithoutPayload.Put(key2, value);
                sorted.Put(key2, new KeyValuePair<long, BytesRef>(value, payload));
                sortedWithContext.Put(key2, new KeyValuePair<long, ISet<BytesRef>>(value, ctxs));
                sortedWithPayloadAndContext.Put(key2, new KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>(value, new KeyValuePair<BytesRef, ISet<BytesRef>>(payload, ctxs)));
                unsorted[i] = new Input(key2, value, payload);
                unsortedWithoutPayload[i] = new Input(key2, value);
                unsortedWithContexts[i] = new Input(key2, value, ctxs);
                unsortedWithPayloadAndContext[i] = new Input(key2, value, payload, ctxs);
            }

            // test the sorted iterator wrapper with payloads
            IInputIterator wrapper = new SortedInputIterator(new InputArrayIterator(unsorted), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, BytesRef>>> expected = sorted.GetEnumerator();
            while (expected.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, BytesRef>> entry = expected.Current;


                assertEquals(entry.Key, wrapper.Next());
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                assertEquals(entry.Value.Value, wrapper.Payload);
            }
            assertNull(wrapper.Next());

            // test the sorted iterator wrapper with contexts
            wrapper = new SortedInputIterator(new InputArrayIterator(unsortedWithContexts), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, ISet<BytesRef>>>> actualEntries = sortedWithContext.GetEnumerator();
            while (actualEntries.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, ISet<BytesRef>>> entry = actualEntries.Current;
                assertEquals(entry.Key, wrapper.Next());
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                ISet<BytesRef> actualCtxs = entry.Value.Value;
                assertEquals(actualCtxs, wrapper.Contexts);
            }
            assertNull(wrapper.Next());

            // test the sorted iterator wrapper with contexts and payload
            wrapper = new SortedInputIterator(new InputArrayIterator(unsortedWithPayloadAndContext), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>>> expectedPayloadContextEntries = sortedWithPayloadAndContext.GetEnumerator();
            while (expectedPayloadContextEntries.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>> entry = expectedPayloadContextEntries.Current;
                assertEquals(entry.Key, wrapper.Next());
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                ISet<BytesRef> actualCtxs = entry.Value.Value.Value;
                assertEquals(actualCtxs, wrapper.Contexts);
                BytesRef actualPayload = entry.Value.Value.Key;
                assertEquals(actualPayload, wrapper.Payload);
            }
            assertNull(wrapper.Next());

            // test the unsorted iterator wrapper with payloads
            wrapper = new UnsortedInputIterator(new InputArrayIterator(unsorted));
            IDictionary<BytesRef, KeyValuePair<long, BytesRef>> actual = new SortedDictionary<BytesRef, KeyValuePair<long, BytesRef>>(); //new TreeMap<>();
            BytesRef key;
            while ((key = wrapper.Next()) != null)
            {
                long value = wrapper.Weight;
                BytesRef payload = wrapper.Payload;
                actual.Put(BytesRef.DeepCopyOf(key), new KeyValuePair<long, BytesRef>(value, BytesRef.DeepCopyOf(payload)));
            }
            assertEquals(sorted, actual);

            // test the sorted iterator wrapper without payloads
            IInputIterator wrapperWithoutPayload = new SortedInputIterator(new InputArrayIterator(unsortedWithoutPayload), comparer);
            IEnumerator<KeyValuePair<BytesRef, long>> expectedWithoutPayload = sortedWithoutPayload.GetEnumerator();
            while (expectedWithoutPayload.MoveNext())
            {
                KeyValuePair<BytesRef, long> entry = expectedWithoutPayload.Current;


                assertEquals(entry.Key, wrapperWithoutPayload.Next());
                assertEquals(Convert.ToInt64(entry.Value), wrapperWithoutPayload.Weight);
                assertNull(wrapperWithoutPayload.Payload);
            }
            assertNull(wrapperWithoutPayload.Next());

            // test the unsorted iterator wrapper without payloads
            wrapperWithoutPayload = new UnsortedInputIterator(new InputArrayIterator(unsortedWithoutPayload));
            IDictionary<BytesRef, long> actualWithoutPayload = new SortedDictionary<BytesRef, long>(); //new TreeMap<>();
            while ((key = wrapperWithoutPayload.Next()) != null)
            {
                long value = wrapperWithoutPayload.Weight;
                assertNull(wrapperWithoutPayload.Payload);
                actualWithoutPayload.Put(BytesRef.DeepCopyOf(key), value);
            }
            assertEquals(sortedWithoutPayload, actualWithoutPayload);
        }

        public static long AsLong(BytesRef b)
        {
            return (((long)AsIntInternal(b, b.Offset) << 32) | AsIntInternal(b,
                b.Offset + 4) & 0xFFFFFFFFL);
        }

        private static int AsIntInternal(BytesRef b, int pos)
        {
            return ((b.Bytes[pos++] & 0xFF) << 24) | ((b.Bytes[pos++] & 0xFF) << 16)
                | ((b.Bytes[pos++] & 0xFF) << 8) | (b.Bytes[pos] & 0xFF);
        }
    }
}
