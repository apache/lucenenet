using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

    public class FileDictionaryTest : LuceneTestCase
    {
        private KeyValuePair<IList<string>, string> GenerateFileEntry(string fieldDelimiter, bool hasWeight, bool hasPayload)
        {
            IList<string> entryValues = new JCG.List<string>();
            StringBuilder sb = new StringBuilder();
            string term = TestUtil.RandomSimpleString(Random, 1, 300);
            sb.Append(term);
            entryValues.Add(term);
            if (hasWeight)
            {
                sb.Append(fieldDelimiter);
                long weight = TestUtil.NextInt64(Random, long.MinValue, long.MaxValue);
                // LUCENENET: We need to explicitly use invariant culture here,
                // as that is what is expected in Java
                sb.Append(weight.ToString(CultureInfo.InvariantCulture));
                entryValues.Add(weight.ToString(CultureInfo.InvariantCulture));
            }
            if (hasPayload)
            {
                sb.Append(fieldDelimiter);
                string payload = TestUtil.RandomSimpleString(Random, 1, 300);
                sb.Append(payload);
                entryValues.Add(payload);
            }
            sb.append("\n");
            return new KeyValuePair<IList<string>, string>(entryValues, sb.ToString());
        }

        private KeyValuePair<IList<IList<string>>, string> generateFileInput(int count, string fieldDelimiter, bool hasWeights, bool hasPayloads)
        {
            IList<IList<string>> entries = new JCG.List<IList<string>>();
            StringBuilder sb = new StringBuilder();
            bool hasPayload = hasPayloads;
            for (int i = 0; i < count; i++)
            {
                if (hasPayloads)
                {
                    hasPayload = (i == 0) ? true : Random.nextBoolean();
                }
                KeyValuePair<IList<string>, string> entrySet = GenerateFileEntry(fieldDelimiter, (!hasPayloads && hasWeights) ? Random.nextBoolean() : hasWeights, hasPayload);
                entries.Add(entrySet.Key);
                sb.Append(entrySet.Value);
            }
            return new KeyValuePair<IList<IList<string>>, string>(entries, sb.ToString());
        }

        [Test]
        public void TestFileWithTerm()
        {
            KeyValuePair<IList<IList<string>>, string> fileInput = generateFileInput(AtLeast(100), FileDictionary.DEFAULT_FIELD_DELIMITER, false, false);
            Stream inputReader = new MemoryStream(fileInput.Value.getBytes(Encoding.UTF8));
            FileDictionary dictionary = new FileDictionary(inputReader);
            IList<IList<string>> entries = fileInput.Key;
            IInputEnumerator inputIter = dictionary.GetEntryEnumerator();
            assertFalse(inputIter.HasPayloads);
            int count = 0;
            while (inputIter.MoveNext())
            {
                assertTrue(entries.size() > count);
                IList<string> entry = entries[count];
                assertTrue(entry.size() >= 1); // at least a term
                assertEquals(entry[0], inputIter.Current.Utf8ToString());
                assertEquals(1, inputIter.Weight);
                assertNull(inputIter.Payload);
                count++;
            }
            assertEquals(count, entries.size());
        }

        [Test]
        public void TestFileWithWeight()
        {
            KeyValuePair<IList<IList<string>>, string> fileInput = generateFileInput(AtLeast(100), FileDictionary.DEFAULT_FIELD_DELIMITER, true, false);
            Stream inputReader = new MemoryStream(fileInput.Value.getBytes(Encoding.UTF8));
            FileDictionary dictionary = new FileDictionary(inputReader);
            IList<IList<String>> entries = fileInput.Key;
            IInputEnumerator inputIter = dictionary.GetEntryEnumerator();
            assertFalse(inputIter.HasPayloads);
            int count = 0;
            while (inputIter.MoveNext())
            {
                assertTrue(entries.size() > count);
                IList<String> entry = entries[count];
                assertTrue(entry.size() >= 1); // at least a term
                assertEquals(entry[0], inputIter.Current.Utf8ToString());
                assertEquals((entry.size() == 2) ? long.Parse(entry[1], CultureInfo.InvariantCulture) : 1, inputIter.Weight);
                assertNull(inputIter.Payload);
                count++;
            }
            assertEquals(count, entries.size());
        }

        [Test]
        public void TestFileWithWeightAndPayload()
        {
            KeyValuePair<IList<IList<string>>, string> fileInput = generateFileInput(AtLeast(100), FileDictionary.DEFAULT_FIELD_DELIMITER, true, true);
            Stream inputReader = new MemoryStream(fileInput.Value.getBytes(Encoding.UTF8));
            FileDictionary dictionary = new FileDictionary(inputReader);
            IList<IList<string>> entries = fileInput.Key;
            IInputEnumerator inputIter = dictionary.GetEntryEnumerator();
            assertTrue(inputIter.HasPayloads);
            int count = 0;
            while (inputIter.MoveNext())
            {
                assertTrue(entries.size() > count);
                IList<string> entry = entries[count];
                assertTrue(entry.size() >= 2); // at least term and weight
                assertEquals(entry[0], inputIter.Current.Utf8ToString());
                assertEquals(long.Parse(entry[1], CultureInfo.InvariantCulture), inputIter.Weight);
                if (entry.size() == 3)
                {
                    assertEquals(entry[2], inputIter.Payload.Utf8ToString());
                }
                else
                {
                    assertEquals(inputIter.Payload.Length, 0);
                }
                count++;
            }
            assertEquals(count, entries.size());
        }

        [Test]
        public void TestFileWithOneEntry()
        {
            KeyValuePair<IList<IList<string>>, string> fileInput = generateFileInput(1, FileDictionary.DEFAULT_FIELD_DELIMITER, true, true);
            Stream inputReader = new MemoryStream(fileInput.Value.getBytes(Encoding.UTF8));
            FileDictionary dictionary = new FileDictionary(inputReader);
            IList<IList<string>> entries = fileInput.Key;
            IInputEnumerator inputIter = dictionary.GetEntryEnumerator();
            assertTrue(inputIter.HasPayloads);
            int count = 0;
            while (inputIter.MoveNext())
            {
                assertTrue(entries.size() > count);
                IList<string> entry = entries[count];
                assertTrue(entry.size() >= 2); // at least term and weight
                assertEquals(entry[0], inputIter.Current.Utf8ToString());
                assertEquals(long.Parse(entry[1], CultureInfo.InvariantCulture), inputIter.Weight);
                if (entry.size() == 3)
                {
                    assertEquals(entry[2], inputIter.Payload.Utf8ToString());
                }
                else
                {
                    assertEquals(inputIter.Payload.Length, 0);
                }
                count++;
            }
            assertEquals(count, entries.size());
        }

        [Test]
        public void TestFileWithDifferentDelimiter()
        {
            KeyValuePair<IList<IList<string>>, string> fileInput = generateFileInput(AtLeast(100), " , ", true, true);
            Stream inputReader = new MemoryStream(fileInput.Value.getBytes(Encoding.UTF8));
            FileDictionary dictionary = new FileDictionary(inputReader, " , ");
            IList<IList<string>> entries = fileInput.Key;
            IInputEnumerator inputIter = dictionary.GetEntryEnumerator();
            assertTrue(inputIter.HasPayloads);
            int count = 0;
            while (inputIter.MoveNext())
            {
                assertTrue(entries.size() > count);
                IList<string> entry = entries[count];
                assertTrue(entry.size() >= 2); // at least term and weight
                assertEquals(entry[0], inputIter.Current.Utf8ToString());
                assertEquals(long.Parse(entry[1], CultureInfo.InvariantCulture), inputIter.Weight);
                if (entry.size() == 3)
                {
                    assertEquals(entry[2], inputIter.Payload.Utf8ToString());
                }
                else
                {
                    assertEquals(inputIter.Payload.Length, 0);
                }
                count++;
            }
            assertEquals(count, entries.size());
        }
    }
}
