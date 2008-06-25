/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Payload = Lucene.Net.Index.Payload;

namespace Lucene.Net.Analysis
{
	
	/// <summary>A Token is an occurence of a term from the text of a field.  It consists of
	/// a term's text, the start and end offset of the term in the text of the field,
	/// and a type string.
	/// <p>
	/// The start and end offsets permit applications to re-associate a token with
	/// its source text, e.g., to display highlighted query terms in a document
	/// browser, or to show matching text fragments in a KWIC (KeyWord In Context)
	/// display, etc.
	/// <p>
	/// The type is an interned string, assigned by a lexical analyzer
	/// (a.k.a. tokenizer), naming the lexical or syntactic class that the token
	/// belongs to.  For example an end of sentence marker token might be implemented
	/// with type "eos".  The default token type is "word".  
	/// <p>
	/// A Token can optionally have metadata (a.k.a. Payload) in the form of a variable
	/// length byte array. Use {@link TermPositions#GetPayloadLength()} and 
	/// {@link TermPositions#GetPayload(byte[], int)} to retrieve the payloads from the index.
	/// </summary>
	/// <summary><br><br>
	/// <p><font color="#FF0000">
	/// WARNING: The status of the <b>Payloads</b> feature is experimental. 
	/// The APIs introduced here might change in the future and will not be 
	/// supported anymore in such a case.</font>
	/// <br><br>
	/// <p><b>NOTE:</b> As of 2.3, Token stores the term text
	/// internally as a malleable char[] termBuffer instead of
	/// String termText.  The indexing code and core tokenizers
	/// have been changed re-use a single Token instance, changing
	/// its buffer and other fields in-place as the Token is
	/// processed.  This provides substantially better indexing
	/// performance as it saves the GC cost of new'ing a Token and
	/// String for every term.  The APIs that accept String
	/// termText are still available but a warning about the
	/// associated performance cost has been added (below).  The
	/// {@link #TermText()} method has been deprecated.</p>
	/// </summary>
	/// <summary><p>Tokenizers and filters should try to re-use a Token
	/// instance when possible for best performance, by
	/// implementing the {@link TokenStream#Next(Token)} API.
	/// Failing that, to create a new Token you should first use
	/// one of the constructors that starts with null text.  Then
	/// you should call either {@link #TermBuffer()} or {@link
	/// #ResizeTermBuffer(int)} to retrieve the Token's
	/// termBuffer.  Fill in the characters of your term into this
	/// buffer, and finally call {@link #SetTermLength(int)} to
	/// set the length of the term text.  See <a target="_top"
	/// href="https://issues.apache.org/jira/browse/LUCENE-969">LUCENE-969</a>
	/// for details.</p>
	/// </summary>
	/// <seealso cref="Lucene.Net.Index.Payload">
	/// </seealso>
    public class Token : System.ICloneable
    {
		
		public const System.String DEFAULT_TYPE = "word";
		private static int MIN_BUFFER_SIZE = 10;
		
		/// <deprecated>: we will remove this when we remove the
		/// deprecated APIs 
		/// </deprecated>
		private System.String termText;
		
		internal char[] termBuffer; // characters for the term text
		internal int termLength; // length of term text in buffer
		
		internal int startOffset; // start in source text
		internal int endOffset; // end in source text
		internal System.String type = DEFAULT_TYPE; // lexical type
		
		internal Payload payload;
		
		internal int positionIncrement = 1;
		
		/// <summary>Constructs a Token will null text. </summary>
		public Token()
		{
		}
		
		/// <summary>Constructs a Token with null text and start & end
		/// offsets.
		/// </summary>
		/// <param name="start">start offset
		/// </param>
		/// <param name="end">end offset 
		/// </param>
		public Token(int start, int end)
		{
			startOffset = start;
			endOffset = end;
		}
		
		/// <summary>Constructs a Token with null text and start & end
		/// offsets plus the Token type.
		/// </summary>
		/// <param name="start">start offset
		/// </param>
		/// <param name="end">end offset 
		/// </param>
		public Token(int start, int end, System.String typ)
		{
			startOffset = start;
			endOffset = end;
			type = typ;
		}
		
		/// <summary>Constructs a Token with the given term text, and start
		/// & end offsets.  The type defaults to "word."
		/// <b>NOTE:</b> for better indexing speed you should
		/// instead use the char[] termBuffer methods to set the
		/// term text.
		/// </summary>
		/// <param name="text">term text
		/// </param>
		/// <param name="start">start offset
		/// </param>
		/// <param name="end">end offset 
		/// </param>
		public Token(System.String text, int start, int end)
		{
			termText = text;
			startOffset = start;
			endOffset = end;
		}
		
		/// <summary>Constructs a Token with the given text, start and end
		/// offsets, & type.  <b>NOTE:</b> for better indexing
		/// speed you should instead use the char[] termBuffer
		/// methods to set the term text.
		/// </summary>
		/// <param name="text">term text
		/// </param>
		/// <param name="start">start offset
		/// </param>
		/// <param name="end">end offset
		/// </param>
		/// <param name="typ">token type 
		/// </param>
		public Token(System.String text, int start, int end, System.String typ)
		{
			termText = text;
			startOffset = start;
			endOffset = end;
			type = typ;
		}
		
		/// <summary>Set the position increment.  This determines the position of this token
		/// relative to the previous Token in a {@link TokenStream}, used in phrase
		/// searching.
		/// 
		/// <p>The default value is one.
		/// 
		/// <p>Some common uses for this are:<ul>
		/// 
		/// <li>Set it to zero to put multiple terms in the same position.  This is
		/// useful if, e.g., a word has multiple stems.  Searches for phrases
		/// including either stem will match.  In this case, all but the first stem's
		/// increment should be set to zero: the increment of the first instance
		/// should be one.  Repeating a token with an increment of zero can also be
		/// used to boost the scores of matches on that token.
		/// 
		/// <li>Set it to values greater than one to inhibit exact phrase matches.
		/// If, for example, one does not want phrases to match across removed stop
		/// words, then one could build a stop word filter that removes stop words and
		/// also sets the increment to the number of stop words removed before each
		/// non-stop word.  Then exact phrase queries will only match when the terms
		/// occur with no intervening stop words.
		/// 
		/// </ul>
		/// </summary>
		/// <seealso cref="Lucene.Net.Index.TermPositions">
		/// </seealso>
		public virtual void  SetPositionIncrement(int positionIncrement)
		{
			if (positionIncrement < 0)
				throw new System.ArgumentException("Increment must be zero or greater: " + positionIncrement);
			this.positionIncrement = positionIncrement;
		}
		
		/// <summary>Returns the position increment of this Token.</summary>
		/// <seealso cref="setPositionIncrement">
		/// </seealso>
		public virtual int GetPositionIncrement()
		{
			return positionIncrement;
		}
		
		/// <summary>Sets the Token's term text.  <b>NOTE:</b> for better
		/// indexing speed you should instead use the char[]
		/// termBuffer methods to set the term text. 
		/// </summary>
		public virtual void  SetTermText(System.String text)
		{
			termText = text;
			termBuffer = null;
		}
		
		/// <summary>Returns the Token's term text.
		/// 
		/// </summary>
		/// <deprecated> Use {@link #TermBuffer()} and {@link
		/// #TermLength()} instead. 
		/// </deprecated>
		public System.String TermText()
		{
			if (termText == null && termBuffer != null)
				termText = new System.String(termBuffer, 0, termLength);
			return termText;
		}
		
		/// <summary>Copies the contents of buffer, starting at offset for
		/// length characters, into the termBuffer
		/// array. <b>NOTE:</b> for better indexing speed you
		/// should instead retrieve the termBuffer, using {@link
		/// #TermBuffer()} or {@link #ResizeTermBuffer(int)}, and
		/// fill it in directly to set the term text.  This saves
		/// an extra copy. 
		/// </summary>
		public void  SetTermBuffer(char[] buffer, int offset, int length)
		{
			ResizeTermBuffer(length);
			Array.Copy(buffer, offset, termBuffer, 0, length);
			termLength = length;
		}
		
		/// <summary>Returns the internal termBuffer character array which
		/// you can then directly alter.  If the array is too
		/// small for your token, use {@link
		/// #ResizeTermBuffer(int)} to increase it.  After
		/// altering the buffer be sure to call {@link
		/// #setTermLength} to record the number of valid
		/// characters that were placed into the termBuffer. 
		/// </summary>
		public char[] TermBuffer()
		{
			InitTermBuffer();
			return termBuffer;
		}
		
		/// <summary>Grows the termBuffer to at least size newSize.</summary>
		/// <param name="newSize">minimum size of the new termBuffer
		/// </param>
		/// <returns> newly created termBuffer with length >= newSize
		/// </returns>
		public virtual char[] ResizeTermBuffer(int newSize)
		{
			InitTermBuffer();
			if (newSize > termBuffer.Length)
			{
				int size = termBuffer.Length;
				while (size < newSize)
					size *= 2;
				char[] newBuffer = new char[size];
				Array.Copy(termBuffer, 0, newBuffer, 0, termBuffer.Length);
				termBuffer = newBuffer;
			}
			return termBuffer;
		}
		
		// TODO: once we remove the deprecated termText() method
		// and switch entirely to char[] termBuffer we don't need
		// to use this method anymore
		private void  InitTermBuffer()
		{
			if (termBuffer == null)
			{
				if (termText == null)
				{
					termBuffer = new char[MIN_BUFFER_SIZE];
					termLength = 0;
				}
				else
				{
					int length = termText.Length;
					if (length < MIN_BUFFER_SIZE)
						length = MIN_BUFFER_SIZE;
					termBuffer = new char[length];
					termLength = termText.Length;

					int offset = 0;
					while (offset < termText.Length)
					{
						termBuffer[offset] = (char) termText[offset];
						offset++;
					}

					termText = null;
				}
			}
			else if (termText != null)
				termText = null;
		}
		
		/// <summary>Return number of valid characters (length of the term)
		/// in the termBuffer array. 
		/// </summary>
		public int TermLength()
		{
			InitTermBuffer();
			return termLength;
		}
		
		/// <summary>Set number of valid characters (length of the term) in
		/// the termBuffer array. 
		/// </summary>
		public void  SetTermLength(int length)
		{
			InitTermBuffer();
			termLength = length;
		}
		
		/// <summary>Returns this Token's starting offset, the position of the first character
		/// corresponding to this token in the source text.
		/// Note that the difference between endOffset() and startOffset() may not be
		/// equal to termText.length(), as the term text may have been altered by a
		/// stemmer or some other filter. 
		/// </summary>
		public int StartOffset()
		{
			return startOffset;
		}
		
		/// <summary>Set the starting offset.</summary>
		/// <seealso cref="StartOffset()">
		/// </seealso>
		public virtual void  SetStartOffset(int offset)
		{
			this.startOffset = offset;
		}
		
		/// <summary>Returns this Token's ending offset, one greater than the position of the
		/// last character corresponding to this token in the source text. 
		/// </summary>
		public int EndOffset()
		{
			return endOffset;
		}
		
		/// <summary>Set the ending offset.</summary>
		/// <seealso cref="EndOffset()">
		/// </seealso>
		public virtual void  SetEndOffset(int offset)
		{
			this.endOffset = offset;
		}
		
		/// <summary>Returns this Token's lexical type.  Defaults to "word". </summary>
		public System.String Type()
		{
			return type;
		}
		
		/// <summary>Set the lexical type.</summary>
		/// <seealso cref="Type()">
		/// </seealso>
		public void  SetType(System.String type)
		{
			this.type = type;
		}
		
		/// <summary> Returns this Token's payload.</summary>
		public virtual Payload GetPayload()
		{
			return this.payload;
		}
		
		/// <summary> Sets this Token's payload.</summary>
		public virtual void  SetPayload(Payload payload)
		{
			this.payload = payload;
		}
		
		public override System.String ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append('(');
			InitTermBuffer();
			if (termBuffer == null)
				sb.Append("null");
			else
				sb.Append(termBuffer, 0, termLength);
			sb.Append(',').Append(startOffset).Append(',').Append(endOffset);
			if (!type.Equals("word"))
				sb.Append(",type=").Append(type);
			if (positionIncrement != 1)
				sb.Append(",posIncr=").Append(positionIncrement);
			sb.Append(')');
			return sb.ToString();
		}
		
		/// <summary>Resets the term text, payload, and positionIncrement to default.
		/// Other fields such as startOffset, endOffset and the token type are
		/// not reset since they are normally overwritten by the tokenizer. 
		/// </summary>
		public virtual void  Clear()
		{
			payload = null;
			// Leave termBuffer to allow re-use
			termLength = 0;
			termText = null;
			positionIncrement = 1;
			// startOffset = endOffset = 0;
			// type = DEFAULT_TYPE;
		}
		
		public virtual System.Object Clone()
		{
			try
			{
				Token t = (Token) base.MemberwiseClone();
				if (termBuffer != null)
				{
					t.termBuffer = null;
					t.SetTermBuffer(termBuffer, 0, termLength);
				}
				if (payload != null)
				{
					t.SetPayload((Payload) payload.Clone());
				}
				return t;
			}
			catch (System.Exception e)
			{
				throw new System.SystemException("", e); // shouldn't happen
			}
		}
	}
}