using Lucene.Net.Index;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Analysis.Tokenattributes;
    using System.Reflection;
    using Attribute = Lucene.Net.Util.Attribute;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using IAttributeReflector = Lucene.Net.Util.IAttributeReflector;

    /// <summary>
    ///  A Token is an occurrence of a term from the text of a field.  It consists of
    ///  a term's text, the start and end offset of the term in the text of the field,
    ///  and a type string.
    ///  <p>
    ///  The start and end offsets permit applications to re-associate a token with
    ///  its source text, e.g., to display highlighted query terms in a document
    ///  browser, or to show matching text fragments in a <abbr title="KeyWord In Context">KWIC</abbr>
    ///  display, etc.
    ///  <p>
    ///  The type is a string, assigned by a lexical analyzer
    ///  (a.k.a. tokenizer), naming the lexical or syntactic class that the token
    ///  belongs to.  For example an end of sentence marker token might be implemented
    ///  with type "eos".  The default token type is "word".
    ///  <p>
    ///  A Token can optionally have metadata (a.k.a. payload) in the form of a variable
    ///  length byte array. Use <seealso cref="DocsAndPositionsEnum#getPayload()"/> to retrieve the
    ///  payloads from the index.
    ///
    ///  <br><br>
    ///
    ///  <p><b>NOTE:</b> As of 2.9, Token implements all <seealso cref="Attribute"/> interfaces
    ///  that are part of core Lucene and can be found in the {@code tokenattributes} subpackage.
    ///  Even though it is not necessary to use Token anymore, with the new TokenStream API it can
    ///  be used as convenience class that implements all <seealso cref="Attribute"/>s, which is especially useful
    ///  to easily switch from the old to the new TokenStream API.
    ///
    ///  <br><br>
    ///
    ///  <p>Tokenizers and TokenFilters should try to re-use a Token
    ///  instance when possible for best performance, by
    ///  implementing the <seealso cref="TokenStream#IncrementToken()"/> API.
    ///  Failing that, to create a new Token you should first use
    ///  one of the constructors that starts with null text.  To load
    ///  the token from a char[] use <seealso cref="#copyBuffer(char[], int, int)"/>.
    ///  To load from a String use <seealso cref="#SetEmpty()"/> followed by <seealso cref="#append(CharSequence)"/> or <seealso cref="#append(CharSequence, int, int)"/>.
    ///  Alternatively you can get the Token's termBuffer by calling either <seealso cref="#buffer()"/>,
    ///  if you know that your text is shorter than the capacity of the termBuffer
    ///  or <seealso cref="#resizeBuffer(int)"/>, if there is any possibility
    ///  that you may need to grow the buffer. Fill in the characters of your term into this
    ///  buffer, with <seealso cref="string#getChars(int, int, char[], int)"/> if loading from a string,
    ///  or with <seealso cref="System#arraycopy(Object, int, Object, int, int)"/>, and finally call <seealso cref="#setLength(int)"/> to
    ///  set the length of the term text.  See <a target="_top"
    ///  href="https://issues.apache.org/jira/browse/LUCENE-969">LUCENE-969</a>
    ///  for details.</p>
    ///  <p>Typical Token reuse patterns:
    ///  <ul>
    ///  <li> Copying text from a string (type is reset to <seealso cref="#DEFAULT_TYPE"/> if not specified):<br/>
    ///  <pre class="prettyprint">
    ///    return reusableToken.reinit(string, startOffset, endOffset[, type]);
    ///  </pre>
    ///  </li>
    ///  <li> Copying some text from a string (type is reset to <seealso cref="#DEFAULT_TYPE"/> if not specified):<br/>
    ///  <pre class="prettyprint">
    ///    return reusableToken.reinit(string, 0, string.length(), startOffset, endOffset[, type]);
    ///  </pre>
    ///  </li>
    ///  </li>
    ///  <li> Copying text from char[] buffer (type is reset to <seealso cref="#DEFAULT_TYPE"/> if not specified):<br/>
    ///  <pre class="prettyprint">
    ///    return reusableToken.reinit(buffer, 0, buffer.length, startOffset, endOffset[, type]);
    ///  </pre>
    ///  </li>
    ///  <li> Copying some text from a char[] buffer (type is reset to <seealso cref="#DEFAULT_TYPE"/> if not specified):<br/>
    ///  <pre class="prettyprint">
    ///    return reusableToken.reinit(buffer, start, end - start, startOffset, endOffset[, type]);
    ///  </pre>
    ///  </li>
    ///  <li> Copying from one one Token to another (type is reset to <seealso cref="#DEFAULT_TYPE"/> if not specified):<br/>
    ///  <pre class="prettyprint">
    ///    return reusableToken.reinit(source.buffer(), 0, source.length(), source.StartOffset(), source.EndOffset()[, source.type()]);
    ///  </pre>
    ///  </li>
    ///  </ul>
    ///  A few things to note:
    ///  <ul>
    ///  <li>clear() initializes all of the fields to default values. this was changed in contrast to Lucene 2.4, but should affect no one.</li>
    ///  <li>Because <code>TokenStreams</code> can be chained, one cannot assume that the <code>Token's</code> current type is correct.</li>
    ///  <li>The startOffset and endOffset represent the start and offset in the source text, so be careful in adjusting them.</li>
    ///  <li>When caching a reusable token, clone it. When injecting a cached token into a stream that can be reset, clone it again.</li>
    ///  </ul>
    ///  </p>
    ///  <p>
    ///  <b>Please note:</b> With Lucene 3.1, the <code><seealso cref="#toString toString()"/></code> method had to be changed to match the
    ///  <seealso cref="CharSequence"/> interface introduced by the interface <seealso cref="Lucene.Net.Analysis.tokenattributes.CharTermAttribute"/>.
    ///  this method now only prints the term text, no additional information anymore.
    ///  </p>
    /// </summary>
    public class Token : CharTermAttribute, ITypeAttribute, IPositionIncrementAttribute, IFlagsAttribute, IOffsetAttribute, IPayloadAttribute, IPositionLengthAttribute
    {
        private int startOffset, endOffset;
        private string type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
        private int flags;
        private BytesRef payload;
        private int positionIncrement = 1;
        private int positionLength = 1;

        /// <summary>
        /// Constructs a Token will null text. </summary>
        public Token()
        {
        }

        /// <summary>
        /// Constructs a Token with null text and start & end
        ///  offsets. </summary>
        ///  <param name="start"> start offset in the source text </param>
        ///  <param name="end"> end offset in the source text  </param>
        public Token(int start, int end)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
        }

        /// <summary>
        /// Constructs a Token with null text and start & end
        ///  offsets plus the Token type. </summary>
        ///  <param name="start"> start offset in the source text </param>
        ///  <param name="end"> end offset in the source text </param>
        ///  <param name="typ"> the lexical type of this Token  </param>
        public Token(int start, int end, string typ)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
            type = typ;
        }

        /// <summary>
        /// Constructs a Token with null text and start & end
        ///  offsets plus flags. NOTE: flags is EXPERIMENTAL. </summary>
        ///  <param name="start"> start offset in the source text </param>
        ///  <param name="end"> end offset in the source text </param>
        ///  <param name="flags"> The bits to set for this token </param>
        public Token(int start, int end, int flags)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
            this.flags = flags;
        }

        /// <summary>
        /// Constructs a Token with the given term text, and start
        ///  & end offsets.  The type defaults to "word."
        ///  <b>NOTE:</b> for better indexing speed you should
        ///  instead use the char[] termBuffer methods to set the
        ///  term text. </summary>
        ///  <param name="text"> term text </param>
        ///  <param name="start"> start offset in the source text </param>
        ///  <param name="end"> end offset in the source text </param>
        public Token(string text, int start, int end)
        {
            CheckOffsets(start, end);
            Append(text);
            startOffset = start;
            endOffset = end;
        }

        /// <summary>
        /// Constructs a Token with the given text, start and end
        ///  offsets, & type.  <b>NOTE:</b> for better indexing
        ///  speed you should instead use the char[] termBuffer
        ///  methods to set the term text. </summary>
        ///  <param name="text"> term text </param>
        ///  <param name="start"> start offset in the source text </param>
        ///  <param name="end"> end offset in the source text </param>
        ///  <param name="typ"> token type </param>
        public Token(string text, int start, int end, string typ)
        {
            CheckOffsets(start, end);
            Append(text);
            startOffset = start;
            endOffset = end;
            type = typ;
        }

        /// <summary>
        ///  Constructs a Token with the given text, start and end
        ///  offsets, & type.  <b>NOTE:</b> for better indexing
        ///  speed you should instead use the char[] termBuffer
        ///  methods to set the term text. </summary>
        /// <param name="text"> term text </param>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        /// <param name="flags"> token type bits </param>
        public Token(string text, int start, int end, int flags)
        {
            CheckOffsets(start, end);
            Append(text);
            startOffset = start;
            endOffset = end;
            this.flags = flags;
        }

        /// <summary>
        ///  Constructs a Token with the given term buffer (offset
        ///  & length), start and end
        ///  offsets </summary>
        /// <param name="startTermBuffer"> buffer containing term text </param>
        /// <param name="termBufferOffset"> the index in the buffer of the first character </param>
        /// <param name="termBufferLength"> number of valid characters in the buffer </param>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        public Token(char[] startTermBuffer, int termBufferOffset, int termBufferLength, int start, int end)
        {
            CheckOffsets(start, end);
            CopyBuffer(startTermBuffer, termBufferOffset, termBufferLength);
            startOffset = start;
            endOffset = end;
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= PositionIncrementAttribute </seealso>
        public int PositionIncrement
        {
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("Increment must be zero or greater: " + value);
                }
                this.positionIncrement = value;
            }
            get
            {
                return positionIncrement;
            }
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= PositionLengthAttribute </seealso>
        public int PositionLength
        {
            set
            {
                this.positionLength = value;
            }
            get
            {
                return positionLength;
            }
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= OffsetAttribute </seealso>
        public int StartOffset()
        {
            return startOffset;
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= OffsetAttribute </seealso>
        public int EndOffset()
        {
            return endOffset;
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= OffsetAttribute </seealso>
        public void SetOffset(int startOffset, int endOffset)
        {
            CheckOffsets(startOffset, endOffset);
            this.startOffset = startOffset;
            this.endOffset = endOffset;
        }

        /// <summary>Returns this Token's lexical type.  Defaults to "word". </summary>
        public string Type
        {
            get { return type; }
            set { this.type = value; }
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= FlagsAttribute </seealso>
        public int Flags
        {
            get
            {
                return flags;
            }
            set
            {
                this.flags = value;
            }
        }

        /// <summary>
        /// {@inheritDoc} </summary>
        /// <seealso cref= PayloadAttribute </seealso>
        public BytesRef Payload
        {
            get
            {
                return this.payload;
            }
            set
            {
                this.payload = value;
            }
        }

        /// <summary>
        /// Resets the term text, payload, flags, and positionIncrement,
        /// startOffset, endOffset and token type to default.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            payload = null;
            positionIncrement = 1;
            flags = 0;
            startOffset = endOffset = 0;
            type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
        }

        public override object Clone()
        {
            var t = (Token)base.Clone();
            // Do a deep clone
            if (payload != null)
            {
                t.payload = (BytesRef)payload.Clone();
            }
            return t;
        }

        /// <summary>
        /// Makes a clone, but replaces the term buffer &
        /// start/end offset in the process.  this is more
        /// efficient than doing a full clone (and then calling
        /// <seealso cref="#copyBuffer"/>) because it saves a wasted copy of the old
        /// termBuffer.
        /// </summary>
        public virtual Token Clone(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            var t = new Token(newTermBuffer, newTermOffset, newTermLength, newStartOffset, newEndOffset)
            {
                positionIncrement = positionIncrement,
                flags = flags,
                type = type
            };
            if (payload != null)
            {
                t.payload = (BytesRef)payload.Clone();
            }
            return t;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            var other = obj as Token;
            if (other != null)
            {
                return (startOffset == other.startOffset && endOffset == other.endOffset && flags == other.flags && positionIncrement == other.positionIncrement && (type == null ? other.type == null : type.Equals(other.type)) && (payload == null ? other.payload == null : payload.Equals(other.payload)) && base.Equals(obj));
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            int code = base.GetHashCode();
            code = code * 31 + startOffset;
            code = code * 31 + endOffset;
            code = code * 31 + flags;
            code = code * 31 + positionIncrement;
            if (type != null)
            {
                code = code * 31 + type.GetHashCode();
            }
            if (payload != null)
            {
                code = code * 31 + payload.GetHashCode();
            }
            return code;
        }

        // like clear() but doesn't clear termBuffer/text
        private void ClearNoTermBuffer()
        {
            payload = null;
            positionIncrement = 1;
            flags = 0;
            startOffset = endOffset = 0;
            type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#copyBuffer(char[], int, int)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset, string newType)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            ClearNoTermBuffer();
            CopyBuffer(newTermBuffer, newTermOffset, newTermLength);
            payload = null;
            positionIncrement = 1;
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#copyBuffer(char[], int, int)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> on Token.DEFAULT_TYPE </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            ClearNoTermBuffer();
            CopyBuffer(newTermBuffer, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#append(CharSequence)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(string newTerm, int newStartOffset, int newEndOffset, string newType)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#append(CharSequence, int, int)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(string newTerm, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset, string newType)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm, newTermOffset, newTermOffset + newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = newType;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#append(CharSequence)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> on Token.DEFAULT_TYPE </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(string newTerm, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <seealso cref="#clear"/>,
        ///  <seealso cref="#append(CharSequence, int, int)"/>,
        ///  <seealso cref="#setOffset"/>,
        ///  <seealso cref="#setType"/> on Token.DEFAULT_TYPE </summary>
        ///  <returns> this Token instance  </returns>
        public virtual Token Reinit(string newTerm, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm, newTermOffset, newTermOffset + newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = Tokenattributes.TypeAttribute_Fields.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Copy the prototype token's fields into this one. Note: Payloads are shared. </summary>
        /// <param name="prototype"> source Token to copy fields from </param>
        public virtual void Reinit(Token prototype)
        {
            CopyBuffer(prototype.Buffer(), 0, prototype.Length);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        /// <summary>
        /// Copy the prototype token's fields into this one, with a different term. Note: Payloads are shared. </summary>
        /// <param name="prototype"> existing Token </param>
        /// <param name="newTerm"> new term text </param>
        public virtual void Reinit(Token prototype, string newTerm)
        {
            SetEmpty().Append(newTerm);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        /// <summary>
        /// Copy the prototype token's fields into this one, with a different term. Note: Payloads are shared. </summary>
        /// <param name="prototype"> existing Token </param>
        /// <param name="newTermBuffer"> buffer containing new term text </param>
        /// <param name="offset"> the index in the buffer of the first character </param>
        /// <param name="length"> number of valid characters in the buffer </param>
        public virtual void Reinit(Token prototype, char[] newTermBuffer, int offset, int length)
        {
            CopyBuffer(newTermBuffer, offset, length);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        public override void CopyTo(Attribute target)
        {
            var to = target as Token;
            if (to != null)
            {
                to.Reinit(this);
                // reinit shares the payload, so clone it:
                if (payload != null)
                {
                    to.payload = (BytesRef)payload.Clone();
                }
            }
            else
            {
                base.CopyTo(target);
                ((IOffsetAttribute)target).SetOffset(startOffset, endOffset);
                ((IPositionIncrementAttribute)target).PositionIncrement = positionIncrement;
                ((IPayloadAttribute)target).Payload = (payload == null) ? null : (BytesRef)payload.Clone();
                ((IFlagsAttribute)target).Flags = flags;
                ((ITypeAttribute)target).Type = type;
            }
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            base.ReflectWith(reflector);
            reflector.Reflect(typeof(IOffsetAttribute), "startOffset", startOffset);
            reflector.Reflect(typeof(IOffsetAttribute), "endOffset", endOffset);
            reflector.Reflect(typeof(IPositionIncrementAttribute), "positionIncrement", positionIncrement);
            reflector.Reflect(typeof(IPayloadAttribute), "payload", payload);
            reflector.Reflect(typeof(IFlagsAttribute), "flags", flags);
            reflector.Reflect(typeof(ITypeAttribute), "type", type);
        }

        private void CheckOffsets(int startOffset, int endOffset)
        {
            if (startOffset < 0 || endOffset < startOffset)
            {
                throw new System.ArgumentException("startOffset must be non-negative, and endOffset must be >= startOffset, " + "startOffset=" + startOffset + ",endOffset=" + endOffset);
            }
        }

        /// <summary>
        /// Convenience factory that returns <code>Token</code> as implementation for the basic
        /// attributes and return the default impl (with &quot;Impl&quot; appended) for all other
        /// attributes.
        /// @since 3.0
        /// </summary>
        public static readonly AttributeSource.AttributeFactory TOKEN_ATTRIBUTE_FACTORY = new TokenAttributeFactory(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY);

        /// <summary>
        /// <b>Expert:</b> Creates a TokenAttributeFactory returning <seealso cref="Token"/> as instance for the basic attributes
        /// and for all other attributes calls the given delegate factory.
        /// @since 3.0
        /// </summary>
        public sealed class TokenAttributeFactory : AttributeSource.AttributeFactory
        {
            internal readonly AttributeSource.AttributeFactory @delegate;

            /// <summary>
            /// <b>Expert</b>: Creates an AttributeFactory returning <seealso cref="Token"/> as instance for the basic attributes
            /// and for all other attributes calls the given delegate factory.
            /// </summary>
            public TokenAttributeFactory(AttributeSource.AttributeFactory @delegate)
            {
                this.@delegate = @delegate;
            }

            public override Attribute CreateAttributeInstance<T>()
            {
                var attClass = typeof(T);
                return attClass.GetTypeInfo().IsAssignableFrom(typeof(Token).GetTypeInfo()) ? new Token() : @delegate.CreateAttributeInstance<T>();
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }

                var af = other as TokenAttributeFactory;
                if (af != null)
                {
                    return this.@delegate.Equals(af.@delegate);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return @delegate.GetHashCode() ^ 0x0a45aa31;
            }
        }
    }
}