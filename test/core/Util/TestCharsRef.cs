using System.Text;

namespace Lucene.Net.Util
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

	public class TestCharsRef : LuceneTestCase
	{
	  public virtual void TestUTF16InUTF8Order()
	  {
		int numStrings = atLeast(1000);
		BytesRef[] utf8 = new BytesRef[numStrings];
		CharsRef[] utf16 = new CharsRef[numStrings];

		for (int i = 0; i < numStrings; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  utf8[i] = new BytesRef(s);
		  utf16[i] = new CharsRef(s);
		}

		Arrays.sort(utf8);
		Arrays.sort(utf16, CharsRef.UTF16SortedAsUTF8Comparator);

		for (int i = 0; i < numStrings; i++)
		{
		  Assert.AreEqual(utf8[i].utf8ToString(), utf16[i].ToString());
		}
	  }

	  public virtual void TestAppend()
	  {
		CharsRef @ref = new CharsRef();
		StringBuilder builder = new StringBuilder();
		int numStrings = atLeast(10);
		for (int i = 0; i < numStrings; i++)
		{
		  char[] charArray = TestUtil.randomRealisticUnicodeString(random(), 1, 100).ToCharArray();
		  int offset = random().Next(charArray.Length);
		  int length = charArray.Length - offset;
		  builder.Append(charArray, offset, length);
		  @ref.append(charArray, offset, length);
		}

		Assert.AreEqual(builder.ToString(), @ref.ToString());
	  }

	  public virtual void TestCopy()
	  {
		int numIters = atLeast(10);
		for (int i = 0; i < numIters; i++)
		{
		  CharsRef @ref = new CharsRef();
		  char[] charArray = TestUtil.randomRealisticUnicodeString(random(), 1, 100).ToCharArray();
		  int offset = random().Next(charArray.Length);
		  int length = charArray.Length - offset;
		  string str = new string(charArray, offset, length);
		  @ref.copyChars(charArray, offset, length);
		  Assert.AreEqual(str, @ref.ToString());
		}

	  }

	  // LUCENE-3590, AIOOBE if you append to a charsref with offset != 0
	  public virtual void TestAppendChars()
	  {
		char[] chars = new char[] {'a', 'b', 'c', 'd'};
		CharsRef c = new CharsRef(chars, 1, 3); // bcd
		c.append(new char[] {'e'}, 0, 1);
		Assert.AreEqual("bcde", c.ToString());
	  }

	  // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
	  public virtual void TestCopyChars()
	  {
		char[] chars = new char[] {'a', 'b', 'c', 'd'};
		CharsRef c = new CharsRef(chars, 1, 3); // bcd
		char[] otherchars = new char[] {'b', 'c', 'd', 'e'};
		c.copyChars(otherchars, 0, 4);
		Assert.AreEqual("bcde", c.ToString());
	  }

	  // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
	  public virtual void TestCopyCharsRef()
	  {
		char[] chars = new char[] {'a', 'b', 'c', 'd'};
		CharsRef c = new CharsRef(chars, 1, 3); // bcd
		char[] otherchars = new char[] {'b', 'c', 'd', 'e'};
		c.copyChars(new CharsRef(otherchars, 0, 4));
		Assert.AreEqual("bcde", c.ToString());
	  }

	  // LUCENE-3590: fix charsequence to fully obey interface
	  public virtual void TestCharSequenceCharAt()
	  {
		CharsRef c = new CharsRef("abc");

		Assert.AreEqual('b', c[1]);

		try
		{
		  c[-1];
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}

		try
		{
		  c[3];
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}
	  }

	  // LUCENE-3590: fix off-by-one in subsequence, and fully obey interface
	  // LUCENE-4671: fix subSequence
	  public virtual void TestCharSequenceSubSequence()
	  {
		CharSequence[] sequences = new CharSequence[] {new CharsRef("abc"), new CharsRef("0abc".ToCharArray(), 1, 3), new CharsRef("abc0".ToCharArray(), 0, 3), new CharsRef("0abc0".ToCharArray(), 1, 3)};

		foreach (CharSequence c in sequences)
		{
		  DoTestSequence(c);
		}
	  }

	  private void DoTestSequence(CharSequence c)
	  {

		// slice
		Assert.AreEqual("a", c.subSequence(0, 1).ToString());
		// mid subsequence
		Assert.AreEqual("b", c.subSequence(1, 2).ToString());
		// end subsequence
		Assert.AreEqual("bc", c.subSequence(1, 3).ToString());
		// empty subsequence
		Assert.AreEqual("", c.subSequence(0, 0).ToString());

		try
		{
		  c.subSequence(-1, 1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}

		try
		{
		  c.subSequence(0, -1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}

		try
		{
		  c.subSequence(0, 4);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}

		try
		{
		  c.subSequence(2, 1);
		  Assert.Fail();
		}
		catch (System.IndexOutOfRangeException expected)
		{
		  // expected exception
		}
	  }
	}

}