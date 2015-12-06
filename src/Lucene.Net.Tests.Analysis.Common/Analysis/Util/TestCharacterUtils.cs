using System;

namespace org.apache.lucene.analysis.util
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


	using CharacterBuffer = org.apache.lucene.analysis.util.CharacterUtils.CharacterBuffer;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using Version = org.apache.lucene.util.Version;
	using TestUtil = org.apache.lucene.util.TestUtil;
	using Test = org.junit.Test;

	/// <summary>
	/// TestCase for the <seealso cref="CharacterUtils"/> class.
	/// </summary>
	public class TestCharacterUtils : LuceneTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCodePointAtCharSequenceInt()
	  public virtual void testCodePointAtCharSequenceInt()
	  {
		CharacterUtils java4 = CharacterUtils.getInstance(Version.LUCENE_30);
		string cpAt3 = "Abc\ud801\udc1c";
		string highSurrogateAt3 = "Abc\ud801";
		assertEquals((int) 'A', java4.codePointAt(cpAt3, 0));
		assertEquals((int) '\ud801', java4.codePointAt(cpAt3, 3));
		assertEquals((int) '\ud801', java4.codePointAt(highSurrogateAt3, 3));
		try
		{
		  java4.codePointAt(highSurrogateAt3, 4);
		  fail("string index out of bounds");
		}
		catch (System.IndexOutOfRangeException)
		{
		}

		CharacterUtils java5 = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
		assertEquals((int) 'A', java5.codePointAt(cpAt3, 0));
		assertEquals(char.toCodePoint('\ud801', '\udc1c'), java5.codePointAt(cpAt3, 3));
		assertEquals((int) '\ud801', java5.codePointAt(highSurrogateAt3, 3));
		try
		{
		  java5.codePointAt(highSurrogateAt3, 4);
		  fail("string index out of bounds");
		}
		catch (System.IndexOutOfRangeException)
		{
		}

	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCodePointAtCharArrayIntInt()
	  public virtual void testCodePointAtCharArrayIntInt()
	  {
		CharacterUtils java4 = CharacterUtils.getInstance(Version.LUCENE_30);
		char[] cpAt3 = "Abc\ud801\udc1c".ToCharArray();
		char[] highSurrogateAt3 = "Abc\ud801".ToCharArray();
		assertEquals((int) 'A', java4.codePointAt(cpAt3, 0, 2));
		assertEquals((int) '\ud801', java4.codePointAt(cpAt3, 3, 5));
		assertEquals((int) '\ud801', java4.codePointAt(highSurrogateAt3, 3, 4));

		CharacterUtils java5 = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
		assertEquals((int) 'A', java5.codePointAt(cpAt3, 0, 2));
		assertEquals(char.toCodePoint('\ud801', '\udc1c'), java5.codePointAt(cpAt3, 3, 5));
		assertEquals((int) '\ud801', java5.codePointAt(highSurrogateAt3, 3, 4));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCodePointCount()
	  public virtual void testCodePointCount()
	  {
		CharacterUtils java4 = CharacterUtils.Java4Instance;
		CharacterUtils java5 = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.apache.lucene.util.TestUtil.randomUnicodeString(random());
		string s = TestUtil.randomUnicodeString(random());
		assertEquals(s.Length, java4.codePointCount(s));
		assertEquals(char.codePointCount(s, 0, s.Length), java5.codePointCount(s));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testOffsetByCodePoint()
	  public virtual void testOffsetByCodePoint()
	  {
		CharacterUtils java4 = CharacterUtils.Java4Instance;
		CharacterUtils java5 = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
		for (int i = 0; i < 10; ++i)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] s = org.apache.lucene.util.TestUtil.randomUnicodeString(random()).toCharArray();
		  char[] s = TestUtil.randomUnicodeString(random()).toCharArray();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int index = org.apache.lucene.util.TestUtil.nextInt(random(), 0, s.length);
		  int index = TestUtil.Next(random(), 0, s.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offset = random().nextInt(7) - 3;
		  int offset = random().Next(7) - 3;
		  try
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o = java4.offsetByCodePoints(s, 0, s.length, index, offset);
			int o = java4.offsetByCodePoints(s, 0, s.Length, index, offset);
			assertEquals(o, index + offset);
		  }
		  catch (System.IndexOutOfRangeException)
		  {
			assertTrue((index + offset) < 0 || (index + offset) > s.Length);
		  }

		  int o;
		  try
		  {
			o = java5.offsetByCodePoints(s, 0, s.Length, index, offset);
		  }
		  catch (System.IndexOutOfRangeException)
		  {
			try
			{
			  char.offsetByCodePoints(s, 0, s.Length, index, offset);
			  fail();
			}
			catch (System.IndexOutOfRangeException)
			{
			  // OK
			}
			o = -1;
		  }
		  if (o >= 0)
		  {
			assertEquals(char.offsetByCodePoints(s, 0, s.Length, index, offset), o);
		  }
		}
	  }

	  public virtual void testConversions()
	  {
		CharacterUtils java4 = CharacterUtils.Java4Instance;
		CharacterUtils java5 = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
		testConversions(java4);
		testConversions(java5);
	  }

	  private void testConversions(CharacterUtils charUtils)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] orig = org.apache.lucene.util.TestUtil.randomUnicodeString(random(), 100).toCharArray();
		char[] orig = TestUtil.randomUnicodeString(random(), 100).toCharArray();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] buf = new int[orig.length];
		int[] buf = new int[orig.Length];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] restored = new char[buf.length];
		char[] restored = new char[buf.Length];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o1 = org.apache.lucene.util.TestUtil.nextInt(random(), 0, Math.min(5, orig.length));
		int o1 = TestUtil.Next(random(), 0, Math.Min(5, orig.Length));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o2 = org.apache.lucene.util.TestUtil.nextInt(random(), 0, o1);
		int o2 = TestUtil.Next(random(), 0, o1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o3 = org.apache.lucene.util.TestUtil.nextInt(random(), 0, o1);
		int o3 = TestUtil.Next(random(), 0, o1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePointCount = charUtils.toCodePoints(orig, o1, orig.length - o1, buf, o2);
		int codePointCount = charUtils.toCodePoints(orig, o1, orig.Length - o1, buf, o2);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int charCount = charUtils.toChars(buf, o2, codePointCount, restored, o3);
		int charCount = charUtils.toChars(buf, o2, codePointCount, restored, o3);
		assertEquals(orig.Length - o1, charCount);
		assertArrayEquals(Arrays.copyOfRange(orig, o1, o1 + charCount), Arrays.copyOfRange(restored, o3, o3 + charCount));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testNewCharacterBuffer()
	  public virtual void testNewCharacterBuffer()
	  {
		CharacterBuffer newCharacterBuffer = CharacterUtils.newCharacterBuffer(1024);
		assertEquals(1024, newCharacterBuffer.Buffer.length);
		assertEquals(0, newCharacterBuffer.Offset);
		assertEquals(0, newCharacterBuffer.Length);

		newCharacterBuffer = CharacterUtils.newCharacterBuffer(2);
		assertEquals(2, newCharacterBuffer.Buffer.length);
		assertEquals(0, newCharacterBuffer.Offset);
		assertEquals(0, newCharacterBuffer.Length);

		try
		{
		  newCharacterBuffer = CharacterUtils.newCharacterBuffer(1);
		  fail("length must be >= 2");
		}
		catch (System.ArgumentException)
		{
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFillNoHighSurrogate() throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testFillNoHighSurrogate()
	  {
		Version[] versions = new Version[] {Version.LUCENE_30, TEST_VERSION_CURRENT};
		foreach (Version version in versions)
		{
		  CharacterUtils instance = CharacterUtils.getInstance(version);
		  Reader reader = new StringReader("helloworld");
		  CharacterBuffer buffer = CharacterUtils.newCharacterBuffer(6);
		  assertTrue(instance.fill(buffer,reader));
		  assertEquals(0, buffer.Offset);
		  assertEquals(6, buffer.Length);
		  assertEquals("hellow", new string(buffer.Buffer));
		  assertFalse(instance.fill(buffer,reader));
		  assertEquals(4, buffer.Length);
		  assertEquals(0, buffer.Offset);

		  assertEquals("orld", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		  assertFalse(instance.fill(buffer,reader));
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFillJava15() throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testFillJava15()
	  {
		string input = "1234\ud801\udc1c789123\ud801\ud801\udc1c\ud801";
		CharacterUtils instance = CharacterUtils.getInstance(TEST_VERSION_CURRENT);
		Reader reader = new StringReader(input);
		CharacterBuffer buffer = CharacterUtils.newCharacterBuffer(5);
		assertTrue(instance.fill(buffer, reader));
		assertEquals(4, buffer.Length);
		assertEquals("1234", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		assertTrue(instance.fill(buffer, reader));
		assertEquals(5, buffer.Length);
		assertEquals("\ud801\udc1c789", new string(buffer.Buffer));
		assertTrue(instance.fill(buffer, reader));
		assertEquals(4, buffer.Length);
		assertEquals("123\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		assertFalse(instance.fill(buffer, reader));
		assertEquals(3, buffer.Length);
		assertEquals("\ud801\udc1c\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		assertFalse(instance.fill(buffer, reader));
		assertEquals(0, buffer.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFillJava14() throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testFillJava14()
	  {
		string input = "1234\ud801\udc1c789123\ud801\ud801\udc1c\ud801";
		CharacterUtils instance = CharacterUtils.getInstance(Version.LUCENE_30);
		Reader reader = new StringReader(input);
		CharacterBuffer buffer = CharacterUtils.newCharacterBuffer(5);
		assertTrue(instance.fill(buffer, reader));
		assertEquals(5, buffer.Length);
		assertEquals("1234\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		assertTrue(instance.fill(buffer, reader));
		assertEquals(5, buffer.Length);
		assertEquals("\udc1c7891", new string(buffer.Buffer));
		buffer = CharacterUtils.newCharacterBuffer(6);
		assertTrue(instance.fill(buffer, reader));
		assertEquals(6, buffer.Length);
		assertEquals("23\ud801\ud801\udc1c\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
		assertFalse(instance.fill(buffer, reader));

	  }

	}

}