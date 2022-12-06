using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Reflection;
using Attribute = Lucene.Net.Util.Attribute;
using AttributeSource = Lucene.Net.Util.AttributeSource;
using BytesRef = Lucene.Net.Util.BytesRef;
using IAttribute = Lucene.Net.Util.IAttribute;

namespace Lucene.Net.Analysis
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

    using CharTermAttribute = Lucene.Net.Analysis.TokenAttributes.CharTermAttribute;
    using IAttributeReflector = Lucene.Net.Util.IAttributeReflector;

    /// <summary>
    /// A <see cref="Token"/> is an occurrence of a term from the text of a field.  It consists of
    /// a term's text, the start and end offset of the term in the text of the field,
    /// and a type string.
    /// <para/>
    /// The start and end offsets permit applications to re-associate a token with
    /// its source text, e.g., to display highlighted query terms in a document
    /// browser, or to show matching text fragments in a KWIC (KeyWord In Context)
    /// display, etc.
    /// <para/>
    /// The type is a string, assigned by a lexical analyzer
    /// (a.k.a. tokenizer), naming the lexical or syntactic class that the token
    /// belongs to.  For example an end of sentence marker token might be implemented
    /// with type "eos".  The default token type is "word".
    /// <para/>
    /// A Token can optionally have metadata (a.k.a. payload) in the form of a variable
    /// length byte array. Use <see cref="Index.DocsAndPositionsEnum.GetPayload()"/> to retrieve the
    /// payloads from the index.
    ///
    /// <para/><para/>
    ///
    /// <para/><b>NOTE:</b> As of 2.9, Token implements all <see cref="IAttribute"/> interfaces
    /// that are part of core Lucene and can be found in the <see cref="TokenAttributes"/> namespace.
    /// Even though it is not necessary to use <see cref="Token"/> anymore, with the new <see cref="TokenStream"/> API it can
    /// be used as convenience class that implements all <see cref="IAttribute"/>s, which is especially useful
    /// to easily switch from the old to the new <see cref="TokenStream"/> API.
    ///
    /// <para/><para/>
    ///
    /// <para><see cref="Tokenizer"/>s and <see cref="TokenFilter"/>s should try to re-use a <see cref="Token"/>
    /// instance when possible for best performance, by
    /// implementing the <see cref="TokenStream.IncrementToken()"/> API.
    /// Failing that, to create a new <see cref="Token"/> you should first use
    /// one of the constructors that starts with null text.  To load
    /// the token from a char[] use <see cref="ICharTermAttribute.CopyBuffer(char[], int, int)"/>.
    /// To load from a <see cref="string"/> use <see cref="ICharTermAttribute.SetEmpty()"/> followed by 
    /// <see cref="ICharTermAttribute.Append(string)"/> or <see cref="ICharTermAttribute.Append(string, int, int)"/>.
    /// Alternatively you can get the <see cref="Token"/>'s termBuffer by calling either <see cref="ICharTermAttribute.Buffer"/>,
    /// if you know that your text is shorter than the capacity of the termBuffer
    /// or <see cref="ICharTermAttribute.ResizeBuffer(int)"/>, if there is any possibility
    /// that you may need to grow the buffer. Fill in the characters of your term into this
    /// buffer, with <see cref="string.ToCharArray(int, int)"/> if loading from a string,
    /// or with <see cref="System.Array.Copy(System.Array, int, System.Array, int, int)"/>, 
    /// and finally call <see cref="ICharTermAttribute.SetLength(int)"/> to
    /// set the length of the term text.  See <a target="_top"
    /// href="https://issues.apache.org/jira/browse/LUCENE-969">LUCENE-969</a>
    /// for details.</para>
    /// <para>Typical Token reuse patterns:
    /// <list type="bullet">
    ///     <item><description> Copying text from a string (type is reset to <see cref="TypeAttribute.DEFAULT_TYPE"/> if not specified):
    ///     <code>
    ///         return reusableToken.Reinit(string, startOffset, endOffset[, type]);
    ///     </code>
    ///     </description></item>
    ///     <item><description> Copying some text from a string (type is reset to <see cref="TypeAttribute.DEFAULT_TYPE"/> if not specified):
    ///     <code>
    ///         return reusableToken.Reinit(string, 0, string.Length, startOffset, endOffset[, type]);
    ///     </code>
    ///     </description></item>
    ///     <item><description> Copying text from char[] buffer (type is reset to <see cref="TypeAttribute.DEFAULT_TYPE"/> if not specified):
    ///     <code>
    ///         return reusableToken.Reinit(buffer, 0, buffer.Length, startOffset, endOffset[, type]);
    ///     </code>
    ///     </description></item>
    ///     <item><description> Copying some text from a char[] buffer (type is reset to <see cref="TypeAttribute.DEFAULT_TYPE"/> if not specified):
    ///     <code>
    ///         return reusableToken.Reinit(buffer, start, end - start, startOffset, endOffset[, type]);
    ///     </code>
    ///     </description></item>
    ///     <item><description> Copying from one one <see cref="Token"/> to another (type is reset to <see cref="TypeAttribute.DEFAULT_TYPE"/> if not specified):
    ///     <code>
    ///         return reusableToken.Reinit(source.Buffer, 0, source.Length, source.StartOffset, source.EndOffset[, source.Type]);
    ///     </code>
    ///     </description></item>
    /// </list>
    /// A few things to note:
    /// <list type="bullet">
    ///     <item><description><see cref="Clear()"/> initializes all of the fields to default values. this was changed in contrast to Lucene 2.4, but should affect no one.</description></item>
    ///     <item><description>Because <see cref="TokenStream"/>s can be chained, one cannot assume that the <see cref="Token"/>'s current type is correct.</description></item>
    ///     <item><description>The startOffset and endOffset represent the start and offset in the source text, so be careful in adjusting them.</description></item>
    ///     <item><description>When caching a reusable token, clone it. When injecting a cached token into a stream that can be reset, clone it again.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Please note:</b> With Lucene 3.1, the <see cref="CharTermAttribute.ToString()"/> method had to be changed to match the
    /// <see cref="ICharSequence"/> interface introduced by the interface <see cref="ICharTermAttribute"/>.
    /// this method now only prints the term text, no additional information anymore.
    /// </para>
    /// </summary>
    public class Token : CharTermAttribute, ITypeAttribute, IPositionIncrementAttribute, IFlagsAttribute, IOffsetAttribute, IPayloadAttribute, IPositionLengthAttribute
    {
        private int startOffset, endOffset;
        private string type = TypeAttribute.DEFAULT_TYPE;
        private int flags;
        private BytesRef payload;
        private int positionIncrement = 1;
        private int positionLength = 1;

        /// <summary>
        /// Constructs a <see cref="Token"/> will null text. </summary>
        public Token()
        {
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with null text and start &amp; end
        /// offsets. </summary>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text  </param>
        public Token(int start, int end)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with null text and start &amp; end
        /// offsets plus the <see cref="Token"/> type. </summary>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        /// <param name="typ"> the lexical type of this <see cref="Token"/>  </param>
        public Token(int start, int end, string typ)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
            type = typ;
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with null text and start &amp; end
        /// offsets plus flags. NOTE: flags is EXPERIMENTAL. </summary>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        /// <param name="flags"> The bits to set for this token </param>
        public Token(int start, int end, int flags)
        {
            CheckOffsets(start, end);
            startOffset = start;
            endOffset = end;
            this.flags = flags;
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with the given term text, and start
        /// &amp; end offsets.  The type defaults to "word."
        /// <b>NOTE:</b> for better indexing speed you should
        /// instead use the char[] termBuffer methods to set the
        /// term text. </summary>
        /// <param name="text"> term text </param>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        public Token(string text, int start, int end)
        {
            CheckOffsets(start, end);
            Append(text);
            startOffset = start;
            endOffset = end;
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with the given text, start and end
        /// offsets, &amp; type.  <b>NOTE:</b> for better indexing
        /// speed you should instead use the char[] termBuffer
        /// methods to set the term text. </summary>
        /// <param name="text"> term text </param>
        /// <param name="start"> start offset in the source text </param>
        /// <param name="end"> end offset in the source text </param>
        /// <param name="typ"> token type </param>
        public Token(string text, int start, int end, string typ)
        {
            CheckOffsets(start, end);
            Append(text);
            startOffset = start;
            endOffset = end;
            type = typ;
        }

        /// <summary>
        /// Constructs a <see cref="Token"/> with the given text, start and end
        /// offsets, &amp; type.  <b>NOTE:</b> for better indexing
        /// speed you should instead use the char[] termBuffer
        /// methods to set the term text. </summary>
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
        /// Constructs a <see cref="Token"/> with the given term buffer (offset
        /// &amp; length), start and end offsets
        /// </summary>
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
        /// Gets or Sets the position increment (the distance from the prior term). The default value is one.
        /// </summary>
        /// <exception cref="ArgumentException"> if value is set to a negative value. </exception>
        /// <seealso cref="IPositionIncrementAttribute"/>
        public virtual int PositionIncrement
        {
            get => positionIncrement;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(PositionIncrement), "Increment must be zero or greater: " + value); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.positionIncrement = value;
            }
        }

        /// <summary>
        /// Gets or Sets the position length of this <see cref="Token"/> (how many positions this token
        /// spans).
        /// <para/>
        /// The default value is one.
        /// </summary>
        /// <exception cref="ArgumentException"> if value
        ///         is set to zero or negative. </exception>
        /// <seealso cref="IPositionLengthAttribute"/>
        public virtual int PositionLength
        {
            get => positionLength;
            set => this.positionLength = value;
        }

        /// <summary>
        /// Returns this <see cref="Token"/>'s starting offset, the position of the first character
        /// corresponding to this token in the source text.
        /// <para/>
        /// Note that the difference between <see cref="EndOffset"/> and <see cref="StartOffset"/>
        /// may not be equal to termText.Length, as the term text may have been altered by a
        /// stemmer or some other filter.
        /// </summary>
        /// <seealso cref="SetOffset(int, int)"/>
        /// <seealso cref="IOffsetAttribute"/>
        public int StartOffset => startOffset;

        /// <summary>
        /// Returns this <see cref="Token"/>'s ending offset, one greater than the position of the
        /// last character corresponding to this token in the source text. The length
        /// of the token in the source text is (<code>EndOffset</code> - <see cref="StartOffset"/>).
        /// </summary>
        /// <seealso cref="SetOffset(int, int)"/>
        /// <seealso cref="IOffsetAttribute"/>
        public int EndOffset => endOffset;

        /// <summary>
        /// Set the starting and ending offset.
        /// </summary>
        /// <exception cref="ArgumentException"> If <paramref name="startOffset"/> or <paramref name="endOffset"/>
        ///         are negative, or if <paramref name="startOffset"/> is greater than
        ///         <paramref name="endOffset"/> </exception>
        /// <seealso cref="StartOffset"/>
        /// <seealso cref="EndOffset"/>
        /// <seealso cref="IOffsetAttribute"/>
        public virtual void SetOffset(int startOffset, int endOffset)
        {
            CheckOffsets(startOffset, endOffset);
            this.startOffset = startOffset;
            this.endOffset = endOffset;
        }

        /// <summary>Gets or Sets this <see cref="Token"/>'s lexical type.  Defaults to "word". </summary>
        public string Type
        {
            get => type;
            set => this.type = value;
        }

        /// <summary>
        /// Get the bitset for any bits that have been set.
        /// <para/>
        /// This is completely distinct from <see cref="ITypeAttribute.Type" />, although they do share similar purposes.
        /// The flags can be used to encode information about the token for use by other <see cref="Lucene.Net.Analysis.TokenFilter" />s.
        /// </summary>
        /// <seealso cref="IFlagsAttribute"/>
        public virtual int Flags
        {
            get => flags;
            set => this.flags = value;
        }

        /// <summary>
        /// Gets or Sets this <see cref="Token"/>'s payload.
        /// </summary>
        /// <seealso cref="IPayloadAttribute"/>
        public virtual BytesRef Payload
        {
            get => this.payload;
            set => this.payload = value;
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
            type = TokenAttributes.TypeAttribute.DEFAULT_TYPE;
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
        /// Makes a clone, but replaces the term buffer &amp;
        /// start/end offset in the process.  This is more
        /// efficient than doing a full clone (and then calling
        /// <see cref="ICharTermAttribute.CopyBuffer"/>) because it saves a wasted copy of the old
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


            if (obj is Token other)
            {
                return (startOffset == other.startOffset &&
                    endOffset == other.endOffset &&
                    flags == other.flags &&
                    positionIncrement == other.positionIncrement &&
                    (type is null ? other.type is null : type.Equals(other.type, StringComparison.Ordinal)) &&
                    (payload is null ? other.payload is null : payload.Equals(other.payload)) &&
                    base.Equals(obj)
                );
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
            type = TokenAttributes.TypeAttribute.DEFAULT_TYPE;
        }

        /// <summary>
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.CopyBuffer(char[], int, int)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
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
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.CopyBuffer(char[], int, int)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) on <see cref="TypeAttribute.DEFAULT_TYPE"/> </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
        public virtual Token Reinit(char[] newTermBuffer, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            ClearNoTermBuffer();
            CopyBuffer(newTermBuffer, newTermOffset, newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = TokenAttributes.TypeAttribute.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.Append(string)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
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
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.Append(string, int, int)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
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
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.Append(string)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) on <see cref="TypeAttribute.DEFAULT_TYPE"/> </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
        public virtual Token Reinit(string newTerm, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = TokenAttributes.TypeAttribute.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Shorthand for calling <see cref="Clear"/>,
        /// <see cref="ICharTermAttribute.Append(string, int, int)"/>,
        /// <see cref="SetOffset"/>,
        /// <see cref="Type"/> (set) on <see cref="TypeAttribute.DEFAULT_TYPE"/> </summary>
        /// <returns> this <see cref="Token"/> instance  </returns>
        public virtual Token Reinit(string newTerm, int newTermOffset, int newTermLength, int newStartOffset, int newEndOffset)
        {
            CheckOffsets(newStartOffset, newEndOffset);
            Clear();
            Append(newTerm, newTermOffset, newTermOffset + newTermLength);
            startOffset = newStartOffset;
            endOffset = newEndOffset;
            type = TokenAttributes.TypeAttribute.DEFAULT_TYPE;
            return this;
        }

        /// <summary>
        /// Copy the prototype token's fields into this one. Note: Payloads are shared. </summary>
        /// <param name="prototype"> source <see cref="Token"/> to copy fields from </param>
        public virtual void Reinit(Token prototype)
        {
            CopyBuffer(prototype.Buffer, 0, prototype.Length);
            positionIncrement = prototype.positionIncrement;
            flags = prototype.flags;
            startOffset = prototype.startOffset;
            endOffset = prototype.endOffset;
            type = prototype.type;
            payload = prototype.payload;
        }

        /// <summary>
        /// Copy the prototype token's fields into this one, with a different term. Note: Payloads are shared. </summary>
        /// <param name="prototype"> existing <see cref="Token"/> </param>
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
        /// <param name="prototype"> existing <see cref="Token"/> </param>
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

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            if (target is Token to)
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
                ((IPayloadAttribute)target).Payload = (payload is null) ? null : (BytesRef)payload.Clone();
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

        private static void CheckOffsets(int startOffset, int endOffset) // LUCENENET: CA1822: Mark members as static
        {
            if (startOffset < 0 || endOffset < startOffset)
            {
                throw new ArgumentException("startOffset must be non-negative, and endOffset must be >= startOffset, " + "startOffset=" + startOffset + ",endOffset=" + endOffset);
            }
        }

        /// <summary>
        /// Convenience factory that returns <see cref="Token"/> as implementation for the basic
        /// attributes and return the default impl (with &quot;Impl&quot; appended) for all other
        /// attributes.
        /// @since 3.0
        /// </summary>
        public static readonly AttributeSource.AttributeFactory TOKEN_ATTRIBUTE_FACTORY = new TokenAttributeFactory(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY);

        /// <summary>
        /// <b>Expert:</b> Creates a <see cref="TokenAttributeFactory"/> returning <see cref="Token"/> as instance for the basic attributes
        /// and for all other attributes calls the given delegate factory.
        /// @since 3.0
        /// </summary>
        public sealed class TokenAttributeFactory : AttributeSource.AttributeFactory
        {
            internal readonly AttributeSource.AttributeFactory @delegate;

            /// <summary>
            /// <b>Expert</b>: Creates an <see cref="AttributeSource.AttributeFactory"/> returning <see cref="Token"/> as instance for the basic attributes
            /// and for all other attributes calls the given delegate factory.
            /// </summary>
            public TokenAttributeFactory(AttributeSource.AttributeFactory @delegate)
            {
                this.@delegate = @delegate;
            }

            public override Attribute CreateAttributeInstance<T>()
            {
                var attClass = typeof(T);
                return attClass.IsAssignableFrom(typeof(Token)) ? new Token() : @delegate.CreateAttributeInstance<T>();
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }

                if (other is TokenAttributeFactory af)
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
