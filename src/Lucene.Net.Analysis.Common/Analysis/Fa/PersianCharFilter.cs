namespace org.apache.lucene.analysis.fa
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
	/// CharFilter that replaces instances of Zero-width non-joiner with an
	/// ordinary space.
	/// </summary>
	public class PersianCharFilter : CharFilter
	{

	  public PersianCharFilter(Reader @in) : base(@in)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int read(char[] cbuf, int off, int len) throws java.io.IOException
	  public override int read(char[] cbuf, int off, int len)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int charsRead = input.read(cbuf, off, len);
		int charsRead = input.read(cbuf, off, len);
		if (charsRead > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = off + charsRead;
		  int end = off + charsRead;
		  while (off < end)
		  {
			if (cbuf[off] == '\u200C')
			{
			  cbuf[off] = ' ';
			}
			off++;
		  }
		}
		return charsRead;
	  }

	  // optimized impl: some other charfilters consume with read()
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int read() throws java.io.IOException
	  public override int read()
	  {
		int ch = input.read();
		if (ch == '\u200C')
		{
		  return ' ';
		}
		else
		{
		  return ch;
		}
	  }

	  protected internal override int correct(int currentOff)
	  {
		return currentOff; // we don't change the length of the string
	  }
	}

}