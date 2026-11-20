using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JCG = J2N.Collections.Generic;
#nullable enable

namespace Lucene.Net.Support
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

    [TestFixture]
    [LuceneNetSpecific]
    public class TestDictionaryExtensions_StringAlternateLookup
    {
        private static IEnumerable<TestCaseData> DictionaryCases()
        {
            foreach (StringComparer? comparer in new StringComparer?[] { null, StringComparer.OrdinalIgnoreCase })
            {
                foreach (TestCaseData testCase in CreateDictionaryCases(comparer))
                    yield return testCase;
            }
        }

        private static IEnumerable<TestCaseData> DictionaryCaseInsensitiveCases()
        {
            return CreateDictionaryCases(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<TestCaseData> CreateDictionaryCases(StringComparer? comparer)
        {
            string comparerName = comparer is null ? "DefaultComparer" : "OrdinalIgnoreCase";

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new JCG.Dictionary<string, int>() : new JCG.Dictionary<string, int>(c)), comparer)
                .SetName($"JCG_Dictionary_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new JCG.OrderedDictionary<string, int>() : new JCG.OrderedDictionary<string, int>(c)), comparer)
                .SetName($"JCG_OrderedDictionary_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new JCG.SortedDictionary<string, int>() : new JCG.SortedDictionary<string, int>(c)), comparer)
                .SetName($"JCG_SortedDictionary_{comparerName}");

#if FEATURE_DICTIONARY_ALTERNATELOOKUP
            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new Dictionary<string, int>() : new Dictionary<string, int>(c)), comparer)
                .SetName($"SCG_Dictionary_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new ConcurrentDictionary<string, int>() : new ConcurrentDictionary<string, int>(c)), comparer)
                .SetName($"SCG_ConcurrentDictionary_{comparerName}");
#endif

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new SortedDictionary<string, int>() : new SortedDictionary<string, int>(c)), comparer)
                .SetName($"SCG_SortedDictionary_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, IDictionary<string, int>>)(c => c is null ? new SortedList<string, int>() : new SortedList<string, int>(c)), comparer)
                .SetName($"SCG_SortedList_{comparerName}");
        }

        [TestCaseSource(nameof(DictionaryCases))]
        public void TestDictionarySpanOperations(Func<StringComparer, IDictionary<string, int>> createDictionary, StringComparer comparer)
        {
            IDictionary<string, int> dictionary = createDictionary(comparer);

            ReadOnlySpan<char> alpha = "xxalphaYY".AsSpan(2, 5);
            ReadOnlySpan<char> beta = "xxbetaYY".AsSpan(2, 4);
            ReadOnlySpan<char> missing = "xxmissingYY".AsSpan(2, 7);

            Assert.That(dictionary.ContainsKey(alpha), Is.False);

            Assert.That(dictionary.TryGetValue(alpha, out int value), Is.False);
            Assert.That(value, Is.EqualTo(default(int)));

            Assert.That(dictionary.TryGetValue(alpha, out string? actualKey, out value), Is.False);
            Assert.That(value, Is.EqualTo(default(int)));

            Assert.That(dictionary.TryAdd(alpha, 10), Is.True);
            Assert.That(dictionary.ContainsKey(alpha), Is.True);
            Assert.That(dictionary.TryGetValue(alpha, out value), Is.True);
            Assert.That(value, Is.EqualTo(10));

            Assert.That(dictionary.TryAdd(alpha, 20), Is.False);
            Assert.That(dictionary.TryGetValue(alpha, out value), Is.True);
            Assert.That(value, Is.EqualTo(10));

            Assert.That(dictionary.TryGetValue(alpha, out actualKey, out value), Is.True);
            Assert.That(actualKey, Is.EqualTo("alpha"));
            Assert.That(value, Is.EqualTo(10));

            Assert.That(dictionary.Put(alpha, 30), Is.EqualTo(10));
            Assert.That(dictionary.TryGetValue(alpha, out value), Is.True);
            Assert.That(value, Is.EqualTo(30));

            Assert.That(dictionary.Put(beta, 40), Is.EqualTo(default(int)));
            Assert.That(dictionary.TryGetValue(beta, out value), Is.True);
            Assert.That(value, Is.EqualTo(40));

            Assert.That(dictionary[alpha], Is.EqualTo(30));
            dictionary[alpha] = 50;
            Assert.That(dictionary[alpha], Is.EqualTo(50));

            Assert.That(dictionary.Remove(missing), Is.False);

            Assert.That(dictionary.Remove(beta), Is.True);
            Assert.That(dictionary.ContainsKey(beta), Is.False);

            Assert.That(dictionary.Remove(alpha, out actualKey, out value), Is.True);
            Assert.That(actualKey, Is.EqualTo("alpha"));
            Assert.That(value, Is.EqualTo(50));
            Assert.That(dictionary.ContainsKey(alpha), Is.False);

            Assert.That(dictionary.Remove(alpha), Is.False);
        }

        [TestCaseSource(nameof(DictionaryCaseInsensitiveCases))]
        public void TestDictionarySpanOperationsWithCaseInsensitiveKeys(Func<StringComparer, IDictionary<string, int>> createDictionary, StringComparer comparer)
        {
            IDictionary<string, int> dictionary = createDictionary(comparer);

            dictionary.Add("Alpha", 10);

            ReadOnlySpan<char> lowerCaseKey = "alpha".AsSpan();

            Assert.That(dictionary.ContainsKey(lowerCaseKey), Is.True);

            Assert.That(dictionary.TryGetValue(lowerCaseKey, out int value), Is.True);
            Assert.That(value, Is.EqualTo(10));

            Assert.That(dictionary.TryGetValue(lowerCaseKey, out string? actualKey, out value), Is.True);
            Assert.That(comparer.Equals(actualKey, "Alpha"), Is.True);
            Assert.That(value, Is.EqualTo(10));

            Assert.That(dictionary.Put(lowerCaseKey, 20), Is.EqualTo(10));
            Assert.That(dictionary.Count, Is.EqualTo(1));
            Assert.That(dictionary.TryGetValue("Alpha".AsSpan(), out value), Is.True);
            Assert.That(value, Is.EqualTo(20));

            Assert.That(dictionary.Remove(lowerCaseKey, out actualKey, out value), Is.True);
            Assert.That(comparer.Equals(actualKey, "Alpha"), Is.True);
            Assert.That(value, Is.EqualTo(20));
        }

        [Test]
        public void TestDictionarySpanIndexerMissingKeyThrows()
        {
            IDictionary<string, int> dictionary = new JCG.Dictionary<string, int>();

            Assert.Throws<KeyNotFoundException>(() => GetDictionaryValue(dictionary, "missing".AsSpan()));
        }

        private static int GetDictionaryValue(IDictionary<string, int> dictionary, ReadOnlySpan<char> key)
        {
            return dictionary[key];
        }
    }
}
