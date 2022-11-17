using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
            InputArrayEnumerator iterator = new InputArrayEnumerator(new Input[0]);
            IInputEnumerator wrapper = new SortedInputEnumerator(iterator, BytesRef.UTF8SortedAsUnicodeComparer);
            assertFalse(wrapper.MoveNext());
            wrapper = new UnsortedInputEnumerator(iterator);
            assertFalse(wrapper.MoveNext());
        }

        [Test]
        public void TestTerms()
        {
            Random random = Random;
            int num = AtLeast(10000);
#pragma warning disable 612, 618
            IComparer<BytesRef> comparer = random.nextBoolean() ? BytesRef.UTF8SortedAsUnicodeComparer : BytesRef.UTF8SortedAsUTF16Comparer;
#pragma warning restore 612, 618
            IDictionary<BytesRef, KeyValuePair<long, BytesRef>> sorted = new JCG.SortedDictionary<BytesRef, KeyValuePair<long, BytesRef>>(comparer);
            IDictionary<BytesRef, long> sortedWithoutPayload = new JCG.SortedDictionary<BytesRef, long>(comparer);
            IDictionary<BytesRef, KeyValuePair<long, ISet<BytesRef>>> sortedWithContext = new JCG.SortedDictionary<BytesRef, KeyValuePair<long, ISet<BytesRef>>>(comparer);
            IDictionary<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>> sortedWithPayloadAndContext = new JCG.SortedDictionary<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>>(comparer);
            Input[] unsorted = new Input[num];
            Input[] unsortedWithoutPayload = new Input[num];
            Input[] unsortedWithContexts = new Input[num];
            Input[] unsortedWithPayloadAndContext = new Input[num];
            ISet<BytesRef> ctxs;
            for (int i = 0; i < num; i++)
            {
                BytesRef key2;
                BytesRef payload;
                ctxs = new JCG.HashSet<BytesRef>();
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
                sortedWithoutPayload[key2] = value;
                sorted[key2] = new KeyValuePair<long, BytesRef>(value, payload);
                sortedWithContext[key2] = new KeyValuePair<long, ISet<BytesRef>>(value, ctxs);
                sortedWithPayloadAndContext[key2] = new KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>(value, new KeyValuePair<BytesRef, ISet<BytesRef>>(payload, ctxs));
                unsorted[i] = new Input(key2, value, payload);
                unsortedWithoutPayload[i] = new Input(key2, value);
                unsortedWithContexts[i] = new Input(key2, value, ctxs);
                unsortedWithPayloadAndContext[i] = new Input(key2, value, payload, ctxs);
            }

            // test the sorted iterator wrapper with payloads
            IInputEnumerator wrapper = new SortedInputEnumerator(new InputArrayEnumerator(unsorted), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, BytesRef>>> expected = sorted.GetEnumerator();
            while (expected.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, BytesRef>> entry = expected.Current;

                assertTrue(wrapper.MoveNext());
                assertEquals(entry.Key, wrapper.Current);
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                assertEquals(entry.Value.Value, wrapper.Payload);
            }
            assertFalse(wrapper.MoveNext());

            // test the sorted iterator wrapper with contexts
            wrapper = new SortedInputEnumerator(new InputArrayEnumerator(unsortedWithContexts), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, ISet<BytesRef>>>> actualEntries = sortedWithContext.GetEnumerator();
            while (actualEntries.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, ISet<BytesRef>>> entry = actualEntries.Current;
                assertTrue(wrapper.MoveNext());
                assertEquals(entry.Key, wrapper.Current);
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                ISet<BytesRef> actualCtxs = entry.Value.Value;
                assertEquals(actualCtxs, wrapper.Contexts);
            }
            assertFalse(wrapper.MoveNext());

            // test the sorted iterator wrapper with contexts and payload
            wrapper = new SortedInputEnumerator(new InputArrayEnumerator(unsortedWithPayloadAndContext), comparer);
            IEnumerator<KeyValuePair<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>>> expectedPayloadContextEntries = sortedWithPayloadAndContext.GetEnumerator();
            while (expectedPayloadContextEntries.MoveNext())
            {
                KeyValuePair<BytesRef, KeyValuePair<long, KeyValuePair<BytesRef, ISet<BytesRef>>>> entry = expectedPayloadContextEntries.Current;
                assertTrue(wrapper.MoveNext());
                assertEquals(entry.Key, wrapper.Current);
                assertEquals(Convert.ToInt64(entry.Value.Key), wrapper.Weight);
                ISet<BytesRef> actualCtxs = entry.Value.Value.Value;
                assertEquals(actualCtxs, wrapper.Contexts);
                BytesRef actualPayload = entry.Value.Value.Key;
                assertEquals(actualPayload, wrapper.Payload);
            }
            assertFalse(wrapper.MoveNext());

            // test the unsorted iterator wrapper with payloads
            wrapper = new UnsortedInputEnumerator(new InputArrayEnumerator(unsorted));
            IDictionary<BytesRef, KeyValuePair<long, BytesRef>> actual = new JCG.SortedDictionary<BytesRef, KeyValuePair<long, BytesRef>>();
            while (wrapper.MoveNext())
            {
                long value = wrapper.Weight;
                BytesRef payload = wrapper.Payload;
                actual[BytesRef.DeepCopyOf(wrapper.Current)] = new KeyValuePair<long, BytesRef>(value, BytesRef.DeepCopyOf(payload));
            }
            assertEquals(sorted, actual, aggressive: false);

            // test the sorted iterator wrapper without payloads
            IInputEnumerator wrapperWithoutPayload = new SortedInputEnumerator(new InputArrayEnumerator(unsortedWithoutPayload), comparer);
            IEnumerator<KeyValuePair<BytesRef, long>> expectedWithoutPayload = sortedWithoutPayload.GetEnumerator();
            while (expectedWithoutPayload.MoveNext())
            {
                KeyValuePair<BytesRef, long> entry = expectedWithoutPayload.Current;

                assertTrue(wrapperWithoutPayload.MoveNext());
                assertEquals(entry.Key, wrapperWithoutPayload.Current);
                assertEquals(Convert.ToInt64(entry.Value), wrapperWithoutPayload.Weight);
                assertNull(wrapperWithoutPayload.Payload);
            }
            assertFalse(wrapperWithoutPayload.MoveNext());

            // test the unsorted iterator wrapper without payloads
            wrapperWithoutPayload = new UnsortedInputEnumerator(new InputArrayEnumerator(unsortedWithoutPayload));
            IDictionary<BytesRef, long> actualWithoutPayload = new JCG.SortedDictionary<BytesRef, long>();
            while (wrapperWithoutPayload.MoveNext())
            {
                long value = wrapperWithoutPayload.Weight;
                assertNull(wrapperWithoutPayload.Payload);
                actualWithoutPayload[BytesRef.DeepCopyOf(wrapperWithoutPayload.Current)] = value;
            }
            assertEquals(sortedWithoutPayload, actualWithoutPayload, aggressive: false);
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
