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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analyzers.Shingle
{
    /// <summary>
    /// <p>A ShingleFilter constructs shingles (token n-grams) from a token stream.
    /// In other words, it creates combinations of tokens as a single token.</p>
    /// 
    /// <p>For example, the sentence "please divide this sentence into shingles"
    /// might be tokenized into shingles "please divide", "divide this",
    /// "this sentence", "sentence into", and "into shingles".</p>
    ///     
    /// <p>This filter handles position increments > 1 by inserting filler tokens
    /// (tokens with termtext "_"). It does not handle a position increment of 0. </p>
    /// </summary>
    public class ShingleFilter : TokenFilter
    {
        /// <summary>
        /// Filler token for when positionIncrement is more than 1
        /// </summary>
        public static readonly char[] FillerToken = {'_'};

        /// <summary>
        /// Default maximum shingle size is 2.
        /// </summary>
        public static readonly int DefaultMaxShingleSize = 2;

        /// <summary>
        /// The string to use when joining adjacent tokens to form a shingle
        /// </summary>
        public static readonly string TokenSeparator = " ";

        private readonly OffsetAttribute _offsetAtt;
        private readonly PositionIncrementAttribute _posIncrAtt;

        private readonly LinkedList<State> _shingleBuf = new LinkedList<State>();
        private readonly TermAttribute _termAtt;
        private readonly TypeAttribute _typeAtt;
        private State _currentToken;
        private int[] _endOffsets;
        private bool _hasCurrentToken;

        /// <summary>
        /// Maximum shingle size (number of tokens)
        /// </summary>
        private int _maxShingleSize;

        private State _nextToken;
        private int _numFillerTokensToInsert;

        /// <summary>
        /// By default, we output unigrams (individual tokens) as well as shingles (token n-grams).
        /// </summary>
        private bool _outputUnigrams = true;

        private int _shingleBufferPosition;
        private StringBuilder[] _shingles;
        private String _tokenType = "shingle";

        /// <summary>
        /// Constructs a ShingleFilter with the specified single size from the TokenStream
        /// </summary>
        /// <param name="input">input token stream</param>
        /// <param name="maxShingleSize">maximum shingle size produced by the filter.</param>
        public ShingleFilter(TokenStream input, int maxShingleSize) : base(input)
        {
            SetMaxShingleSize(maxShingleSize);

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _termAtt = (TermAttribute) AddAttribute(typeof (TermAttribute));
            _offsetAtt = (OffsetAttribute) AddAttribute(typeof (OffsetAttribute));
            _posIncrAtt = (PositionIncrementAttribute) AddAttribute(typeof (PositionIncrementAttribute));
            _typeAtt = (TypeAttribute) AddAttribute(typeof (TypeAttribute));
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        /// <summary>
        /// Construct a ShingleFilter with default shingle size.
        /// </summary>
        /// <param name="input">input stream</param>
        public ShingleFilter(TokenStream input) :
            this(input, DefaultMaxShingleSize)
        {
        }

        /// <summary>
        /// Construct a ShingleFilter with the specified token type for shingle tokens.
        /// </summary>
        /// <param name="input">input stream</param>
        /// <param name="tokenType">token type for shingle tokens</param>
        public ShingleFilter(TokenStream input, String tokenType) :
            this(input, DefaultMaxShingleSize)
        {
            SetTokenType(tokenType);
        }

        /// <summary>
        /// Set the type of the shingle tokens produced by this filter. (default: "shingle")
        /// </summary>
        /// <param name="tokenType">token TokenType</param>
        public void SetTokenType(String tokenType)
        {
            _tokenType = tokenType;
        }

        /// <summary>
        /// Shall the output stream contain the input tokens (unigrams) as well as shingles? (default: true.)
        /// </summary>
        /// <param name="outputUnigrams">Whether or not the output stream shall contain the input tokens (unigrams)</param>
        public void SetOutputUnigrams(bool outputUnigrams)
        {
            _outputUnigrams = outputUnigrams;
        }

        /// <summary>
        /// Set the max shingle size (default: 2)
        /// </summary>
        /// <param name="maxShingleSize">max size of output shingles</param>
        public void SetMaxShingleSize(int maxShingleSize)
        {
            if (maxShingleSize < 2)
                throw new ArgumentException("Max shingle size must be >= 2", "maxShingleSize");

            _shingles = new StringBuilder[maxShingleSize];

            for (int i = 0; i < _shingles.Length; i++)
            {
                _shingles[i] = new StringBuilder();
            }

            _maxShingleSize = maxShingleSize;
        }

        /// <summary>
        /// Clear the StringBuilders that are used for storing the output shingles.
        /// </summary>
        private void ClearShingles()
        {
            foreach (StringBuilder t in _shingles)
            {
                t.Length = 0;
            }
        }

        /// <summary>
        /// See Lucene.Net.Analysis.TokenStream.Next()
        /// </summary>
        /// <returns></returns>
        public override bool IncrementToken()
        {
            while (true)
            {
                if (_nextToken == null)
                {
                    if (!FillShingleBuffer())
                        return false;
                }

                _nextToken = _shingleBuf.First.Value;

                if (_outputUnigrams)
                {
                    if (_shingleBufferPosition == 0)
                    {
                        RestoreState(_nextToken);
                        _posIncrAtt.SetPositionIncrement(1);
                        _shingleBufferPosition++;
                        return true;
                    }
                }
                else if (_shingleBufferPosition%_maxShingleSize == 0)
                {
                    _shingleBufferPosition++;
                }

                if (_shingleBufferPosition < _shingleBuf.Count)
                {
                    RestoreState(_nextToken);
                    _typeAtt.SetType(_tokenType);
                    _offsetAtt.SetOffset(_offsetAtt.StartOffset(), _endOffsets[_shingleBufferPosition]);
                    StringBuilder buf = _shingles[_shingleBufferPosition];
                    int termLength = buf.Length;
                    char[] termBuffer = _termAtt.TermBuffer();
                    if (termBuffer.Length < termLength)
                        termBuffer = _termAtt.ResizeTermBuffer(termLength);
                    buf.CopyTo(0, termBuffer, 0, termLength);
                    _termAtt.SetTermLength(termLength);
                    if ((! _outputUnigrams) && _shingleBufferPosition%_maxShingleSize == 1)
                    {
                        _posIncrAtt.SetPositionIncrement(1);
                    }
                    else
                    {
                        _posIncrAtt.SetPositionIncrement(0);
                    }
                    _shingleBufferPosition++;
                    if (_shingleBufferPosition == _shingleBuf.Count)
                    {
                        _nextToken = null;
                        _shingleBufferPosition = 0;
                    }
                    return true;
                }

                _nextToken = null;
                _shingleBufferPosition = 0;
            }
        }

        /// <summary>
        /// <p>
        /// Get the next token from the input stream and push it on the token buffer.
        /// If we encounter a token with position increment > 1, we put filler tokens
        /// on the token buffer.
        /// </p>
        /// Returns null when the end of the input stream is reached.
        /// </summary>
        /// <returns>the next token, or null if at end of input stream</returns>
        private bool GetNextToken()
        {
            while (true)
            {
                if (_numFillerTokensToInsert > 0)
                {
                    if (_currentToken == null)
                    {
                        _currentToken = CaptureState();
                    }
                    else
                    {
                        RestoreState(_currentToken);
                    }
                    _numFillerTokensToInsert--;
                    // A filler token occupies no space
                    _offsetAtt.SetOffset(_offsetAtt.StartOffset(), _offsetAtt.StartOffset());
                    _termAtt.SetTermBuffer(FillerToken, 0, FillerToken.Length);
                    return true;
                }

                if (_hasCurrentToken)
                {
                    if (_currentToken != null)
                    {
                        RestoreState(_currentToken);
                        _currentToken = null;
                    }
                    _hasCurrentToken = false;
                    return true;
                }

                if (!input.IncrementToken())
                    return false;

                _hasCurrentToken = true;

                if (_posIncrAtt.GetPositionIncrement() > 1)
                    _numFillerTokensToInsert = _posIncrAtt.GetPositionIncrement() - 1;
            }
        }

        /// <summary>
        /// Fill the output buffer with new shingles.
        /// </summary>
        /// <exception cref="IOException">throws IOException if there's a problem getting the next token</exception>
        /// <returns></returns>
        private bool FillShingleBuffer()
        {
            bool addedToken = false;

            // Try to fill the shingle buffer.

            do
            {
                if (!GetNextToken())
                    break;

                _shingleBuf.AddLast(CaptureState());

                if (_shingleBuf.Count > _maxShingleSize)
                    _shingleBuf.RemoveFirst();

                addedToken = true;
            } while (_shingleBuf.Count < _maxShingleSize);

            if (_shingleBuf.Count == 0)
                return false;


            // If no new token could be added to the shingle buffer, we have reached
            // the end of the input stream and have to discard the least recent token.

            if (! addedToken)
                _shingleBuf.RemoveFirst();

            if (_shingleBuf.Count == 0)
                return false;

            ClearShingles();

            _endOffsets = new int[_shingleBuf.Count];
            for (int i = 0; i < _endOffsets.Length; i++)
            {
                _endOffsets[i] = 0;
            }

            int shingleIndex = 0;

            foreach (State state in _shingleBuf)
            {
                RestoreState(state);

                for (int j = shingleIndex; j < _shingles.Length; j++)
                {
                    if (_shingles[j].Length != 0)
                        _shingles[j].Append(TokenSeparator);

                    _shingles[j].Append(_termAtt.TermBuffer(), 0, _termAtt.TermLength());
                }

                _endOffsets[shingleIndex] = _offsetAtt.EndOffset();
                shingleIndex++;
            }

            return true;
        }

        /// <summary>
        /// Deprecated: Will be removed in Lucene 3.0. This method is readonly, as it should not be overridden. 
        /// Delegates to the backwards compatibility layer.
        /// </summary>
        /// <param name="reusableToken"></param>
        /// <returns></returns>
        [Obsolete("The new IncrementToken() and AttributeSource APIs should be used instead.")]
        public override sealed Token Next(Token reusableToken)
        {
            return base.Next(reusableToken);
        }

        /// <summary>
        /// Deprecated: Will be removed in Lucene 3.0. This method is readonly, as it should not be overridden. 
        /// Delegates to the backwards compatibility layer.
        /// </summary>
        /// <returns></returns>
        [Obsolete("The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling Next(Token) or using the new IncrementToken() method with the new AttributeSource API.")]
        public override sealed Token Next()
        {
            return base.Next();
        }

        public override void Reset()
        {
            base.Reset();

            _nextToken = null;
            _shingleBufferPosition = 0;
            _shingleBuf.Clear();
            _numFillerTokensToInsert = 0;
            _currentToken = null;
            _hasCurrentToken = false;
        }
    }
}