using System;

namespace Lucene.Net.Analysis.Util
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
	/// A StringBuilder that allows one to access the array.
	/// </summary>
	public class OpenStringBuilder : Appendable, CharSequence
	{
	  protected internal char[] buf;
	  protected internal int len;

	  public OpenStringBuilder() : this(32)
	  {
	  }

	  public OpenStringBuilder(int size)
	  {
		buf = new char[size_Renamed];
	  }

	  public OpenStringBuilder(char[] arr, int len)
	  {
		set(arr, len);
	  }

	  public virtual int Length
	  {
		  set
		  {
			  this.len = value;
		  }
	  }

	  public virtual void set(char[] arr, int end)
	  {
		this.buf = arr;
		this.len = end;
	  }

	  public virtual char[] Array
	  {
		  get
		  {
			  return buf;
		  }
	  }
	  public virtual int size()
	  {
		  return len;
	  }
	  public override int length()
	  {
		  return len;
	  }
	  public virtual int capacity()
	  {
		  return buf.Length;
	  }

	  public override Appendable append(CharSequence csq)
	  {
		return append(csq, 0, csq.length());
	  }

	  public override Appendable append(CharSequence csq, int start, int end)
	  {
		reserve(end - start);
		for (int i = start; i < end; i++)
		{
		  unsafeWrite(csq.charAt(i));
		}
		return this;
	  }

	  public override Appendable append(char c)
	  {
		write(c);
		return this;
	  }

	  public override char charAt(int index)
	  {
		return buf[index];
	  }

	  public virtual void setCharAt(int index, char ch)
	  {
		buf[index] = ch;
	  }

	  public override CharSequence subSequence(int start, int end)
	  {
		throw new System.NotSupportedException(); // todo
	  }

	  public virtual void unsafeWrite(char b)
	  {
		buf[len++] = b;
	  }

	  public virtual void unsafeWrite(int b)
	  {
		  unsafeWrite((char)b);
	  }

	  public virtual void unsafeWrite(char[] b, int off, int len)
	  {
		Array.Copy(b, off, buf, this.len, len);
		this.len += len;
	  }

	  protected internal virtual void resize(int len)
	  {
		char[] newbuf = new char[Math.Max(buf.Length << 1, len)];
		Array.Copy(buf, 0, newbuf, 0, size());
		buf = newbuf;
	  }

	  public virtual void reserve(int num)
	  {
		if (len + num > buf.Length)
		{
			resize(len + num);
		}
	  }

	  public virtual void write(char b)
	  {
		if (len >= buf.Length)
		{
		  resize(len + 1);
		}
		unsafeWrite(b);
	  }

	  public virtual void write(int b)
	  {
		  write((char)b);
	  }

	  public void write(char[] b)
	  {
		write(b,0,b.Length);
	  }

	  public virtual void write(char[] b, int off, int len)
	  {
		reserve(len);
		unsafeWrite(b, off, len);
	  }

	  public void write(OpenStringBuilder arr)
	  {
		write(arr.buf, 0, len);
	  }

	  public virtual void write(string s)
	  {
		reserve(s.Length);
		s.CopyTo(0, buf, len, s.Length - 0);
		len += s.Length;
	  }

	  public virtual void flush()
	  {
	  }

	  public void reset()
	  {
		len = 0;
	  }

	  public virtual char[] ToCharArray()
	  {
		char[] newbuf = new char[size()];
		Array.Copy(buf, 0, newbuf, 0, size());
		return newbuf;
	  }

	  public override string ToString()
	  {
		return new string(buf, 0, size());
	  }
	}

}