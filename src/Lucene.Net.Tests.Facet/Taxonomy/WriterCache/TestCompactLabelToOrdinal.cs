// Lucene version compatibility level 4.8.1
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    public class TestCompactLabelToOrdinal : FacetTestCase
    {
        [Test]
        public virtual void TestL2O()
        {
            LabelToOrdinal map = new LabelToOrdinalMap();

            CompactLabelToOrdinal compact = new CompactLabelToOrdinal(2000000, 0.15f, 3);

            int n = AtLeast(10 * 1000);
            const int numUniqueValues = 50 * 1000;

            string[] uniqueValues = new string[numUniqueValues];
            byte[] buffer = new byte[50];

            // This is essentially the equivalent of
            // CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder()
            //     .onUnmappableCharacter(CodingErrorAction.REPLACE)
            //     .onMalformedInput(CodingErrorAction.REPLACE);
            // 
            // Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage, 
            //     new EncoderReplacementFallback("?"), 
            //     new DecoderReplacementFallback("?"));

            Random random = Random;
            for (int i = 0; i < numUniqueValues;)
            {
                random.NextBytes(buffer);
                int size = 1 + random.Next(buffer.Length);

                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage,
                    new EncoderReplacementFallback("?"),
                    new DecoderReplacementFallback("?"));
                uniqueValues[i] = decoder.GetString(buffer, 0, size);
                // we cannot have empty path components, so eliminate all prefix as well
                // as middle consecutive delimiter chars.
                uniqueValues[i] = Regex.Replace(uniqueValues[i], "/+", "/");
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
            var f = new FileInfo(Path.Combine(tmpDir.FullName, "CompactLabelToOrdinalTest.tmp"));
            int flushInterval = 10;

            for (int i = 0; i < n; i++)
            {
                if (i > 0 && i % flushInterval == 0)
                {
                    using (var fileStream = new FileStream(f.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        compact.Flush(fileStream);
                    }
                    compact = CompactLabelToOrdinal.Open(f, 0.15f, 3);
                    //assertTrue(f.Delete());
                    f.Delete();
                    assertFalse(File.Exists(f.FullName));
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
                    label = new FacetLabel(s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                }

                int ord1 = map.GetOrdinal(label);
                int ord2 = compact.GetOrdinal(label);

                if (Verbose)
                {
                    Console.WriteLine("Testing label: " + label.ToString());
                }

                assertEquals(ord1, ord2);

                if (ord1 == LabelToOrdinal.INVALID_ORDINAL)
                {
                    ord1 = compact.GetNextOrdinal();
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
                    label = new FacetLabel(s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                }
                int ord1 = map.GetOrdinal(label);
                int ord2 = compact.GetOrdinal(label);

                if (Verbose)
                {
                    Console.WriteLine("Testing label 2: " + label.ToString());
                }

                assertEquals(ord1, ord2);
            }
        }

        /// <summary>
        /// LUCENENET specific test similar to TestL2O without any randomness, useful for debugging
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestL2OBasic()
        {
            LabelToOrdinal map = new LabelToOrdinalMap();

            CompactLabelToOrdinal compact = new CompactLabelToOrdinal(200, 0.15f, 3);

            int n = 50;

            string[] uniqueValues = new string[]
            {
                //@"�",
                //@"�r�G��F�\u0382�7\u0019�h�\u0015���#\u001d3\r{��q�_���Ԃ������",
                "foo bar one",
                //new string(new char[] { (char)65533, (char)65533, (char)65, (char)65533, (char)45, (char)106, (char)40, (char)643, (char)65533, (char)11, (char)65533, (char)88, (char)65533, (char)78, (char)126, (char)56, (char)12, (char)71 }),
                //"foo bar two",
                //"foo bar three",
                //"foo bar four",
                //"foo bar five",
                //"foo bar six",
                //"foo bar seven",
                //"foo bar eight",
                //"foo bar nine",
                //"foo bar ten",
                //"foo/bar/one",
                //"foo/bar/two",
                //"foo/bar/three",
                //"foo/bar/four",
                //"foo/bar/five",
                //"foo/bar/six",
                //"foo/bar/seven",
                //"foo/bar/eight",
                //"foo/bar/nine",
                //"foo/bar/ten",
                //""
            };

            var tmpDir = CreateTempDir("testLableToOrdinal");
            var f = new FileInfo(Path.Combine(tmpDir.FullName, "CompactLabelToOrdinalTest.tmp"));
            int flushInterval = 10;

            for (int i = 0; i < n; i++)
            {
                if (i > 0 && i % flushInterval == 0)
                {
                    using (var fileStream = new FileStream(f.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        compact.Flush(fileStream);
                    }
                    compact = CompactLabelToOrdinal.Open(f, 0.15f, 3);
                    //assertTrue(f.Delete());
                    f.Delete();
                    assertFalse(File.Exists(f.FullName));
                    if (flushInterval < (n / 10))
                    {
                        flushInterval *= 10;
                    }
                }

                FacetLabel label = new FacetLabel();
                foreach (string s in uniqueValues)
                {
                    if (s.Length == 0)
                    {
                        label = new FacetLabel();
                    }
                    else
                    {
                        label = new FacetLabel(s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    int ord1 = map.GetOrdinal(label);
                    int ord2 = compact.GetOrdinal(label);

                    if (Verbose)
                    {
                        Console.WriteLine("Testing label: " + label.ToString());
                    }

                    assertEquals(ord1, ord2);

                    if (ord1 == LabelToOrdinal.INVALID_ORDINAL)
                    {
                        ord1 = compact.GetNextOrdinal();
                        map.AddLabel(label, ord1);
                        compact.AddLabel(label, ord1);
                    }
                }
            }

            for (int i = 0; i < uniqueValues.Length; i++)
            {
                FacetLabel label;
                string s = uniqueValues[i];
                if (s.Length == 0)
                {
                    label = new FacetLabel();
                }
                else
                {
                    label = new FacetLabel(s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                }
                int ord1 = map.GetOrdinal(label);
                int ord2 = compact.GetOrdinal(label);

                if (Verbose)
                {
                    Console.WriteLine("Testing label 2: " + label.ToString());
                }

                assertEquals(ord1, ord2);
            }
        }

        private class LabelToOrdinalMap : LabelToOrdinal
        {
            internal IDictionary<FacetLabel, int> map = new Dictionary<FacetLabel, int>();

            internal LabelToOrdinalMap()
            {
            }

            public override void AddLabel(FacetLabel label, int ordinal)
            {
                map[label] = ordinal;
            }

            public override int GetOrdinal(FacetLabel label)
            {
                if (map.TryGetValue(label, out int value))
                {
                    return value;
                }
                return LabelToOrdinal.INVALID_ORDINAL;
            }
        } 
    }
}