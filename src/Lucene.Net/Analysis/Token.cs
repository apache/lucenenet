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
using ArrayUtil = Lucene.Net.Util.ArrayUtil;

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
	/// The type is a string, assigned by a lexical analyzer
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
	/// have been changed to re-use a single Token instance, changing
	/// its buffer and other fields in-place as the Token is
	/// processed.  This provides substantially better indexing
	/// performance as it saves the GC cost of new'ing a Token and
	/// String for every term.  The APIs that accept String
	/// termText are still available but a warning about the
	/// associated performance cost has been added (below).  The
	/// {@link #TermText()} method has been deprecated.</p>
    /// <p>Tokenizers and filters should try to re-use a Token
	/// instance when possible for best performance, by
	/// implementing the {@link TokenStream#Next(Token)} API.
	/// Failing that, to create a new Token you should first use
    /// one of the constructors that starts with null text.  To load
    /// the token from a char[] use {@link #setTermBuffer(char[], int, int)}.
    /// To load from a String use {@link #setTermBuffer(String)} or {@link #setTermBuffer(String, int, int)}.
    ///  Alternatively you can get the Token's termBuffer by calling either {@link #termBuffer()},
    ///  if you know that your text is shorter than the capacity of the termBuffer
    /// or {@link #resizeTermBuffer(int)}, if there is any possibility
    /// that you may need to grow the buffer. Fill in the characters of your term into this
    /// buffer, with {@link String#getChars(int, int, char[], int)} if loading from a string,
    /// or with {@link System#arraycopy(object, int, object, int, int)}, and finally call {@link #setTermLength(int)} to
    /// set the length of the term text.  See <a target="_top"
    /// href="https://issues.apache.org/jira/browse/LUCENE-969">LUCENE-969</a>
    /// for details.</p>
    /// <p>Typical reuse patterns:
    /// <ul>
    /// <li> Copying text from a string (type is reset to #DEFAULT_TYPE if not specified):<br/>
    ///  <pre>
    ///    return reusableToken.reinit(string, startOffset, endOffset[, type]);
    /// </pre>
    /// </li>
    /// <li> Copying some text from a string (type is reset to #DEFAULT_TYPE if not specified):<br/>
    ///  <pre>
    /// return reusableToken.reinit(string, 0, string.length(), startOffset, endOffset[, type]);
    /// </pre>
    /// </li>
    /// </li>
    /// <li> Copying text from char[] buffer (type is reset to #DEFAULT_TYPE if not specified):<br/>
    ///  <pre>
    /// return reusableToken.reinit(buffer, 0, buffer.length, startOffset, endOffset[, type]);
    /// </pre>
    /// </li>
    /// <li> Copying some text from a char[] buffer (type is reset to #DEFAULT_TYPE if not specified):<br/>
    /// <pre>
    /// return reusableToken.reinit(buffer, start, end - start, startOffset, endOffset[, type]);
    /// </pre>
    /// </li>
    /// <li> Copying from one one Token to another (type is reset to #DEFAULT_TYPE if not specified):<br/>
    /// <pre>
    /// return reusableToken.reinit(source.termBuffer(), 0, source.termLength(), source.startOffset(), source.endOffset()[, source.type()]);
    /// </pre>
    /// </li>
    ///  </ul>
    /// A few things to note:
    /// <ul>
    ///  <li>clear() initializes most of the fields to default values, but not startOffset, endOffset and type.</li>
    /// <li>Because <code>TokenStreams</code> can be chained, one cannot assume that the <code>Token's</code> current type is correct.</li>
    /// <li>The startOffset and endOffset represent the start and offset in the source text. So be careful in adjusting them.</li>
    /// <li>When caching a reusable token, clone it. When injecting a cached token into a stream that can be reset, clone it again.</li>
    /// </ul>
    /// </p>
	/// </summary>
	/// <seealso cref="Lucene.Net.Index.Payload">
	/// </seealso>
    public class Token : System.ICloneable
    {
		
		public const System.String DEFAULT_TYPE = "word";

		private static int MIN_BUFFER_SIZE = 10;
		
		/// <deprecated>
        /// We will remove this when we remove the deprecated APIs. 
		/// </deprecated>
		private System.String termText;

        /// <summary>
        /// Characters for the term text.
        /// </summary>
        /// <deprecated>
        /// This will be made private.  Instead, use:
        /// {@link #setTermBuffer(char[], int, int)},
        /// {@link #setTermBuffer(String)}, or
        /// {@link #setTermBuffer(String, int, int)},
        /// </deprecated>
        internal char[] termBuffer;

        /// <summary>
        /// Length of term text in the buffer.
        /// </summary>
        /// <deprecated>
        /// This will be made private.  Instead, use:
        /// {@link termLength()} or {@link setTermLength(int)}
        /// </deprecated>
        internal int termLength;

        /// <summary>
        /// Start in source text.
        /// </summary>
        /// <deprecated>
        /// This will be made private.  Instead, use:
        /// {@link startOffset()} or {@link setStartOffset(int)}
        /// </deprecated>
        internal int startOffset;

        /// <summary>
        /// End in source text.
        /// </summary>
        /// <deprecated>
        /// This will be made private.  Instead, use:
        /// {@link endOffset()} or {@link setEndOffset(int)}
        /// </deprecated>
        internal int endOffset;

        /// <summary>
        /// The lexical type of the token.
        /// </summary>
        /// <deprecated>
        /// This will be made private.  Instead, use:
        /// {@link type()} or {@link setType(String)}
        /// </deprecated>
        internal System.String type = DEFAULT_TYPE;

        private int flags;
        
        /// <deprecated>
        /// This will be made private. Instead, use:
        /// {@link getPayload()} or {@link setPayload(Payload)}.
        /// </deprecated>
        internal Payload payload;
		
        /// <deprecated>
        /// This will be made private. Instead, use:
        /// {@link getPositionIncrement()} or {@link setPositionIncrement(String)}.
        /// </deprecated>
		internal int positionIncrement = 1;

		/// <summary>Constructs a Token will null text. </summary>
		public Token()
		{
		}
		
		/// <summary>
        /// Constructs a Token with null text and start & endoffsets.
		/// </summary>
		/// <param name="start">start offset in the source text</param>
        /// <param name="end">end offset in the source text</param>
		public Token(int start, int end)
		{
			startOffset = start;
			endOffset = end;
		}

        /// <summary>
        /// Constructs a Token with null text and start & endoffsets plus the Token type.
        /// </summary>
        /// <param name="start">start offset in the source text</param>
        /// <param name="end">end offset in the source text</param>
        /// <param name="typ">the lexical type of this Token</param>
        public Token(int start, int end, System.String typ)
		{
			startOffset = start;
			endOffset = end;
			type = typ;
		}

        /// <summary>
        /// Constructs a Token with null text and start & endoffsets plus flags.
        /// NOTE: flags is EXPERIMENTAL.
        /// </summary>
        /// <param name="start">start offset in the source text</param>
        /// <param name="end">end offset in the source text</param>
        /// <param name="flags">the bits to set for this Token</param>
        public Token(int start, int end, int flags)
        {
            startOffset = start;
            endOffset = end;
            this.flags = flags;
        }

        /// <summary>
        /// Constructs a Token with the given term text, and start
		/// and end offsets.  The type defaults to "word."
		/// <b>NOTE:</b> for better indexing speed you should
		/// instead use the char[] termBuffer methods to set the
		/// term text.
		/// </summary>
        /// <param name="text">term text</param>
		/// <param name="start">start offset</param>
		/// <param name="end">end offset</param>
        /// <deprecated></deprecated>
		public Token(System.String text, int start, int end)
		{
			termText = text;
			startOffset = start;
			endOffset = end;
		}

        /// <summary>
        /// Constructs a Token with the given term text, start
        /// and end offsets, and type.
        /// <b>NOTE:</b> for better indexing speed you should
        /// instead use the char[] termBuffer methods to set the
        /// term text.
        /// </summary>
        /// <param name="text">term text</param>
        /// <param name="start">start offset</param>
        /// <param name="end">end offset</param>
        /// <param name="typ">token type</param>
        /// <deprecated></deprecated>
        public Token(System.String text, int start, int end, System.String typ)
		{
			termText = text;
			startOffset = start;
			endOffset = end;
			type = typ;
		}

        /// <summary>
        /// Constructs a Token with the given term text, start
        /// and end offsets, and flags.
        /// <b>NOTE:</b> for better indexing speed you should
        /// instead use the char[] termBuffer methods to set the
        /// term text.
        /// </summary>
        /// <param name="text">term text</param>
        /// <param name="start">start offset</param>
        /// <param name="end">end offset</param>
        /// <param name="flags">the bits to set for this Token</param>
        /// <deprecated></deprecated>
        public Token(System.String text, int start, int end, int flags)
        {
            termText = text;
            startOffset = start;
            endOffset = end;
            this.flags = flags;
        }

        /// <summary>
        /// Constructs a Token with the given term buffer (offset and length), start and end offsets.
        /// </summary>
        /// <param name="startTermBuffer"></param>
        /// <param name="termBufferOffset"></param>
        /// <param name="termBufferLength"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public Token(char[] startTermBuffer, int termBufferOffset, int termBufferLength, int start, int end)
        {
            SetTermBuffer(startTermBuffer, termBufferOffset, termBufferLength);
            startOffset = start;
            endOffset = end;
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
        /// <param name="positionIncrement">the distance from the prior term</param>
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
        /// <deprecated>
        /// use {@link #setTermBuffer(char[], int, int)}, 
        ///     {@link #setTermBuffer(string)}, or
        ///     {@link #setTermBuffer(string, int, int)}.
        /// </deprecated>
		public virtual void  SetTermText(System.String text)
		{
			termText = text;
			termBuffer = null;
		}

        /// <summary>
        /// Returns the Token's term text.
        /// This method has a performance penalty because the text is stored
        /// internally in a char[].  If possible, use {@link #termBuffer()}
        /// and {@link #termLength()} directly instead.  If you really need
        /// a string, use {@link #Term()}.
        /// </summary>
        /// <returns></returns>
        public System.String TermText()
		{
			if (termText == null && termBuffer != null)
				termText = new System.String(termBuffer, 0, termLength);
			return termText;
		}

        /// <summary>
        /// Returns the Token's term text.
        /// This method has a performance penalty because the text is stored
        /// internally in a char[].  If possible, use {@link #termBuffer()}
        /// and {@link #termLength()} directly instead.  If you really need
        /// a string, use this method which is nothing more than a
        /// convenience cal to <b>new String(token.TermBuffer(), o, token.TermLength())</b>.
        /// </summary>
        /// <returns></returns>
        public string Term()
        {
            if (termText != null)
                return termText;
            InitTermBuffer();
            return new String(termBuffer, 0, termLength);
        }

		/// <summary>
        /// Copies the contents of buffer, starting at offset for
		/// length characters, into the termBuffer array.
		/// </summary>
        /// <param name="buffer"/>
        /// <param name="offset"/>
        /// <param name="length"/>
		public void  SetTermBuffer(char[] buffer, int offset, int length)
		{
            termText = null;
            char[] newCharBuffer = GrowTermBuffer(length);
            if (newCharBuffer != null)
                termBuffer = newCharBuffer;
            Array.Copy(buffer, offset, termBuffer, 0, length);
			termLength = length;
		}

        /// <summary>
        /// Copies the contents of buffer, starting at offset for
        /// length characters, into the termBuffer array.
        /// </summary>
        /// <param name="buffer"/>
        public void SetTermBuffer(string buffer)
        {
            termText = null;
            int length = buffer.Length;
            char[] newCharBuffer = GrowTermBuffer(length);
            if (newCharBuffer != null)
                termBuffer = newCharBuffer;
            buffer.CopyTo(0, termBuffer, 0, length);
            termLength = length;
        }

        /// <summary>
        /// Copies the contents of buffer, starting at offset for
        /// length characters, into the termBuffer array.
        /// </summary>
        /// <param name="buffer"/>
        /// <param name="offset"/>
        /// <param name="length"/>
        public void SetTermBuffer(string buffer, int offset, int length)
        {
            System.Diagnostics.Debug.Assert(offset <= buffer.Length);
            System.Diagnostics.Debug.Assert(offset + length <= buffer.Length);
            termText = null;
            char[] newCharBuffer = GrowTermBuffer(length);
            if (newCharBuffer != null)
                termBuffer = newCharBuffer;
            buffer.CopyTo(offset, termBuffer, 0, length);
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
		
		/// <summary>
        /// Grows the termBuffer to at least size newSize, preserving the
        /// existing content.  Note: If the next operation is to change
        /// the contents of the term buffer use
        /// {@link #setTermBuffer(char[], int, int)},
        /// {@link #setTermBuffer(String)}, or
        /// {@link #setTermBuffer(String, int, int)},
        /// to optimally combine the resize with the setting of the termBuffer.
        /// </summary>
		/// <param name="newSize">minimum size of the new termBuffer</param>
		/// <returns> newly created termBuffer with length >= newSize</returns>
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

        /// <summary>
        /// Allocates a buffer char[] of at least newSize.
        /// </summary>
        /// <param name="newSize">minimum size of the buffer</param>
        /// <returns>newly created buffer with length >= newSize or null if the current termBuffer is big enough</returns>
        private char[] GrowTermBuffer(int newSize)
        {
            if (termBuffer != null)
            {
                if (termBuffer.Length >= newSize)
                    // Already big enough 
                    return null;
                else
                    // Not big enough; create a new array with slight
                    // over-allocation
                    return new char[ArrayUtil.GetNextSize(newSize)];
            }
            else
            {
                // determine the best size
                // The buffer is always at least MIN_BUFFER_SIZE
                if (newSize < MIN_BUFFER_SIZE)
                    newSize = MIN_BUFFER_SIZE;

                // If there is already a termText, then the size has to be at least that big
                if (termText != null)
                {
                    int ttLengh = termText.Length;
                    if (newSize < ttLengh)
                        newSize = ttLengh;
                }

                return new char[newSize];
            }
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
		/// the termBuffer array.  Use this to truncate the termBuffer
        /// or to synchronize with external manipulation of the termBuffer.
        /// Note: to grow the size of the array use {@link #resizeTermBuffer(int)} first.
		/// </summary>
        /// <param name="length">the truncated length</param>
		public void  SetTermLength(int length)
		{
			InitTermBuffer();
            if (length > termBuffer.Length)
                throw new ArgumentOutOfRangeException("length " + length + " exceeds the size of the termBuffer (" + termBuffer.Length + ")");
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
		/// last character corresponding to this token in the source text.  The length of the
        /// token in the source text is (endOffset - startOffset).
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

        ///
        /// <summary>
        /// EXPERIMENTAL:  While we think this is here to stay, we may want to change it to be a long.
        /// Get the bitset for any bits that have been set.  This is completely distinct from {@link #type()}, although they do share similar purposes.
        /// The flags can be used to encode information about the token for use by other {@link org.apache.lucene.analysis.TokenFilter}s.
        /// </summary>
        /// <returns>The bits</returns>
        public int GetFlags()
        {
            return flags;
        }

        ///
        /// <seealso cref="GetFlags()"/>
        ///
        public void SetFlags(int flags)
        {
            this.flags = flags;
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
            flags = 0;
			// startOffset = endOffset = 0;
			// type = DEFAULT_TYPE;
		}
		
		public virtual object Clone()
		{
			try
			{
				Token t = (Token) base.MemberwiseClone();
                // Do a deep clone
				if (termBuffer != null)
				{
					t.termBuffer = (char[]) termBuffer.Clone();
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

        /** Makes a clone, but replaces the term buffer &
         * start/end offset in the process.  This is more
         * efficient than doing a full clone (and then calling
         * setTermBuffer) because it saves a wasted copy of the old
         * termBuffer. */
        public Token Clone(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            Token t = new Token(newTermBuffer, newTermOffset, newTermLength, newStartOffset, newEndOffset);
            t.positionIncrement = positionIncrement;
            t.flags = flags;
            t.type = type;
            if (payload != null)
                t.payload = (Payload)payload.Clone();
            return t;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if (obj is Token)
            {
                Token other = (Token)obj;

                InitTermBuffer();
                other.InitTermBuffer();

                if (termLength == other.termLength &&
                    startOffset == other.startOffset &&
                    endOffset == other.endOffset &&
                    flags == other.flags &&
                    positionIncrement == other.positionIncrement &&
                    SubEqual(type, other.type) &&
                    SubEqual(payload, other.payload))
                {
                    for (int i = 0; i < termLength; i++)
                        if (termBuffer[i] != other.termBuffer[i])
                            return false;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        private bool SubEqual(object o1, object o2)
        {
            if (o1 == null)
                return o2 == null;
            else
                return o1.Equals(o2);
        }

        public override int GetHashCode()
        {
            InitTermBuffer();
            int code = termLength;
            code = code * 31 + startOffset;
            code = code * 31 + endOffset;
            code = code * 31 + flags;
            code = code * 31 + positionIncrement;
            code = code * 31 + type.GetHashCode();
            code = (payload == null ? code : code * 31 + payload.GetHashCode());
            code = code * 31 + ArrayUtil.HashCode(termBuffer, 0, termLength);
            return code;
        }

        // like clear() but doesn't clear termBuffer/text
        private void ClearNoTermBuffer()
        {
            payload = null;
            positionIncrement = 1;
            flags = 0;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #setTermBuffer(char[], int, int)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset},
         *  {@link #setType}
         *  @return this Token instance */
        public Token Reinit(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset, String newType)
        {
            ClearNoTermBuffer();
            payload = null;
            positionIncrement = 1;
            SetTermBuffer(newTermBuffer, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #SetTermBuffer(char[], int, int)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset}
         *  {@link #setType} on Token.DEFAULT_TYPE
         *  @return this Token instance */
        public Token Reinit(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            ClearNoTermBuffer();
            SetTermBuffer(newTermBuffer, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = DEFAULT_TYPE;
            return this;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #SetTermBuffer(String)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset}
         *  {@link #setType}
         *  @return this Token instance */
        public Token Reinit(String newTerm, int newStartOffset, int newEndOffset, String newType)
        {
            ClearNoTermBuffer();
            SetTermBuffer(newTerm);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #SetTermBuffer(String, int, int)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset}
         *  {@link #setType}
         *  @return this Token instance */
        public Token Reinit(String newTerm, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset, String newType)
        {
            ClearNoTermBuffer();
            SetTermBuffer(newTerm, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #SetTermBuffer(String)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset}
         *  {@link #setType} on Token.DEFAULT_TYPE
         *  @return this Token instance */
        public Token Reinit(String newTerm, int newStartOffset, int newEndOffset)
        {
            ClearNoTermBuffer();
            SetTermBuffer(newTerm);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = DEFAULT_TYPE;
            return this;
        }

        /** Shorthand for calling {@link #clear},
         *  {@link #SetTermBuffer(String, int, int)},
         *  {@link #setStartOffset},
         *  {@link #setEndOffset}
         *  {@link #setType} on Token.DEFAULT_TYPE
         *  @return this Token instance */
        public Token Reinit(String newTerm, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            ClearNoTermBuffer();
            SetTermBuffer(newTerm, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = DEFAULT_TYPE;
            return this;
        }

        /**
         * Copy the prototype token's fields into this one. Note: Payloads are shared.
         * @param prototype
         */
        public void Reinit(Token prototype)
        {
            prototype.InitTermBuffer();
            SetTermBuffer(prototype.termBuffer, 0, prototype.termLength);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        /**
         * Copy the prototype token's fields into this one, with a different term. Note: Payloads are shared.
         * @param prototype
         * @param newTerm
         */
        public void Reinit(Token prototype, String newTerm)
        {
            SetTermBuffer(newTerm);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        /**
         * Copy the prototype token's fields into this one, with a different term. Note: Payloads are shared.
         * @param prototype
         * @param newTermBuffer
         * @param offset
         * @param length
         */
        public void Reinit(Token prototype, char[] newTermBuffer, int offset, int length)
        {
            SetTermBuffer(newTermBuffer, offset, length);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }
    }
}