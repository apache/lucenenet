using J2N.Text;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharsRef = Lucene.Net.Util.CharsRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    [TestFixture]
    public class TestIndexWriterUnicode : LuceneTestCase
    {
        internal readonly string[] utf8Data = new string[] { "ab\udc17cd", "ab\ufffdcd", "\udc17abcd", "\ufffdabcd", "\udc17", "\ufffd", "ab\udc17\udc17cd", "ab\ufffd\ufffdcd", "\udc17\udc17abcd", "\ufffd\ufffdabcd", "\udc17\udc17", "\ufffd\ufffd", "ab\ud917cd", "ab\ufffdcd", "\ud917abcd", "\ufffdabcd", "\ud917", "\ufffd", "ab\ud917\ud917cd", "ab\ufffd\ufffdcd", "\ud917\ud917abcd", "\ufffd\ufffdabcd", "\ud917\ud917", "\ufffd\ufffd", "ab\udc17\ud917cd", "ab\ufffd\ufffdcd", "\udc17\ud917abcd", "\ufffd\ufffdabcd", "\udc17\ud917", "\ufffd\ufffd", "ab\udc17\ud917\udc17\ud917cd", "ab\ufffd\ud917\udc17\ufffdcd", "\udc17\ud917\udc17\ud917abcd", "\ufffd\ud917\udc17\ufffdabcd", "\udc17\ud917\udc17\ud917", "\ufffd\ud917\udc17\ufffd" };

        private int NextInt(int lim)
        {
            return Random.Next(lim);
        }

        private int NextInt(int start, int end)
        {
            return start + NextInt(end - start);
        }

        private bool FillUnicode(char[] buffer, char[] expected, int offset, int count)
        {
            int len = offset + count;
            bool hasIllegal = false;

            if (offset > 0 && buffer[offset] >= 0xdc00 && buffer[offset] < 0xe000)
            // Don't start in the middle of a valid surrogate pair
            {
                offset--;
            }

            for (int i = offset; i < len; i++)
            {
                int t = NextInt(6);
                if (0 == t && i < len - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    expected[i] = buffer[i++] = (char)NextInt(0xd800, 0xdc00);
                    // Low surrogate
                    expected[i] = buffer[i] = (char)NextInt(0xdc00, 0xe000);
                }
                else if (t <= 1)
                {
                    expected[i] = buffer[i] = (char)NextInt(0x80);
                }
                else if (2 == t)
                {
                    expected[i] = buffer[i] = (char)NextInt(0x80, 0x800);
                }
                else if (3 == t)
                {
                    expected[i] = buffer[i] = (char)NextInt(0x800, 0xd800);
                }
                else if (4 == t)
                {
                    expected[i] = buffer[i] = (char)NextInt(0xe000, 0xffff);
                }
                else if (5 == t && i < len - 1)
                {
                    // Illegal unpaired surrogate
                    if (NextInt(10) == 7)
                    {
                        if (Random.NextBoolean())
                        {
                            buffer[i] = (char)NextInt(0xd800, 0xdc00);
                        }
                        else
                        {
                            buffer[i] = (char)NextInt(0xdc00, 0xe000);
                        }
                        expected[i++] = (char)0xfffd;
                        expected[i] = buffer[i] = (char)NextInt(0x800, 0xd800);
                        hasIllegal = true;
                    }
                    else
                    {
                        expected[i] = buffer[i] = (char)NextInt(0x800, 0xd800);
                    }
                }
                else
                {
                    expected[i] = buffer[i] = ' ';
                }
            }

            return hasIllegal;
        }

        // both start & end are inclusive
        private int GetInt(Random r, int start, int end)
        {
            return start + r.Next(1 + end - start);
        }

        private string AsUnicodeChar(char c)
        {
            return "U+" + ((int)c).ToString("x");
        }

        private string TermDesc(string s)
        {
            string s0;
            Assert.IsTrue(s.Length <= 2);
            if (s.Length == 1)
            {
                s0 = AsUnicodeChar(s[0]);
            }
            else
            {
                s0 = AsUnicodeChar(s[0]) + "," + AsUnicodeChar(s[1]);
            }
            return s0;
        }

        private void CheckTermsOrder(IndexReader r, ISet<string> allTerms, bool isTop)
        {
            TermsEnum terms = MultiFields.GetFields(r).GetTerms("f").GetEnumerator();

            BytesRef last = new BytesRef();

            ISet<string> seenTerms = new JCG.HashSet<string>();

            while (terms.MoveNext())
            {
                BytesRef term = terms.Term;

                Assert.IsTrue(last.CompareTo(term) < 0);
                last.CopyBytes(term);

                string s = term.Utf8ToString();
                Assert.IsTrue(allTerms.Contains(s), "term " + TermDesc(s) + " was not added to index (count=" + allTerms.Count + ")");
                seenTerms.Add(s);
            }

            if (isTop)
            {
                Assert.IsTrue(allTerms.SetEquals(seenTerms));
            }

            // Test seeking:
            IEnumerator<string> it = seenTerms.GetEnumerator();
            while (it.MoveNext())
            {
                BytesRef tr = new BytesRef(it.Current);
                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(tr), "seek failed for term=" + TermDesc(tr.Utf8ToString()));
            }
        }

        // LUCENE-510
        [Test]
        public virtual void TestRandomUnicodeStrings()
        {
            char[] buffer = new char[20];
            char[] expected = new char[20];

            BytesRef utf8 = new BytesRef(20);
            CharsRef utf16 = new CharsRef(20);

            int num = AtLeast(100000);
            for (int iter = 0; iter < num; iter++)
            {
                bool hasIllegal = FillUnicode(buffer, expected, 0, 20);

                UnicodeUtil.UTF16toUTF8(buffer, 0, 20, utf8);
                if (!hasIllegal)
                {
#pragma warning disable 612, 618
                    var b = (new string(buffer, 0, 20)).GetBytes(IOUtils.CHARSET_UTF_8);
#pragma warning restore 612, 618
                    Assert.AreEqual(b.Length, utf8.Length);
                    for (int i = 0; i < b.Length; i++)
                    {
                        Assert.AreEqual(b[i], utf8.Bytes[i]);
                    }
                }

                UnicodeUtil.UTF8toUTF16(utf8.Bytes, 0, utf8.Length, utf16);
                Assert.AreEqual(utf16.Length, 20);
                for (int i = 0; i < 20; i++)
                {
                    Assert.AreEqual(expected[i], utf16.Chars[i]);
                }
            }
        }

        // LUCENE-510
        [Test]
        public virtual void TestAllUnicodeChars()
        {
            BytesRef utf8 = new BytesRef(10);
            CharsRef utf16 = new CharsRef(10);
            char[] chars = new char[2];
            for (int ch = 0; ch < 0x0010FFFF; ch++)
            {
                if (ch == 0xd800)
                // Skip invalid code points
                {
                    ch = 0xe000;
                }

                int len = 0;
                if (ch <= 0xffff)
                {
                    chars[len++] = (char)ch;
                }
                else
                {
                    chars[len++] = (char)(((ch - 0x0010000) >> 10) + UnicodeUtil.UNI_SUR_HIGH_START);
                    chars[len++] = (char)(((ch - 0x0010000) & 0x3FFL) + UnicodeUtil.UNI_SUR_LOW_START);
                }

                UnicodeUtil.UTF16toUTF8(chars, 0, len, utf8);

                string s1 = new string(chars, 0, len);
                string s2 = Encoding.UTF8.GetString(utf8.Bytes, utf8.Offset, utf8.Length);
                Assert.AreEqual(s1, s2, "codepoint " + ch);

                UnicodeUtil.UTF8toUTF16(utf8.Bytes, 0, utf8.Length, utf16);
                Assert.AreEqual(s1, new string(utf16.Chars, 0, utf16.Length), "codepoint " + ch);

                var b = s1.GetBytes(Encoding.UTF8);
                Assert.AreEqual(utf8.Length, b.Length);
                for (int j = 0; j < utf8.Length; j++)
                {
                    Assert.AreEqual(utf8.Bytes[j], b[j]);
                }
            }
        }

        [Test]
        public virtual void TestEmbeddedFFFF()
        {
            Directory d = NewDirectory();
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            doc.Add(NewTextField("field", "a a\uffffb", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewTextField("field", "a", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            Assert.AreEqual(1, r.DocFreq(new Term("field", "a\uffffb")));
            r.Dispose();
            w.Dispose();
            d.Dispose();
        }

        // LUCENE-510
        [Test]
        public virtual void TestInvalidUTF16()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new TestIndexWriter.StringSplitAnalyzer()));
            Document doc = new Document();

            int count = utf8Data.Length / 2;
            for (int i = 0; i < count; i++)
            {
                doc.Add(NewTextField("f" + i, utf8Data[2 * i], Field.Store.YES));
            }
            w.AddDocument(doc);
            w.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            Document doc2 = ir.Document(0);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(1, ir.DocFreq(new Term("f" + i, utf8Data[2 * i + 1])), "field " + i + " was not indexed correctly");
                Assert.AreEqual(utf8Data[2 * i + 1], doc2.GetField("f" + i).GetStringValue(), "field " + i + " is incorrect");
            }
            ir.Dispose();
            dir.Dispose();
        }

        // Make sure terms, including ones with surrogate pairs,
        // sort in codepoint sort order by default
        [Test]
        public virtual void TestTermUTF16SortOrder()
        {
            Random rnd = Random;
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(rnd, dir);
            Document d = new Document();
            // Single segment
            Field f = NewStringField("f", "", Field.Store.NO);
            d.Add(f);
            char[] chars = new char[2];
            ISet<string> allTerms = new JCG.HashSet<string>();

            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                string s;
                if (rnd.NextBoolean())
                {
                    // Single char
                    if (rnd.NextBoolean())
                    {
                        // Above surrogates
                        chars[0] = (char)GetInt(rnd, 1 + UnicodeUtil.UNI_SUR_LOW_END, 0xffff);
                    }
                    else
                    {
                        // Below surrogates
                        chars[0] = (char)GetInt(rnd, 0, UnicodeUtil.UNI_SUR_HIGH_START - 1);
                    }
                    s = new string(chars, 0, 1);
                }
                else
                {
                    // Surrogate pair
                    chars[0] = (char)GetInt(rnd, UnicodeUtil.UNI_SUR_HIGH_START, UnicodeUtil.UNI_SUR_HIGH_END);
                    Assert.IsTrue(((int)chars[0]) >= UnicodeUtil.UNI_SUR_HIGH_START && ((int)chars[0]) <= UnicodeUtil.UNI_SUR_HIGH_END);
                    chars[1] = (char)GetInt(rnd, UnicodeUtil.UNI_SUR_LOW_START, UnicodeUtil.UNI_SUR_LOW_END);
                    s = new string(chars, 0, 2);
                }
                allTerms.Add(s);
                f.SetStringValue(s);

                writer.AddDocument(d);

                if ((1 + i) % 42 == 0)
                {
                    writer.Commit();
                }
            }

            IndexReader r = writer.GetReader();

            // Test each sub-segment
            foreach (AtomicReaderContext ctx in r.Leaves)
            {
                CheckTermsOrder(ctx.Reader, allTerms, false);
            }
            CheckTermsOrder(r, allTerms, true);

            // Test multi segment
            r.Dispose();

            writer.ForceMerge(1);

            // Test single segment
            r = writer.GetReader();
            CheckTermsOrder(r, allTerms, true);
            r.Dispose();

            writer.Dispose();
            dir.Dispose();
        }
    }
}