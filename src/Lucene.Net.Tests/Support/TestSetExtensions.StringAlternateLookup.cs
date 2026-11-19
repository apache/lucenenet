using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    public class TestSetExtensions_StringAlternateLookup
    {
        private static IEnumerable<TestCaseData> SetCases()
        {
            foreach (StringComparer? comparer in new StringComparer?[] { null, StringComparer.OrdinalIgnoreCase })
            {
                foreach (TestCaseData testCase in CreateSetCases(comparer))
                    yield return testCase;
            }
        }

        private static IEnumerable<TestCaseData> SetCaseInsensitiveCases()
        {
            return CreateSetCases(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<TestCaseData> CreateSetCases(StringComparer? comparer)
        {
            string comparerName = comparer is null ? "DefaultComparer" : "OrdinalIgnoreCase";

            yield return new TestCaseData((Func<StringComparer?, ISet<string>>)(c => c is null ? new JCG.HashSet<string>() : new JCG.HashSet<string>(c)), comparer)
                .SetName($"JCG_HashSet_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, ISet<string>>)(c => c is null ? new JCG.OrderedHashSet<string>() : new JCG.OrderedHashSet<string>(c)), comparer)
                .SetName($"JCG_OrderedHashSet_{comparerName}");

            yield return new TestCaseData((Func<StringComparer?, ISet<string>>)(c => c is null ? new JCG.SortedSet<string>() : new JCG.SortedSet<string>(c)), comparer)
                .SetName($"JCG_SortedSet_{comparerName}");

#if FEATURE_HASHSET_ALTERNATELOOKUP
            yield return new TestCaseData((Func<StringComparer?, ISet<string>>)(c => c is null ? new HashSet<string>() : new HashSet<string>(c)), comparer)
                .SetName($"SCG_HashSet_{comparerName}");
#endif

            yield return new TestCaseData((Func<StringComparer?, ISet<string>>)(c => c is null ? new SortedSet<string>() : new SortedSet<string>(c)), comparer)
                .SetName($"SCG_SortedSet_{comparerName}");
        }

        [TestCaseSource(nameof(SetCases))]
        public void TestSetSpanOperations(Func<StringComparer, ISet<string>> createSet, StringComparer comparer)
        {
            ISet<string> set = createSet(comparer);

            ReadOnlySpan<char> alpha = "xxalphaYY".AsSpan(2, 5);
            ReadOnlySpan<char> beta = "xxbetaYY".AsSpan(2, 4);
            ReadOnlySpan<char> missing = "xxmissingYY".AsSpan(2, 7);

            Assert.That(set.Contains(alpha), Is.False);

            Assert.That(set.Add(alpha), Is.True);
            Assert.That(set.Contains(alpha), Is.True);
            Assert.That(set.Count, Is.EqualTo(1));

            Assert.That(set.Add(alpha), Is.False);
            Assert.That(set.Count, Is.EqualTo(1));

            Assert.That(set.TryGetValue(alpha, out string? actualValue), Is.True);
            Assert.That(actualValue, Is.EqualTo("alpha"));

            Assert.That(set.TryGetValue(missing, out actualValue), Is.False);
            Assert.That(actualValue, Is.Null);

            Assert.That(set.Add(beta), Is.True);
            Assert.That(set.Count, Is.EqualTo(2));

            Assert.That(set.Remove(missing), Is.False);

            Assert.That(set.Remove(alpha), Is.True);
            Assert.That(set.Contains(alpha), Is.False);
            Assert.That(set.Count, Is.EqualTo(1));

            Assert.That(set.Remove(alpha), Is.False);

            Assert.That(set.Contains(beta), Is.True);
            Assert.That(set.TryGetValue(beta, out actualValue), Is.True);
            Assert.That(actualValue, Is.EqualTo("beta"));
        }

        [TestCaseSource(nameof(SetCaseInsensitiveCases))]
        public void TestSetSpanOperationsWithCaseInsensitiveValues(Func<StringComparer, ISet<string>> createSet, StringComparer comparer)
        {
            ISet<string> set = createSet(comparer);

            set.Add("Alpha");

            Assert.That(set.Contains("alpha".AsSpan()), Is.True);

            Assert.That(set.TryGetValue("alpha".AsSpan(), out string? actualValue), Is.True);
            Assert.That(comparer.Equals(actualValue, "Alpha"), Is.True);

            Assert.That(set.Add("alpha".AsSpan()), Is.False);
            Assert.That(set.Count, Is.EqualTo(1));

            Assert.That(set.Remove("alpha".AsSpan()), Is.True);
            Assert.That(set.Count, Is.EqualTo(0));
        }
    }
}
