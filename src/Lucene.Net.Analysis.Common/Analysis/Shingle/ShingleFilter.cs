// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Shingle
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
    /// <para>A <see cref="ShingleFilter"/> constructs shingles (token n-grams) from a token stream.
    /// In other words, it creates combinations of tokens as a single token.
    /// 
    /// </para>
    /// <para>For example, the sentence "please divide this sentence into shingles"
    /// might be tokenized into shingles "please divide", "divide this",
    /// "this sentence", "sentence into", and "into shingles".
    /// 
    /// </para>
    /// <para>This filter handles position increments > 1 by inserting filler tokens
    /// (tokens with termtext "_"). It does not handle a position increment of 0.
    /// </para>
    /// </summary>
    public sealed class ShingleFilter : TokenFilter
    {
        /// <summary>
        /// filler token for when positionIncrement is more than 1
        /// </summary>
        public const string DEFAULT_FILLER_TOKEN = "_";

        /// <summary>
        /// default maximum shingle size is 2.
        /// </summary>
        public const int DEFAULT_MAX_SHINGLE_SIZE = 2;

        /// <summary>
        /// default minimum shingle size is 2.
        /// </summary>
        public const int DEFAULT_MIN_SHINGLE_SIZE = 2;

        /// <summary>
        /// default token type attribute value is "shingle" 
        /// </summary>
        public const string DEFAULT_TOKEN_TYPE = "shingle";

        /// <summary>
        /// The default string to use when joining adjacent tokens to form a shingle
        /// </summary>
        public const string DEFAULT_TOKEN_SEPARATOR = " ";

        /// <summary>
        /// The sequence of input stream tokens (or filler tokens, if necessary)
        /// that will be composed to form output shingles.
        /// </summary>
        private readonly Queue<InputWindowToken> inputWindow = new Queue<InputWindowToken>();

        /// <summary>
        /// The number of input tokens in the next output token.  This is the "n" in
        /// "token n-grams".
        /// </summary>
        private CircularSequence gramSize;

        /// <summary>
        /// Shingle and unigram text is composed here.
        /// </summary>
        private readonly StringBuilder gramBuilder = new StringBuilder();

        /// <summary>
        /// The token type attribute value to use - default is "shingle"
        /// </summary>
        private string tokenType = DEFAULT_TOKEN_TYPE;

        /// <summary>
        /// The string to use when joining adjacent tokens to form a shingle
        /// </summary>
        private string tokenSeparator = DEFAULT_TOKEN_SEPARATOR;

        /// <summary>
        /// The string to insert for each position at which there is no token
        /// (i.e., when position increment is greater than one).
        /// </summary>
        private char[] fillerToken = DEFAULT_FILLER_TOKEN.ToCharArray();

        /// <summary>
        /// By default, we output unigrams (individual tokens) as well as shingles
        /// (token n-grams).
        /// </summary>
        private bool outputUnigrams = true;

        /// <summary>
        /// By default, we don't override behavior of outputUnigrams.
        /// </summary>
        private bool outputUnigramsIfNoShingles = false;

        /// <summary>
        /// maximum shingle size (number of tokens)
        /// </summary>
        private int maxShingleSize;

        /// <summary>
        /// minimum shingle size (number of tokens)
        /// </summary>
        private int minShingleSize;

        /// <summary>
        /// The remaining number of filler tokens to be inserted into the input stream
        /// from which shingles are composed, to handle position increments greater
        /// than one.
        /// </summary>
        private int numFillerTokensToInsert;

        /// <summary>
        /// When the next input stream token has a position increment greater than
        /// one, it is stored in this field until sufficient filler tokens have been
        /// inserted to account for the position increment. 
        /// </summary>
        private AttributeSource nextInputStreamToken;

        /// <summary>
        /// Whether or not there is a next input stream token.
        /// </summary>
        private bool isNextInputStreamToken = false;

        /// <summary>
        /// Whether at least one unigram or shingle has been output at the current 
        /// position.
        /// </summary>
        private bool isOutputHere = false;

        /// <summary>
        /// true if no shingles have been output yet (for outputUnigramsIfNoShingles).
        /// </summary>
        private bool noShingleOutput = true;

        /// <summary>
        /// Holds the State after input.end() was called, so we can
        /// restore it in our end() impl.
        /// </summary>
        private State endState;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPositionLengthAttribute posLenAtt;
        private readonly ITypeAttribute typeAtt;


        /// <summary>
        /// Constructs a <see cref="ShingleFilter"/> with the specified shingle size from the
        /// <see cref="TokenStream"/> <paramref name="input"/>
        /// </summary>
        /// <param name="input"> input stream </param>
        /// <param name="minShingleSize"> minimum shingle size produced by the filter. </param>
        /// <param name="maxShingleSize"> maximum shingle size produced by the filter. </param>
        public ShingleFilter(TokenStream input, int minShingleSize, int maxShingleSize)
            : base(input)
        {
            SetMaxShingleSize(maxShingleSize);
            SetMinShingleSize(minShingleSize);
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = AddAttribute<IPositionLengthAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
        }

        /// <summary>
        /// Constructs a <see cref="ShingleFilter"/> with the specified shingle size from the
        /// <see cref="TokenStream"/> <paramref name="input"/>
        /// </summary>
        /// <param name="input"> input stream </param>
        /// <param name="maxShingleSize"> maximum shingle size produced by the filter. </param>
        public ShingleFilter(TokenStream input, int maxShingleSize)
            : this(input, DEFAULT_MIN_SHINGLE_SIZE, maxShingleSize)
        {
        }

        /// <summary>
        /// Construct a <see cref="ShingleFilter"/> with default shingle size: 2.
        /// </summary>
        /// <param name="input"> input stream </param>
        public ShingleFilter(TokenStream input)
            : this(input, DEFAULT_MIN_SHINGLE_SIZE, DEFAULT_MAX_SHINGLE_SIZE)
        {
        }

        /// <summary>
        /// Construct a <see cref="ShingleFilter"/> with the specified token type for shingle tokens
        /// and the default shingle size: 2
        /// </summary>
        /// <param name="input"> input stream </param>
        /// <param name="tokenType"> token type for shingle tokens </param>
        public ShingleFilter(TokenStream input, string tokenType)
            : this(input, DEFAULT_MIN_SHINGLE_SIZE, DEFAULT_MAX_SHINGLE_SIZE)
        {
            SetTokenType(tokenType);
        }

        /// <summary>
        /// Set the type of the shingle tokens produced by this filter.
        /// (default: "shingle")
        /// </summary>
        /// <param name="tokenType"> token tokenType </param>
        public void SetTokenType(string tokenType)
        {
            this.tokenType = tokenType;
        }

        /// <summary>
        /// Shall the output stream contain the input tokens (unigrams) as well as
        /// shingles? (default: true.)
        /// </summary>
        /// <param name="outputUnigrams"> Whether or not the output stream shall contain
        /// the input tokens (unigrams) </param>
        public void SetOutputUnigrams(bool outputUnigrams)
        {
            this.outputUnigrams = outputUnigrams;
            gramSize = new CircularSequence(this);
        }

        /// <summary>
        /// <para>Shall we override the behavior of outputUnigrams==false for those
        /// times when no shingles are available (because there are fewer than
        /// minShingleSize tokens in the input stream)? (default: false.)
        /// </para>
        /// <para>Note that if outputUnigrams==true, then unigrams are always output,
        /// regardless of whether any shingles are available.
        /// 
        /// </para>
        /// </summary>
        /// <param name="outputUnigramsIfNoShingles"> Whether or not to output a single
        /// unigram when no shingles are available. </param>
        public void SetOutputUnigramsIfNoShingles(bool outputUnigramsIfNoShingles)
        {
            this.outputUnigramsIfNoShingles = outputUnigramsIfNoShingles;
        }

        /// <summary>
        /// Set the max shingle size (default: 2)
        /// </summary>
        /// <param name="maxShingleSize"> max size of output shingles </param>
        public void SetMaxShingleSize(int maxShingleSize)
        {
            if (maxShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShingleSize), "Max shingle size must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.maxShingleSize = maxShingleSize;
        }

        /// <summary>
        /// <para>Set the min shingle size (default: 2).
        /// </para>
        /// <para>This method requires that the passed in minShingleSize is not greater
        /// than maxShingleSize, so make sure that maxShingleSize is set before
        /// calling this method.
        /// </para>
        /// <para>The unigram output option is independent of the min shingle size.
        /// 
        /// </para>
        /// </summary>
        /// <param name="minShingleSize"> min size of output shingles </param>
        public void SetMinShingleSize(int minShingleSize)
        {
            if (minShingleSize < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(minShingleSize), "Min shingle size must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minShingleSize > maxShingleSize)
            {
                throw new ArgumentException("Min shingle size must be <= max shingle size");
            }
            this.minShingleSize = minShingleSize;
            gramSize = new CircularSequence(this);
        }

        /// <summary>
        /// Sets the string to use when joining adjacent tokens to form a shingle </summary>
        /// <param name="tokenSeparator"> used to separate input stream tokens in output shingles </param>
        public void SetTokenSeparator(string tokenSeparator)
        {
            this.tokenSeparator = tokenSeparator ?? string.Empty;
        }

        /// <summary>
        /// Sets the string to insert for each position at which there is no token
        /// (i.e., when position increment is greater than one).
        /// </summary>
        /// <param name="fillerToken"> string to insert at each position where there is no token </param>
        public void SetFillerToken(string fillerToken)
        {
            this.fillerToken = null == fillerToken ? Arrays.Empty<char>() : fillerToken.ToCharArray();
        }

        public override bool IncrementToken()
        {
            bool tokenAvailable = false;
            int builtGramSize = 0;
            if (gramSize.AtMinValue() || inputWindow.Count < gramSize.Value)
            {
                ShiftInputWindow();
                gramBuilder.Length = 0;
            }
            else
            {
                builtGramSize = gramSize.PreviousValue;
            }
            if (inputWindow.Count >= gramSize.Value)
            {
                bool isAllFiller = true;
                InputWindowToken nextToken = null;
                using (IEnumerator<InputWindowToken> iter = inputWindow.GetEnumerator())
                {
                    for (int gramNum = 1; iter.MoveNext() && builtGramSize < gramSize.Value; ++gramNum)
                    {
                        nextToken = iter.Current;
                        if (builtGramSize < gramNum)
                        {
                            if (builtGramSize > 0)
                            {
                                gramBuilder.Append(tokenSeparator);
                            }
                            gramBuilder.Append(nextToken.termAtt.Buffer, 0, nextToken.termAtt.Length);
                            ++builtGramSize;
                        }
                        if (isAllFiller && nextToken.isFiller)
                        {
                            if (gramNum == gramSize.Value)
                            {
                                gramSize.Advance();
                            }
                        }
                        else
                        {
                            isAllFiller = false;
                        }
                    }
                }
                if (!isAllFiller && builtGramSize == gramSize.Value)
                {
                    inputWindow.Peek().attSource.CopyTo(this);
                    posIncrAtt.PositionIncrement = isOutputHere ? 0 : 1;
                    termAtt.SetEmpty().Append(gramBuilder);
                    if (gramSize.Value > 1)
                    {
                        typeAtt.Type = tokenType;
                        noShingleOutput = false;
                    }
                    offsetAtt.SetOffset(offsetAtt.StartOffset, nextToken.offsetAtt.EndOffset);
                    posLenAtt.PositionLength = builtGramSize;
                    isOutputHere = true;
                    gramSize.Advance();
                    tokenAvailable = true;
                }
            }
            return tokenAvailable;
        }

        private bool exhausted;

        /// <summary>
        /// <para>Get the next token from the input stream.
        /// </para>
        /// <para>If the next token has <c>positionIncrement > 1</c>,
        /// <c>positionIncrement - 1</c> <see cref="fillerToken"/>s are
        /// inserted first.
        /// </para>
        /// </summary>
        /// <param name="target"> Where to put the new token; if null, a new instance is created. </param>
        /// <returns> On success, the populated token; null otherwise </returns>
        /// <exception cref="IOException"> if the input stream has a problem </exception>
        private InputWindowToken GetNextToken(InputWindowToken target)
        {
            InputWindowToken newTarget = target;
            if (numFillerTokensToInsert > 0)
            {
                if (null == target)
                {
                    newTarget = new InputWindowToken(nextInputStreamToken.CloneAttributes());
                }
                else
                {
                    nextInputStreamToken.CopyTo(target.attSource);
                }
                // A filler token occupies no space
                newTarget.offsetAtt.SetOffset(newTarget.offsetAtt.StartOffset, newTarget.offsetAtt.StartOffset);
                newTarget.termAtt.CopyBuffer(fillerToken, 0, fillerToken.Length);
                newTarget.isFiller = true;
                --numFillerTokensToInsert;
            }
            else if (isNextInputStreamToken)
            {
                if (null == target)
                {
                    newTarget = new InputWindowToken(nextInputStreamToken.CloneAttributes());
                }
                else
                {
                    nextInputStreamToken.CopyTo(target.attSource);
                }
                isNextInputStreamToken = false;
                newTarget.isFiller = false;
            }
            else if (!exhausted)
            {
                if (m_input.IncrementToken())
                {
                    if (null == target)
                    {
                        newTarget = new InputWindowToken(CloneAttributes());
                    }
                    else
                    {
                        this.CopyTo(target.attSource);
                    }
                    if (posIncrAtt.PositionIncrement > 1)
                    {
                        // Each output shingle must contain at least one input token, 
                        // so no more than (maxShingleSize - 1) filler tokens will be inserted.
                        numFillerTokensToInsert = Math.Min(posIncrAtt.PositionIncrement - 1, maxShingleSize - 1);
                        // Save the current token as the next input stream token
                        if (null == nextInputStreamToken)
                        {
                            nextInputStreamToken = CloneAttributes();
                        }
                        else
                        {
                            this.CopyTo(nextInputStreamToken);
                        }
                        isNextInputStreamToken = true;
                        // A filler token occupies no space
                        newTarget.offsetAtt.SetOffset(offsetAtt.StartOffset, offsetAtt.StartOffset);
                        newTarget.termAtt.CopyBuffer(fillerToken, 0, fillerToken.Length);
                        newTarget.isFiller = true;
                        --numFillerTokensToInsert;
                    }
                    else
                    {
                        newTarget.isFiller = false;
                    }
                }
                else
                {
                    exhausted = true;
                    m_input.End();
                    endState = CaptureState();
                    numFillerTokensToInsert = Math.Min(posIncrAtt.PositionIncrement, maxShingleSize - 1);
                    if (numFillerTokensToInsert > 0)
                    {
                        nextInputStreamToken = new AttributeSource(this.GetAttributeFactory());
                        nextInputStreamToken.AddAttribute<ICharTermAttribute>();
                        IOffsetAttribute newOffsetAtt = nextInputStreamToken.AddAttribute<IOffsetAttribute>();
                        newOffsetAtt.SetOffset(offsetAtt.EndOffset, offsetAtt.EndOffset);
                        // Recurse/loop just once:
                        return GetNextToken(target);
                    }
                    else
                    {
                        newTarget = null;
                    }
                }
            }
            else
            {
                newTarget = null;
            }
            return newTarget;
        }

        public override void End()
        {
            if (!exhausted)
            {
                base.End();
            }
            else
            {
                RestoreState(endState);
            }
        }

        /// <summary>
        /// <para>Fills <see cref="inputWindow"/> with input stream tokens, if available, 
        /// shifting to the right if the window was previously full.
        /// </para>
        /// <para>
        /// Resets <see cref="gramSize"/> to its minimum value.
        /// </para>
        /// </summary>
        /// <exception cref="IOException"> if there's a problem getting the next token </exception>
        private void ShiftInputWindow()
        {
            InputWindowToken firstToken = null;
            if (inputWindow.Count > 0)
            {
                firstToken = inputWindow.Dequeue();
            }
            while (inputWindow.Count < maxShingleSize)
            {
                if (null != firstToken) // recycle the firstToken, if available
                {
                    if (null != GetNextToken(firstToken))
                    {
                        inputWindow.Enqueue(firstToken); // the firstToken becomes the last
                        firstToken = null;
                    }
                    else
                    {
                        break; // end of input stream
                    }
                }
                else
                {
                    InputWindowToken nextToken = GetNextToken(null);
                    if (null != nextToken)
                    {
                        inputWindow.Enqueue(nextToken);
                    }
                    else
                    {
                        break; // end of input stream
                    }
                }
            }
            if (outputUnigramsIfNoShingles && noShingleOutput && gramSize.MinValue > 1 && inputWindow.Count < minShingleSize)
            {
                gramSize.MinValue = 1;
            }
            gramSize.Reset();
            isOutputHere = false;
        }

        public override void Reset()
        {
            base.Reset();
            gramSize.Reset();
            inputWindow.Clear();
            nextInputStreamToken = null;
            isNextInputStreamToken = false;
            numFillerTokensToInsert = 0;
            isOutputHere = false;
            noShingleOutput = true;
            exhausted = false;
            endState = null;
            if (outputUnigramsIfNoShingles && !outputUnigrams)
            {
                // Fix up gramSize if minValue was reset for outputUnigramsIfNoShingles
                gramSize.MinValue = minShingleSize;
            }
        }


        /// <summary>
        /// <para>An instance of this class is used to maintain the number of input
        /// stream tokens that will be used to compose the next unigram or shingle:
        /// <see cref="gramSize"/>.
        /// </para>
        /// <para><code>gramSize</code> will take on values from the circular sequence
        /// <b>{ [ 1, ] <see cref="minShingleSize"/> [ , ... , <see cref="maxShingleSize"/> ] }</b>.
        /// </para>
        /// <para>1 is included in the circular sequence only if 
        /// <see cref="outputUnigrams"/> = true.
        /// </para>
        /// </summary>
        private class CircularSequence
        {
            private readonly ShingleFilter outerInstance;

            private int value;
            private int previousValue;
            private int minValue;

            public CircularSequence(ShingleFilter shingleFilter)
            {
                this.outerInstance = shingleFilter;
                minValue = shingleFilter.outputUnigrams ? 1 : shingleFilter.minShingleSize;
                Reset();
            }

            /// <returns> the current value. </returns>
            /// <seealso cref="Advance()"/>
            public virtual int Value => value;

            /// <summary>
            /// <para>Increments this circular number's value to the next member in the
            /// circular sequence
            /// <code>gramSize</code> will take on values from the circular sequence
            /// <b>{ [ 1, ] <see cref="minShingleSize"/> [ , ... , <see cref="maxShingleSize"/> ] }</b>.
            /// </para>
            /// <para>1 is included in the circular sequence only if 
            /// <see cref="outputUnigrams"/> = true.
            /// </para>
            /// </summary>
            public virtual void Advance()
            {
                previousValue = value;
                if (value == 1)
                {
                    value = outerInstance.minShingleSize;
                }
                else if (value == outerInstance.maxShingleSize)
                {
                    Reset();
                }
                else
                {
                    ++value;
                }
            }

            /// <summary>
            /// <para>Sets this circular number's value to the first member of the 
            /// circular sequence
            /// </para>
            /// <para><code>gramSize</code> will take on values from the circular sequence
            /// <b>{ [ 1, ] <see cref="minShingleSize"/> [ , ... , <see cref="maxShingleSize"/> ] }</b>.
            /// </para>
            /// <para>1 is included in the circular sequence only if 
            /// <see cref="outputUnigrams"/> = true.
            /// </para>
            /// </summary>
            // LUCENENET specific - S1699 - marked non-virtual because calling virtual members from the constructor is not a safe operation in .NET
            public void Reset()
            {
                previousValue = value = minValue;
            }

            /// <summary>
            /// <para>Returns true if the current value is the first member of the circular
            /// sequence.
            /// </para>
            /// <para>If <see cref="outputUnigrams"/> = true, the first member of the circular
            /// sequence will be 1; otherwise, it will be <see cref="minShingleSize"/>.
            /// 
            /// </para>
            /// </summary>
            /// <returns> true if the current value is the first member of the circular
            ///  sequence; false otherwise </returns>
            public virtual bool AtMinValue()
            {
                return value == minValue;
            }

            /// <returns> the value this instance had before the last <see cref="Advance()"/> call </returns>
            public virtual int PreviousValue => previousValue;

            internal virtual int MinValue // LUCENENET specific - added to encapsulate minValue field
            {
                get => minValue;
                set => minValue = value;
            }
        }

        private class InputWindowToken
        {
            internal readonly AttributeSource attSource;
            internal readonly ICharTermAttribute termAtt;
            internal readonly IOffsetAttribute offsetAtt;
            internal bool isFiller = false;

            public InputWindowToken(AttributeSource attSource)
            {
                this.attSource = attSource;
                this.termAtt = attSource.GetAttribute<ICharTermAttribute>();
                this.offsetAtt = attSource.GetAttribute<IOffsetAttribute>();
            }
        }
    }
}