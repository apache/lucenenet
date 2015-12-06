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


	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestCharArrayIterator : LuceneTestCase
	{

	  public virtual void testWordInstance()
	  {
		doTests(CharArrayIterator.newWordInstance());
	  }

	  public virtual void testConsumeWordInstance()
	  {
		// we use the default locale, as its randomized by LuceneTestCase
		BreakIterator bi = BreakIterator.getWordInstance(Locale.Default);
		CharArrayIterator ci = CharArrayIterator.newWordInstance();
		for (int i = 0; i < 10000; i++)
		{
		  char[] text = TestUtil.randomUnicodeString(random()).toCharArray();
		  ci.setText(text, 0, text.Length);
		  consume(bi, ci);
		}
	  }

	  /* run this to test if your JRE is buggy
	  public void testWordInstanceJREBUG() {
	    // we use the default locale, as its randomized by LuceneTestCase
	    BreakIterator bi = BreakIterator.getWordInstance(Locale.getDefault());
	    Segment ci = new Segment();
	    for (int i = 0; i < 10000; i++) {
	      char text[] = TestUtil.randomUnicodeString(random).toCharArray();
	      ci.array = text;
	      ci.offset = 0;
	      ci.count = text.length;
	      consume(bi, ci);
	    }
	  }
	  */

	  public virtual void testSentenceInstance()
	  {
		doTests(CharArrayIterator.newSentenceInstance());
	  }

	  public virtual void testConsumeSentenceInstance()
	  {
		// we use the default locale, as its randomized by LuceneTestCase
		BreakIterator bi = BreakIterator.getSentenceInstance(Locale.Default);
		CharArrayIterator ci = CharArrayIterator.newSentenceInstance();
		for (int i = 0; i < 10000; i++)
		{
		  char[] text = TestUtil.randomUnicodeString(random()).toCharArray();
		  ci.setText(text, 0, text.Length);
		  consume(bi, ci);
		}
	  }

	  /* run this to test if your JRE is buggy
	  public void testSentenceInstanceJREBUG() {
	    // we use the default locale, as its randomized by LuceneTestCase
	    BreakIterator bi = BreakIterator.getSentenceInstance(Locale.getDefault());
	    Segment ci = new Segment();
	    for (int i = 0; i < 10000; i++) {
	      char text[] = TestUtil.randomUnicodeString(random).toCharArray();
	      ci.array = text;
	      ci.offset = 0;
	      ci.count = text.length;
	      consume(bi, ci);
	    }
	  }
	  */

	  private void doTests(CharArrayIterator ci)
	  {
		// basics
		ci.setText("testing".ToCharArray(), 0, "testing".Length);
		assertEquals(0, ci.BeginIndex);
		assertEquals(7, ci.EndIndex);
		assertEquals(0, ci.Index);
		assertEquals('t', ci.current());
		assertEquals('e', ci.next());
		assertEquals('g', ci.last());
		assertEquals('n', ci.previous());
		assertEquals('t', ci.first());
		assertEquals(CharacterIterator.DONE, ci.previous());

		// first()
		ci.setText("testing".ToCharArray(), 0, "testing".Length);
		ci.next();
		// Sets the position to getBeginIndex() and returns the character at that position. 
		assertEquals('t', ci.first());
		assertEquals(ci.BeginIndex, ci.Index);
		// or DONE if the text is empty
		ci.setText(new char[] {}, 0, 0);
		assertEquals(CharacterIterator.DONE, ci.first());

		// last()
		ci.setText("testing".ToCharArray(), 0, "testing".Length);
		// Sets the position to getEndIndex()-1 (getEndIndex() if the text is empty) 
		// and returns the character at that position. 
		assertEquals('g', ci.last());
		assertEquals(ci.Index, ci.EndIndex - 1);
		// or DONE if the text is empty
		ci.setText(new char[] {}, 0, 0);
		assertEquals(CharacterIterator.DONE, ci.last());
		assertEquals(ci.EndIndex, ci.Index);

		// current()
		// Gets the character at the current position (as returned by getIndex()). 
		ci.setText("testing".ToCharArray(), 0, "testing".Length);
		assertEquals('t', ci.current());
		ci.last();
		ci.next();
		// or DONE if the current position is off the end of the text.
		assertEquals(CharacterIterator.DONE, ci.current());

		// next()
		ci.setText("te".ToCharArray(), 0, 2);
		// Increments the iterator's index by one and returns the character at the new index.
		assertEquals('e', ci.next());
		assertEquals(1, ci.Index);
		// or DONE if the new position is off the end of the text range.
		assertEquals(CharacterIterator.DONE, ci.next());
		assertEquals(ci.EndIndex, ci.Index);

		// setIndex()
		ci.setText("test".ToCharArray(), 0, "test".Length);
		try
		{
		  ci.Index = 5;
		  fail();
		}
		catch (Exception e)
		{
		  assertTrue(e is System.ArgumentException);
		}

		// clone()
		char[] text = "testing".ToCharArray();
		ci.setText(text, 0, text.Length);
		ci.next();
		CharArrayIterator ci2 = ci.clone();
		assertEquals(ci.Index, ci2.Index);
		assertEquals(ci.next(), ci2.next());
		assertEquals(ci.last(), ci2.last());
	  }

	  private void consume(BreakIterator bi, CharacterIterator ci)
	  {
		bi.Text = ci;
		while (bi.next() != BreakIterator.DONE)
		{
		  ;
		}
	  }
	}

}