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


	/// <summary>
	/// A CharacterIterator used internally for use with <seealso cref="BreakIterator"/>
	/// @lucene.internal
	/// </summary>
	public abstract class CharArrayIterator //: CharacterIterator
	{
	  private char[] array;
	  private int start;
	  private int index;
	  private int length;
	  private int limit;

	  public virtual char [] Text
	  {
		  get
		  {
			return array;
		  }
	  }

	  public virtual int Start
	  {
		  get
		  {
			return start;
		  }
	  }

	  public virtual int Length
	  {
		  get
		  {
			return length;
		  }
	  }

	  /// <summary>
	  /// Set a new region of text to be examined by this iterator
	  /// </summary>
	  /// <param name="array"> text buffer to examine </param>
	  /// <param name="start"> offset into buffer </param>
	  /// <param name="length"> maximum length to examine </param>
	  public virtual void setText(char[] array, int start, int length)
	  {
		this.array = array;
		this.start = start;
		this.index = start;
		this.length = length;
		this.limit = start + length;
	  }

	  public override char Current()
	  {
		return (index == limit) ? DONE : jreBugWorkaround(array[index]);
	  }

	  protected internal abstract char jreBugWorkaround(char ch);

	  public override char First()
	  {
		index = start;
		return Current();
	  }

	  public override int BeginIndex
	  {
		  get
		  {
			return 0;
		  }
	  }

	  public override int EndIndex
	  {
		  get
		  {
			return length;
		  }
	  }

	  public override int Index
	  {
		  get
		  {
			return index - start;
		  }
	  }

	  public override char Last()
	  {
		index = (limit == start) ? limit : limit - 1;
		return current();
	  }

	  public override char Next()
	  {
		if (++index >= limit)
		{
		  index = limit;
		  return DONE;
		}
		else
		{
		  return current();
		}
	  }

	  public override char Previous()
	  {
		if (--index < start)
		{
		  index = start;
		  return DONE;
		}
		else
		{
		  return current();
		}
	  }

	  public override char SetIndex(int position)
	  {
		if (position < BeginIndex || position > EndIndex)
		{
		  throw new System.ArgumentException("Illegal Position: " + position);
		}
		index = start + position;
		return current();
	  }

	  public override CharArrayIterator Clone()
	  {
		try
		{
		  return (CharArrayIterator)base.clone();
		}
		catch (CloneNotSupportedException e)
		{
		  // CharacterIterator does not allow you to throw CloneNotSupported
		  throw new Exception(e);
		}
	  }

	  /// <summary>
	  /// Create a new CharArrayIterator that works around JRE bugs
	  /// in a manner suitable for <seealso cref="BreakIterator#getSentenceInstance()"/>
	  /// </summary>
	  public static CharArrayIterator newSentenceInstance()
	  {
		if (HAS_BUGGY_BREAKITERATORS)
		{
		  return new CharArrayIteratorAnonymousInnerClassHelper();
		}
		else
		{
		  return new CharArrayIteratorAnonymousInnerClassHelper2();
		}
	  }

	  private class CharArrayIteratorAnonymousInnerClassHelper : CharArrayIterator
	  {
		  public CharArrayIteratorAnonymousInnerClassHelper()
		  {
		  }

			  // work around this for now by lying about all surrogates to 
			  // the sentence tokenizer, instead we treat them all as 
			  // SContinue so we won't break around them.
		  protected internal override char jreBugWorkaround(char ch)
		  {
			return ch >= 0xD800 && ch <= 0xDFFF ? 0x002C : ch;
		  }
	  }

	  private class CharArrayIteratorAnonymousInnerClassHelper2 : CharArrayIterator
	  {
		  public CharArrayIteratorAnonymousInnerClassHelper2()
		  {
		  }

			  // no bugs
		  protected internal override char jreBugWorkaround(char ch)
		  {
			return ch;
		  }
	  }

	  /// <summary>
	  /// Create a new CharArrayIterator that works around JRE bugs
	  /// in a manner suitable for <seealso cref="BreakIterator#getWordInstance()"/>
	  /// </summary>
	  public static CharArrayIterator newWordInstance()
	  {
		if (HAS_BUGGY_BREAKITERATORS)
		{
		  return new CharArrayIteratorAnonymousInnerClassHelper3();
		}
		else
		{
		  return new CharArrayIteratorAnonymousInnerClassHelper4();
		}
	  }

	  private class CharArrayIteratorAnonymousInnerClassHelper3 : CharArrayIterator
	  {
		  public CharArrayIteratorAnonymousInnerClassHelper3()
		  {
		  }

			  // work around this for now by lying about all surrogates to the word, 
			  // instead we treat them all as ALetter so we won't break around them.
		  protected internal override char jreBugWorkaround(char ch)
		  {
			return ch >= 0xD800 && ch <= 0xDFFF ? 0x0041 : ch;
		  }
	  }

	  private class CharArrayIteratorAnonymousInnerClassHelper4 : CharArrayIterator
	  {
		  public CharArrayIteratorAnonymousInnerClassHelper4()
		  {
		  }

			  // no bugs
		  protected internal override char jreBugWorkaround(char ch)
		  {
			return ch;
		  }
	  }

	  /// <summary>
	  /// True if this JRE has a buggy BreakIterator implementation
	  /// </summary>
	  public static readonly bool HAS_BUGGY_BREAKITERATORS;
	  static CharArrayIterator()
	  {
		bool v;
		try
		{
		  BreakIterator bi = BreakIterator.getSentenceInstance(Locale.US);
		  bi.Text = "\udb40\udc53";
		  bi.next();
		  v = false;
		}
		catch (Exception)
		{
		  v = true;
		}
		HAS_BUGGY_BREAKITERATORS = v;
	  }
	}

}