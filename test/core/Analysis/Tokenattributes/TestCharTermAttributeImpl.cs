using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
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

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using TestUtil = Lucene.Net.Util.TestUtil;
    using NUnit.Framework;

    [TestFixture]
	public class TestCharTermAttributeImpl : LuceneTestCase
	{
      [Test]
	  public virtual void TestResize()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		char[] content = "hello".ToCharArray();
		t.CopyBuffer(content, 0, content.Length);
		for (int i = 0; i < 2000; i++)
		{
		  t.ResizeBuffer(i);
		  Assert.IsTrue(i <= t.Buffer().Length);
		  Assert.AreEqual("hello", t.ToString());
		}
	  }

      [Test]
      public virtual void TestGrow()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		StringBuilder buf = new StringBuilder("ab");
		for (int i = 0; i < 20; i++)
		{
		  char[] content = buf.ToString().ToCharArray();
		  t.CopyBuffer(content, 0, content.Length);
		  Assert.AreEqual(buf.Length, t.Length);
		  Assert.AreEqual(buf.ToString(), t.ToString());
		  buf.Append(buf.ToString());
		}
		Assert.AreEqual(1048576, t.Length);

		// now as a StringBuilder, first variant
		t = new CharTermAttributeImpl();
		buf = new StringBuilder("ab");
		for (int i = 0; i < 20; i++)
		{
		  t.SetEmpty().Append(buf);
		  Assert.AreEqual(buf.Length, t.Length);
		  Assert.AreEqual(buf.ToString(), t.ToString());
		  buf.Append(t);
		}
		Assert.AreEqual(1048576, t.Length);

		// Test for slow growth to a long term
		t = new CharTermAttributeImpl();
		buf = new StringBuilder("a");
		for (int i = 0; i < 20000; i++)
		{
		  t.SetEmpty().Append(buf);
		  Assert.AreEqual(buf.Length, t.Length);
		  Assert.AreEqual(buf.ToString(), t.ToString());
		  buf.Append("a");
		}
		Assert.AreEqual(20000, t.Length);
	  }

      [Test]
      public virtual void TestToString()
	  {
		char[] b = new char[] {'a', 'l', 'o', 'h', 'a'};
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.CopyBuffer(b, 0, 5);
		Assert.AreEqual("aloha", t.ToString());

		t.SetEmpty().Append("hi there");
		Assert.AreEqual("hi there", t.ToString());
	  }

      [Test]
      public virtual void TestClone()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		char[] content = "hello".ToCharArray();
		t.CopyBuffer(content, 0, 5);
		char[] buf = t.Buffer();
		CharTermAttributeImpl copy = TestToken.AssertCloneIsEqual(t);
		Assert.AreEqual(t.ToString(), copy.ToString());
		Assert.AreNotSame(buf, copy.Buffer());
	  }

      [Test]
      public virtual void TestEquals()
	  {
		CharTermAttributeImpl t1a = new CharTermAttributeImpl();
		char[] content1a = "hello".ToCharArray();
		t1a.CopyBuffer(content1a, 0, 5);
		CharTermAttributeImpl t1b = new CharTermAttributeImpl();
		char[] content1b = "hello".ToCharArray();
		t1b.CopyBuffer(content1b, 0, 5);
		CharTermAttributeImpl t2 = new CharTermAttributeImpl();
		char[] content2 = "hello2".ToCharArray();
		t2.CopyBuffer(content2, 0, 6);
		Assert.IsTrue(t1a.Equals(t1b));
		Assert.IsFalse(t1a.Equals(t2));
		Assert.IsFalse(t2.Equals(t1b));
	  }

      [Test]
      public virtual void TestCopyTo()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		CharTermAttributeImpl copy = TestToken.AssertCopyIsEqual(t);
		Assert.AreEqual("", t.ToString());
		Assert.AreEqual("", copy.ToString());

		t = new CharTermAttributeImpl();
		char[] content = "hello".ToCharArray();
		t.CopyBuffer(content, 0, 5);
		char[] buf = t.Buffer();
		copy = TestToken.AssertCopyIsEqual(t);
		Assert.AreEqual(t.ToString(), copy.ToString());
		Assert.AreNotSame(buf, copy.Buffer());
	  }

      [Test]
	  public virtual void TestAttributeReflection()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.Append("foobar");
		TestUtil.assertAttributeReflection(t, new Dictionary<string, object>() {{put(typeof(CharTermAttribute).Name + "#term", "foobar"); put(typeof(TermToBytesRefAttribute).Name + "#bytes", new BytesRef("foobar"));}});
	  }

      [Test]
      public virtual void TestCharSequenceInterface()
	  {
		const string s = "0123456789";
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.Append(s);

		Assert.AreEqual(s.Length, t.Length);
		Assert.AreEqual("12", t.subSequence(1,3).ToString());
		Assert.AreEqual(s, t.subSequence(0,s.Length).ToString());

		Assert.IsTrue(Pattern.matches("01\\d+", t));
		Assert.IsTrue(Pattern.matches("34", t.subSequence(3,5)));

		Assert.AreEqual(s.subSequence(3,7).ToString(), t.subSequence(3,7).ToString());

		for (int i = 0; i < s.Length; i++)
		{
		  Assert.IsTrue(t[i] == s[i]);
		}
	  }

      [Test]
      public virtual void TestAppendableInterface()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		Formatter formatter = new Formatter(t, Locale.ROOT);
		formatter.format("%d", 1234);
		Assert.AreEqual("1234", t.ToString());
		formatter.format("%d", 5678);
		Assert.AreEqual("12345678", t.ToString());
		t.Append('9');
		Assert.AreEqual("123456789", t.ToString());
		t.Append((CharSequence) "0");
		Assert.AreEqual("1234567890", t.ToString());
		t.Append((CharSequence) "0123456789", 1, 3);
		Assert.AreEqual("123456789012", t.ToString());
		t.Append((CharSequence) CharBuffer.wrap("0123456789".ToCharArray()), 3, 5);
		Assert.AreEqual("12345678901234", t.ToString());
		t.Append((CharSequence) t);
		Assert.AreEqual("1234567890123412345678901234", t.ToString());
		t.Append((CharSequence) new StringBuilder("0123456789"), 5, 7);
		Assert.AreEqual("123456789012341234567890123456", t.ToString());
		t.Append((CharSequence) new StringBuilder(t));
		Assert.AreEqual("123456789012341234567890123456123456789012341234567890123456", t.ToString());
		// very wierd, to test if a subSlice is wrapped correct :)
		CharBuffer buf = CharBuffer.wrap("0123456789".ToCharArray(), 3, 5);
		Assert.AreEqual("34567", buf.ToString());
		t.SetEmpty().append((CharSequence) buf, 1, 2);
		Assert.AreEqual("4", t.ToString());
		CharTermAttribute t2 = new CharTermAttributeImpl();
		t2.append("test");
		t.Append((CharSequence) t2);
		Assert.AreEqual("4test", t.ToString());
		t.Append((CharSequence) t2, 1, 2);
		Assert.AreEqual("4teste", t.ToString());

		try
		{
		  t.Append((CharSequence) t2, 1, 5);
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}

		try
		{
		  t.Append((CharSequence) t2, 1, 0);
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}

		t.Append((CharSequence) null);
		Assert.AreEqual("4testenull", t.ToString());
	  }

      [Test]
      public virtual void TestAppendableInterfaceWithLongSequences()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.Append((CharSequence) "01234567890123456789012345678901234567890123456789");
		t.Append((CharSequence) CharBuffer.wrap("01234567890123456789012345678901234567890123456789".ToCharArray()), 3, 50);
		Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
		t.SetEmpty().Append((CharSequence) new StringBuilder("01234567890123456789"), 5, 17);
		Assert.AreEqual((CharSequence) "567890123456", t.ToString());
		t.Append(new StringBuilder(t));
		Assert.AreEqual((CharSequence) "567890123456567890123456", t.ToString());
		// very wierd, to test if a subSlice is wrapped correct :)
		CharBuffer buf = CharBuffer.wrap("012345678901234567890123456789".ToCharArray(), 3, 15);
		Assert.AreEqual("345678901234567", buf.ToString());
		t.SetEmpty().Append(buf, 1, 14);
		Assert.AreEqual("4567890123456", t.ToString());

		// finally use a completely custom CharSequence that is not catched by instanceof checks
		const string longTestString = "012345678901234567890123456789";
		t.Append(new CharSequenceAnonymousInnerClassHelper(this, longTestString));
		Assert.AreEqual("4567890123456" + longTestString, t.ToString());
	  }

	  private class CharSequenceAnonymousInnerClassHelper : CharSequence
	  {
		  private readonly TestCharTermAttributeImpl OuterInstance;

		  private string LongTestString;

		  public CharSequenceAnonymousInnerClassHelper(TestCharTermAttributeImpl outerInstance, string longTestString)
		  {
			  this.OuterInstance = outerInstance;
			  this.LongTestString = longTestString;
		  }

		  public override char CharAt(int i)
		  {
			  return LongTestString[i];
		  }
		  public override int Length()
		  {
			  return LongTestString.Length;
		  }
		  public override CharSequence SubSequence(int start, int end)
		  {
			  return LongTestString.subSequence(start, end);
		  }
		  public override string ToString()
		  {
			  return LongTestString;
		  }
	  }

      [Test]
      public virtual void TestNonCharSequenceAppend()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.Append("0123456789");
		t.Append("0123456789");
		Assert.AreEqual("01234567890123456789", t.ToString());
		t.Append(new StringBuilder("0123456789"));
		Assert.AreEqual("012345678901234567890123456789", t.ToString());
		CharTermAttribute t2 = new CharTermAttributeImpl();
		t2.Append("test");
		t.Append(t2);
		Assert.AreEqual("012345678901234567890123456789test", t.ToString());
		t.Append((string) null);
		t.Append((StringBuilder) null);
		t.Append((CharTermAttribute) null);
		Assert.AreEqual("012345678901234567890123456789testnullnullnull", t.ToString());
	  }

      [Test]
      public virtual void TestExceptions()
	  {
		CharTermAttributeImpl t = new CharTermAttributeImpl();
		t.Append("test");
		Assert.AreEqual("test", t.ToString());

		try
		{
		  t[-1];
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}

		try
		{
		  t[4];
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}

		try
		{
		  t.subSequence(0, 5);
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}

		try
		{
		  t.subSequence(5, 0);
		  Assert.Fail("Should throw IndexOutOfBoundsException");
		}
		catch (System.IndexOutOfRangeException iobe)
		{
		}
	  }

	  /*
	  
	  // test speed of the dynamic instanceof checks in append(CharSequence),
	  // to find the best max length for the generic while (start<end) loop:
	  public void testAppendPerf() {
	    CharTermAttributeImpl t = new CharTermAttributeImpl();
	    final int count = 32;
	    CharSequence[] csq = new CharSequence[count * 6];
	    final StringBuilder sb = new StringBuilder();
	    for (int i=0,j=0; i<count; i++) {
	      sb.append(i%10);
	      final String testString = sb.toString();
	      CharTermAttribute cta = new CharTermAttributeImpl();
	      cta.append(testString);
	      csq[j++] = cta;
	      csq[j++] = testString;
	      csq[j++] = new StringBuilder(sb);
	      csq[j++] = new StringBuffer(sb);
	      csq[j++] = CharBuffer.wrap(testString.toCharArray());
	      csq[j++] = new CharSequence() {
	        public char charAt(int i) { return testString.charAt(i); }
	        public int length() { return testString.length(); }
	        public CharSequence subSequence(int start, int end) { return testString.subSequence(start, end); }
	        public String toString() { return testString; }
	      };
	    }
	
	    Random rnd = newRandom();
	    long startTime = System.currentTimeMillis();
	    for (int i=0; i<100000000; i++) {
	      t.SetEmpty().append(csq[rnd.nextInt(csq.length)]);
	    }
	    long endTime = System.currentTimeMillis();
	    System.out.println("Time: " + (endTime-startTime)/1000.0 + " s");
	  }
	  
	  */

	}

}