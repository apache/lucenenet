using System;
using System.Collections.Generic;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using CharsRef = Lucene.Net.Util.CharsRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

	public class TestIndexWriterUnicode : LuceneTestCase
	{

	  internal readonly string[] Utf8Data = new string[] {"ab\udc17cd", "ab\ufffdcd", "\udc17abcd", "\ufffdabcd", "\udc17", "\ufffd", "ab\udc17\udc17cd", "ab\ufffd\ufffdcd", "\udc17\udc17abcd", "\ufffd\ufffdabcd", "\udc17\udc17", "\ufffd\ufffd", "ab\ud917cd", "ab\ufffdcd", "\ud917abcd", "\ufffdabcd", "\ud917", "\ufffd", "ab\ud917\ud917cd", "ab\ufffd\ufffdcd", "\ud917\ud917abcd", "\ufffd\ufffdabcd", "\ud917\ud917", "\ufffd\ufffd", "ab\udc17\ud917cd", "ab\ufffd\ufffdcd", "\udc17\ud917abcd", "\ufffd\ufffdabcd", "\udc17\ud917", "\ufffd\ufffd", "ab\udc17\ud917\udc17\ud917cd", "ab\ufffd\ud917\udc17\ufffdcd", "\udc17\ud917\udc17\ud917abcd", "\ufffd\ud917\udc17\ufffdabcd", "\udc17\ud917\udc17\ud917", "\ufffd\ud917\udc17\ufffd"};

	  private int NextInt(int lim)
	  {
		return random().Next(lim);
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

		for (int i = offset;i < len;i++)
		{
		  int t = NextInt(6);
		  if (0 == t && i < len - 1)
		  {
			// Make a surrogate pair
			// High surrogate
			expected[i] = buffer[i++] = (char) NextInt(0xd800, 0xdc00);
			// Low surrogate
			expected[i] = buffer[i] = (char) NextInt(0xdc00, 0xe000);
		  }
		  else if (t <= 1)
		  {
			expected[i] = buffer[i] = (char) NextInt(0x80);
		  }
		  else if (2 == t)
		  {
			expected[i] = buffer[i] = (char) NextInt(0x80, 0x800);
		  }
		  else if (3 == t)
		  {
			expected[i] = buffer[i] = (char) NextInt(0x800, 0xd800);
		  }
		  else if (4 == t)
		  {
			expected[i] = buffer[i] = (char) NextInt(0xe000, 0xffff);
		  }
		  else if (5 == t && i < len - 1)
		  {
			// Illegal unpaired surrogate
			if (NextInt(10) == 7)
			{
			  if (random().nextBoolean())
			  {
				buffer[i] = (char) NextInt(0xd800, 0xdc00);
			  }
			  else
			  {
				buffer[i] = (char) NextInt(0xdc00, 0xe000);
			  }
			  expected[i++] = (char)0xfffd;
			  expected[i] = buffer[i] = (char) NextInt(0x800, 0xd800);
			  hasIllegal = true;
			}
			else
			{
			  expected[i] = buffer[i] = (char) NextInt(0x800, 0xd800);
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
		return "U+" + c.ToString("x");
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

	  private void CheckTermsOrder(IndexReader r, Set<string> allTerms, bool isTop)
	  {
		TermsEnum terms = MultiFields.getFields(r).terms("f").iterator(null);

		BytesRef last = new BytesRef();

		Set<string> seenTerms = new HashSet<string>();

		while (true)
		{
		  BytesRef term = terms.next();
		  if (term == null)
		  {
			break;
		  }

		  Assert.IsTrue(last.compareTo(term) < 0);
		  last.copyBytes(term);

		  string s = term.utf8ToString();
		  Assert.IsTrue("term " + TermDesc(s) + " was not added to index (count=" + allTerms.size() + ")", allTerms.contains(s));
		  seenTerms.add(s);
		}

		if (isTop)
		{
		  Assert.IsTrue(allTerms.Equals(seenTerms));
		}

		// Test seeking:
		IEnumerator<string> it = seenTerms.GetEnumerator();
		while (it.MoveNext())
		{
		  BytesRef tr = new BytesRef(it.Current);
		  Assert.AreEqual("seek failed for term=" + TermDesc(tr.utf8ToString()), TermsEnum.SeekStatus.FOUND, terms.seekCeil(tr));
		}
	  }

	  // LUCENE-510
	  public virtual void TestRandomUnicodeStrings()
	  {
		char[] buffer = new char[20];
		char[] expected = new char[20];

		BytesRef utf8 = new BytesRef(20);
		CharsRef utf16 = new CharsRef(20);

		int num = atLeast(100000);
		for (int iter = 0; iter < num; iter++)
		{
		  bool hasIllegal = FillUnicode(buffer, expected, 0, 20);

		  UnicodeUtil.UTF16toUTF8(buffer, 0, 20, utf8);
		  if (!hasIllegal)
		  {
			sbyte[] b = (new string(buffer, 0, 20)).getBytes(StandardCharsets.UTF_8);
			Assert.AreEqual(b.Length, utf8.length);
			for (int i = 0;i < b.Length;i++)
			{
			  Assert.AreEqual(b[i], utf8.bytes[i]);
			}
		  }

		  UnicodeUtil.UTF8toUTF16(utf8.bytes, 0, utf8.length, utf16);
		  Assert.AreEqual(utf16.length, 20);
		  for (int i = 0;i < 20;i++)
		  {
			Assert.AreEqual(expected[i], utf16.chars[i]);
		  }
		}
	  }

	  // LUCENE-510
	  public virtual void TestAllUnicodeChars()
	  {

		BytesRef utf8 = new BytesRef(10);
		CharsRef utf16 = new CharsRef(10);
		char[] chars = new char[2];
		for (int ch = 0;ch < 0x0010FFFF;ch++)
		{

		  if (ch == 0xd800)
			// Skip invalid code points
		  {
			ch = 0xe000;
		  }

		  int len = 0;
		  if (ch <= 0xffff)
		  {
			chars[len++] = (char) ch;
		  }
		  else
		  {
			chars[len++] = (char)(((ch - 0x0010000) >> 10) + UnicodeUtil.UNI_SUR_HIGH_START);
			chars[len++] = (char)(((ch - 0x0010000) & 0x3FFL) + UnicodeUtil.UNI_SUR_LOW_START);
		  }

		  UnicodeUtil.UTF16toUTF8(chars, 0, len, utf8);

		  string s1 = new string(chars, 0, len);
		  string s2 = new string(utf8.bytes, 0, utf8.length, StandardCharsets.UTF_8);
		  Assert.AreEqual("codepoint " + ch, s1, s2);

		  UnicodeUtil.UTF8toUTF16(utf8.bytes, 0, utf8.length, utf16);
		  Assert.AreEqual("codepoint " + ch, s1, new string(utf16.chars, 0, utf16.length));

		  sbyte[] b = s1.getBytes(StandardCharsets.UTF_8);
		  Assert.AreEqual(utf8.length, b.Length);
		  for (int j = 0;j < utf8.length;j++)
		  {
			Assert.AreEqual(utf8.bytes[j], b[j]);
		  }
		}
	  }

	  public virtual void TestEmbeddedFFFF()
	  {
		Directory d = newDirectory();
		IndexWriter w = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("field", "a a\uffffb", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newTextField("field", "a", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		Assert.AreEqual(1, r.docFreq(new Term("field", "a\uffffb")));
		r.close();
		w.close();
		d.close();
	  }

	  // LUCENE-510
	  public virtual void TestInvalidUTF16()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new TestIndexWriter.StringSplitAnalyzer()));
		Document doc = new Document();

		int count = Utf8Data.Length / 2;
		for (int i = 0;i < count;i++)
		{
		  doc.add(newTextField("f" + i, Utf8Data[2 * i], Field.Store.YES));
		}
		w.addDocument(doc);
		w.close();

		IndexReader ir = DirectoryReader.open(dir);
		Document doc2 = ir.document(0);
		for (int i = 0;i < count;i++)
		{
		  Assert.AreEqual("field " + i + " was not indexed correctly", 1, ir.docFreq(new Term("f" + i, Utf8Data[2 * i + 1])));
		  Assert.AreEqual("field " + i + " is incorrect", Utf8Data[2 * i + 1], doc2.getField("f" + i).stringValue());
		}
		ir.close();
		dir.close();
	  }

	  // Make sure terms, including ones with surrogate pairs,
	  // sort in codepoint sort order by default
	  public virtual void TestTermUTF16SortOrder()
	  {
		Random rnd = random();
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(rnd, dir);
		Document d = new Document();
		// Single segment
		Field f = newStringField("f", "", Field.Store.NO);
		d.add(f);
		char[] chars = new char[2];
		Set<string> allTerms = new HashSet<string>();

		int num = atLeast(200);
		for (int i = 0; i < num; i++)
		{

		  string s;
		  if (rnd.nextBoolean())
		  {
			// Single char
			if (rnd.nextBoolean())
			{
			  // Above surrogates
			  chars[0] = (char) GetInt(rnd, 1 + UnicodeUtil.UNI_SUR_LOW_END, 0xffff);
			}
			else
			{
			  // Below surrogates
			  chars[0] = (char) GetInt(rnd, 0, UnicodeUtil.UNI_SUR_HIGH_START - 1);
			}
			s = new string(chars, 0, 1);
		  }
		  else
		  {
			// Surrogate pair
			chars[0] = (char) GetInt(rnd, UnicodeUtil.UNI_SUR_HIGH_START, UnicodeUtil.UNI_SUR_HIGH_END);
			Assert.IsTrue(((int) chars[0]) >= UnicodeUtil.UNI_SUR_HIGH_START && ((int) chars[0]) <= UnicodeUtil.UNI_SUR_HIGH_END);
			chars[1] = (char) GetInt(rnd, UnicodeUtil.UNI_SUR_LOW_START, UnicodeUtil.UNI_SUR_LOW_END);
			s = new string(chars, 0, 2);
		  }
		  allTerms.add(s);
		  f.StringValue = s;

		  writer.addDocument(d);

		  if ((1 + i) % 42 == 0)
		  {
			writer.commit();
		  }
		}

		IndexReader r = writer.Reader;

		// Test each sub-segment
		foreach (AtomicReaderContext ctx in r.leaves())
		{
		  CheckTermsOrder(ctx.reader(), allTerms, false);
		}
		CheckTermsOrder(r, allTerms, true);

		// Test multi segment
		r.close();

		writer.forceMerge(1);

		// Test single segment
		r = writer.Reader;
		CheckTermsOrder(r, allTerms, true);
		r.close();

		writer.close();
		dir.close();
	  }
	}

}