using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
{


    using TestUtil = Lucene.Net.Util.TestUtil;

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
    public class TestCompactLabelToOrdinal : FacetTestCase
    {
        /* not finished to porting yet because of missing decoder implementation */
        /*
        public virtual void TestL2O()
        {
            LabelToOrdinal map = new LabelToOrdinalMap();

            CompactLabelToOrdinal compact = new CompactLabelToOrdinal(2000000, 0.15f, 3);

            int n = AtLeast(10 * 1000);
            const int numUniqueValues = 50 * 1000;

            string[] uniqueValues = new string[numUniqueValues];
            byte[] buffer = new byte[50];

            Random random = Random();
            for (int i = 0; i < numUniqueValues; )
            {
                random.NextBytes(buffer);
                int size = 1 + random.Next(buffer.Length);

                // This test is turning random bytes into a string,
                // this is asking for trouble.
                CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onUnmappableCharacter(CodingErrorAction.REPLACE).onMalformedInput(CodingErrorAction.REPLACE);
                uniqueValues[i] = decoder.decode(ByteBuffer.Wrap(buffer, 0, size)).ToString();
                // we cannot have empty path components, so eliminate all prefix as well
                // as middle consecutive delimiter chars.
                uniqueValues[i] = uniqueValues[i].replaceAll("/+", "/");
                if (uniqueValues[i].StartsWith("/", StringComparison.Ordinal))
                {
                    uniqueValues[i] = uniqueValues[i].Substring(1);
                }
                if (uniqueValues[i].IndexOf(CompactLabelToOrdinal.TERMINATOR_CHAR) == -1)
                {
                    i++;
                }
            }

            var tmpDir = CreateTempDir("testLableToOrdinal");
            var f = new File(tmpDir, "CompactLabelToOrdinalTest.tmp");
            int flushInterval = 10;

            for (int i = 0; i < n; i++)
            {
                if (i > 0 && i % flushInterval == 0)
                {
                    compact.Flush(f);
                    compact = CompactLabelToOrdinal.open(f, 0.15f, 3);
                    Assert.True(f.delete());
                    if (flushInterval < (n / 10))
                    {
                        flushInterval *= 10;
                    }
                }

                int index = random.Next(numUniqueValues);
                FacetLabel label;
                string s = uniqueValues[index];
                if (s.Length == 0)
                {
                    label = new FacetLabel();
                }
                else
                {
                    label = new FacetLabel(s.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                }

                int ord1 = map.GetOrdinal(label);
                int ord2 = compact.GetOrdinal(label);

                Assert.AreEqual(ord1, ord2);

                if (ord1 == LabelToOrdinal.INVALID_ORDINAL)
                {
                    ord1 = compact.NextOrdinal;
                    map.AddLabel(label, ord1);
                    compact.AddLabel(label, ord1);
                }
            }

            for (int i = 0; i < numUniqueValues; i++)
            {
                FacetLabel label;
                string s = uniqueValues[i];
                if (s.Length == 0)
                {
                    label = new FacetLabel();
                }
                else
                {
                    label = new FacetLabel(s.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                }
                int ord1 = map.GetOrdinal(label);
                int ord2 = compact.GetOrdinal(label);
                Assert.AreEqual(ord1, ord2);
            }
        }

        private class LabelToOrdinalMap : LabelToOrdinal
        {
            internal IDictionary<FacetLabel, int?> map = new Dictionary<FacetLabel, int?>();

            internal LabelToOrdinalMap()
            {
            }

            public override void AddLabel(FacetLabel label, int ordinal)
            {
                map[label] = ordinal;
            }

            public override int GetOrdinal(FacetLabel label)
            {
                int? value = map[label];
                return (value != null) ? (int)value : LabelToOrdinal.INVALID_ORDINAL;
            }

        } */

    }

}