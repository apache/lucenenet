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
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analyzers.Miscellaneous;
using Lucene.Net.Analyzers.Shingle.Codec;
using Lucene.Net.Analyzers.Shingle.Matrix;
using FlagsAttribute = Lucene.Net.Analysis.Tokenattributes.FlagsAttribute;

namespace Lucene.Net.Analyzers.Shingle
{
    /// <summary>
    /// <p>A ShingleMatrixFilter constructs shingles (token n-grams) from a token stream.
    /// In other words, it creates combinations of tokens as a single token.</p>
    ///
    /// <p>For example, the sentence "please divide this sentence into shingles"
    /// might be tokenized into shingles "please divide", "divide this",
    /// "this sentence", "sentence into", and "into shingles".</p>
    ///
    /// <p>Using a shingle filter at index and query time can in some instances
    /// be used to replace phrase queries, especially them with 0 slop.</p>
    ///
    /// <p>Without a spacer character
    /// it can be used to handle composition and decomposition of words
    /// such as searching for "multi dimensional" instead of "multidimensional".
    /// It is a rather common human problem at query time
    /// in several languages, notably the northern Germanic branch.</p>
    ///
    /// <p>Shingles are amongst many things also known to solve problems
    /// in spell checking, language detection and document clustering.</p>
    ///
    /// <p>This filter is backed by a three dimensional column oriented matrix
    /// used to create permutations of the second dimension, the rows,
    /// and leaves the third, the z-axis, for for multi token synonyms.</p>
    ///
    /// <p>In order to use this filter you need to define a way of positioning
    /// the input stream tokens in the matrix. This is done using a
    /// ShingleMatrixFilter.TokenSettingsCodec.
    /// There are three simple implementations for demonstrational purposes,
    /// see ShingleMatrixFilter.OneDimensionalNonWeightedTokenSettingsCodec,
    /// ShingleMatrixFilter.TwoDimensionalNonWeightedSynonymTokenSettingsCodec
    /// and ShingleMatrixFilter.SimpleThreeDimensionalTokenSettingsCodec.</p>
    ///
    /// <p>Consider this token matrix:</p>
    /// <pre>
    ///  Token[column][row][z-axis]{
    ///    {{hello}, {greetings, and, salutations}},
    ///    {{world}, {earth}, {tellus}}
    ///  };
    /// </pre>
    ///
    /// It would produce the following 2-3 gram sized shingles:
    ///
    /// <pre>
    /// "hello_world"
    /// "greetings_and"
    /// "greetings_and_salutations"
    /// "and_salutations"
    /// "and_salutations_world"
    /// "salutations_world"
    /// "hello_earth"
    /// "and_salutations_earth"
    /// "salutations_earth"
    /// "hello_tellus"
    /// "and_salutations_tellus"
    /// "salutations_tellus"
    ///  </pre>
    ///
    /// <p>This implementation can be rather heap demanding
    /// if (maximum shingle size - minimum shingle size) is a great number and the stream contains many columns,
    /// or if each column contains a great number of rows.</p>
    ///
    /// <p>The problem is that in order avoid producing duplicates
    /// the filter needs to keep track of any shingle already produced and returned to the consumer.</p>
    ///
    /// <p>There is a bit of resource management to handle this
    /// but it would of course be much better if the filter was written
    /// so it never created the same shingle more than once in the first place.</p>
    ///
    /// <p>The filter also has basic support for calculating weights for the shingles
    /// based on the weights of the tokens from the input stream, output shingle size, etc.
    /// See CalculateShingleWeight.
    /// <p/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
    /// the ones located in org.apache.lucene.analysis.tokenattributes.</p> 
    /// </summary>
    public class ShingleMatrixFilter : TokenStream
    {
        public static Char DefaultSpacerCharacter = '_';
        public static TokenSettingsCodec DefaultSettingsCodec = new OneDimensionalNonWeightedTokenSettingsCodec();
        public static bool IgnoringSinglePrefixOrSuffixShingleByDefault;

        private readonly FlagsAttribute _flagsAtt;
        private readonly FlagsAttribute _inFlagsAtt;

        private readonly OffsetAttribute _inOffsetAtt;
        private readonly PayloadAttribute _inPayloadAtt;
        private readonly PositionIncrementAttribute _inPosIncrAtt;
        private readonly TermAttribute _inTermAtt;
        private readonly TypeAttribute _inTypeAtt;
        private readonly TokenStream _input;
        private readonly OffsetAttribute _offsetAtt;
        private readonly PayloadAttribute _payloadAtt;
        private readonly PositionIncrementAttribute _posIncrAtt;
        private readonly Token _requestNextToken = new Token();
        private readonly Token _reusableToken = new Token();
        private readonly TokenSettingsCodec _settingsCodec;

        /// <summary>
        /// A set containing shingles that has been the result of a call to Next(Token),
        /// used to avoid producing the same shingle more than once.
        /// 
        /// <p>
        /// NOTE: The Java List implementation uses a different equality comparison scheme
        /// than .NET's Generic List. So We have to use a custom IEqualityComparer implementation 
        /// to get the same behaviour.
        /// </p>
        /// </summary>
        private readonly HashSet<SupportClass.EquatableList<Token>> _shinglesSeen =
            new HashSet<SupportClass.EquatableList<Token>>(); 

        private readonly TermAttribute _termAtt;
        private readonly TypeAttribute _typeAtt;
        private List<Token> _currentPermuationTokens;

        // Index to what row a token in currentShingleTokens represents
        private List<Row> _currentPermutationRows;

        private int _currentPermutationTokensStartOffset;
        private int _currentShingleLength;
        private MatrixPermutationIterator _permutations;
        private Token _readColumnBuf;


        /// <summary>
        /// Creates a shingle filter based on a user defined matrix.
        /// 
        /// The filter /will/ delete columns from the input matrix! You will not be able to reset the filter if you used this constructor.
        /// todo: don't touch the matrix! use a bool, set the input stream to null or something, and keep track of where in the matrix we are at.
        /// 
        /// </summary>
        /// <param name="matrix">the input based for creating shingles. Does not need to contain any information until ShingleMatrixFilter.Next(Token) is called the first time.</param>
        /// <param name="minimumShingleSize">minimum number of tokens in any shingle.</param>
        /// <param name="maximumShingleSize">maximum number of tokens in any shingle.</param>
        /// <param name="spacerCharacter">character to use between texts of the token parts in a shingle. null for none.</param>
        /// <param name="ignoringSinglePrefixOrSuffixShingle">if true, shingles that only contains permutation of the first of the last column will not be produced as shingles. Useful when adding boundary marker tokens such as '^' and '$'.</param>
        /// <param name="settingsCodec">codec used to read input token weight and matrix positioning.</param>
        public ShingleMatrixFilter(Matrix.Matrix matrix, int minimumShingleSize, int maximumShingleSize, Char spacerCharacter, bool ignoringSinglePrefixOrSuffixShingle, TokenSettingsCodec settingsCodec)
        {
            Matrix = matrix;
            MinimumShingleSize = minimumShingleSize;
            MaximumShingleSize = maximumShingleSize;
            SpacerCharacter = spacerCharacter;
            IsIgnoringSinglePrefixOrSuffixShingle = ignoringSinglePrefixOrSuffixShingle;
            _settingsCodec = settingsCodec;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _termAtt = (TermAttribute) AddAttribute(typeof (TermAttribute));
            _posIncrAtt = (PositionIncrementAttribute) AddAttribute(typeof (PositionIncrementAttribute));
            _payloadAtt = (PayloadAttribute) AddAttribute(typeof (PayloadAttribute));
            _offsetAtt = (OffsetAttribute) AddAttribute(typeof (OffsetAttribute));
            _typeAtt = (TypeAttribute) AddAttribute(typeof (TypeAttribute));
            _flagsAtt = (FlagsAttribute) AddAttribute(typeof (FlagsAttribute));
            // ReSharper restore DoNotCallOverridableMethodsInConstructor

            // set the input to be an empty token stream, we already have the data.
            _input = new EmptyTokenStream();

            _inTermAtt = (TermAttribute) _input.AddAttribute(typeof (TermAttribute));
            _inPosIncrAtt = (PositionIncrementAttribute) _input.AddAttribute(typeof (PositionIncrementAttribute));
            _inPayloadAtt = (PayloadAttribute) _input.AddAttribute(typeof (PayloadAttribute));
            _inOffsetAtt = (OffsetAttribute) _input.AddAttribute(typeof (OffsetAttribute));
            _inTypeAtt = (TypeAttribute) _input.AddAttribute(typeof (TypeAttribute));
            _inFlagsAtt = (FlagsAttribute) _input.AddAttribute(typeof (FlagsAttribute));
        }

        /// <summary>
        /// Creates a shingle filter using default settings.
        /// 
        /// See ShingleMatrixFilter.DefaultSpacerCharacter, 
        /// ShingleMatrixFilter.IgnoringSinglePrefixOrSuffixShingleByDefault, 
        /// and ShingleMatrixFilter.DefaultSettingsCodec
        /// </summary>
        /// <param name="input">stream from which to construct the matrix</param>
        /// <param name="minimumShingleSize">minimum number of tokens in any shingle.</param>
        /// <param name="maximumShingleSize">maximum number of tokens in any shingle.</param>
        public ShingleMatrixFilter(TokenStream input, int minimumShingleSize, int maximumShingleSize)
            : this(input, minimumShingleSize, maximumShingleSize, DefaultSpacerCharacter) { }

        /// <summary>
        /// Creates a shingle filter using default settings.
        /// 
        /// See IgnoringSinglePrefixOrSuffixShingleByDefault, and DefaultSettingsCodec
        /// </summary>
        /// <param name="input">stream from which to construct the matrix</param>
        /// <param name="minimumShingleSize">minimum number of tokens in any shingle.</param>
        /// <param name="maximumShingleSize">maximum number of tokens in any shingle.</param>
        /// <param name="spacerCharacter">character to use between texts of the token parts in a shingle. null for none. </param>
        public ShingleMatrixFilter(TokenStream input, int minimumShingleSize, int maximumShingleSize, Char? spacerCharacter)
            : this( input, minimumShingleSize, maximumShingleSize, spacerCharacter, IgnoringSinglePrefixOrSuffixShingleByDefault) { }

        /// <summary>
        /// Creates a shingle filter using the default <see cref="TokenSettingsCodec"/>.
        /// 
        /// See DefaultSettingsCodec
        /// </summary>
        /// <param name="input">stream from which to construct the matrix</param>
        /// <param name="minimumShingleSize">minimum number of tokens in any shingle.</param>
        /// <param name="maximumShingleSize">maximum number of tokens in any shingle.</param>
        /// <param name="spacerCharacter">character to use between texts of the token parts in a shingle. null for none.</param>
        /// <param name="ignoringSinglePrefixOrSuffixShingle">if true, shingles that only contains permutation of the first of the last column will not be produced as shingles. Useful when adding boundary marker tokens such as '^' and '$'.</param>
        public ShingleMatrixFilter(TokenStream input, int minimumShingleSize, int maximumShingleSize, Char? spacerCharacter, bool ignoringSinglePrefixOrSuffixShingle)
            : this(input, minimumShingleSize, maximumShingleSize, spacerCharacter, ignoringSinglePrefixOrSuffixShingle, DefaultSettingsCodec) { }

        /// <summary>
        /// Creates a shingle filter with ad hoc parameter settings.
        /// </summary>
        /// <param name="input">stream from which to construct the matrix</param>
        /// <param name="minimumShingleSize">minimum number of tokens in any shingle.</param>
        /// <param name="maximumShingleSize">maximum number of tokens in any shingle.</param>
        /// <param name="spacerCharacter">character to use between texts of the token parts in a shingle. null for none.</param>
        /// <param name="ignoringSinglePrefixOrSuffixShingle">if true, shingles that only contains permutation of the first of the last column will not be produced as shingles. Useful when adding boundary marker tokens such as '^' and '$'.</param>
        /// <param name="settingsCodec">codec used to read input token weight and matrix positioning.</param>
        public ShingleMatrixFilter(TokenStream input, int minimumShingleSize, int maximumShingleSize, Char? spacerCharacter, bool ignoringSinglePrefixOrSuffixShingle, TokenSettingsCodec settingsCodec)
        {
            _input = input;
            MinimumShingleSize = minimumShingleSize;
            MaximumShingleSize = maximumShingleSize;
            SpacerCharacter = spacerCharacter;
            IsIgnoringSinglePrefixOrSuffixShingle = ignoringSinglePrefixOrSuffixShingle;
            _settingsCodec = settingsCodec;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _termAtt = (TermAttribute) AddAttribute(typeof (TermAttribute));
            _posIncrAtt = (PositionIncrementAttribute) AddAttribute(typeof (PositionIncrementAttribute));
            _payloadAtt = (PayloadAttribute) AddAttribute(typeof (PayloadAttribute));
            _offsetAtt = (OffsetAttribute) AddAttribute(typeof (OffsetAttribute));
            _typeAtt = (TypeAttribute) AddAttribute(typeof (TypeAttribute));
            _flagsAtt = (FlagsAttribute) AddAttribute(typeof (FlagsAttribute));
            // ReSharper restore DoNotCallOverridableMethodsInConstructor

            _inTermAtt = (TermAttribute) input.AddAttribute(typeof (TermAttribute));
            _inPosIncrAtt = (PositionIncrementAttribute) input.AddAttribute(typeof (PositionIncrementAttribute));
            _inPayloadAtt = (PayloadAttribute) input.AddAttribute(typeof (PayloadAttribute));
            _inOffsetAtt = (OffsetAttribute) input.AddAttribute(typeof (OffsetAttribute));
            _inTypeAtt = (TypeAttribute) input.AddAttribute(typeof (TypeAttribute));
            _inFlagsAtt = (FlagsAttribute) input.AddAttribute(typeof (FlagsAttribute));
        }

        public int MinimumShingleSize { get; set; }

        public int MaximumShingleSize { get; set; }

        public Matrix.Matrix Matrix { get; set; }

        public Char? SpacerCharacter { get; set; }

        public bool IsIgnoringSinglePrefixOrSuffixShingle { get; set; }

        public override void Reset()
        {
            _permutations = null;
            _shinglesSeen.Clear();
            _input.Reset();
        }

        public override sealed bool IncrementToken()
        {
            if (Matrix == null)
            {
                Matrix = new Matrix.Matrix();

                // fill matrix with maximumShingleSize columns
                while (Matrix.Columns.Count < MaximumShingleSize && ReadColumn())
                {
                    // this loop looks ugly
                }
            }

            // This loop exists in order to avoid recursive calls to the next method
            // as the complexity of a large matrix
            // then would require a multi gigabyte sized stack.
            Token token;
            do
            {
                token = ProduceNextToken(_reusableToken);
            } while (token == _requestNextToken);
            
            if (token == null) 
                return false;

            ClearAttributes();

            _termAtt.SetTermBuffer(token.TermBuffer(), 0, token.TermLength());
            _posIncrAtt.SetPositionIncrement(token.GetPositionIncrement());
            _flagsAtt.SetFlags(token.GetFlags());
            _offsetAtt.SetOffset(token.StartOffset(), token.EndOffset());
            _typeAtt.SetType(token.Type());
            _payloadAtt.SetPayload(token.GetPayload());

            return true;
        }

        private Token GetNextInputToken(Token token)
        {
            if (!_input.IncrementToken()) return null;

            token.SetTermBuffer(_inTermAtt.TermBuffer(), 0, _inTermAtt.TermLength());
            token.SetPositionIncrement(_inPosIncrAtt.GetPositionIncrement());
            token.SetFlags(_inFlagsAtt.GetFlags());
            token.SetOffset(_inOffsetAtt.StartOffset(), _inOffsetAtt.EndOffset());
            token.SetType(_inTypeAtt.Type());
            token.SetPayload(_inPayloadAtt.GetPayload());
            return token;
        }

        /// <summary>
        /// Deprecated: Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.
        /// </summary>
        /// <param name="reusableToken"></param>
        /// <returns></returns>
        [Obsolete("The new IncrementToken() and AttributeSource APIs should be used instead.")]
        public override sealed Token Next(Token reusableToken)
        {
            return base.Next(reusableToken);
        }
        
        /// <summary>
        /// Deprecated: Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.
        /// </summary>
        /// <returns></returns>
        [Obsolete("The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling Next(Token) or using the new IncrementToken() method with the new AttributeSource API.")]
        public override sealed Token Next()
        {
            return base.Next();
        }

        /// <summary>
        /// This method exists in order to avoid recursive calls to the method
        /// as the complexity of a fairly small matrix then easily would require
        /// a gigabyte sized stack per thread.
        /// </summary>
        /// <param name="reusableToken"></param>
        /// <returns>null if exhausted, instance request_next_token if one more call is required for an answer, 
        /// or instance parameter resuableToken.</returns>
        private Token ProduceNextToken(Token reusableToken)
        {
            if (_currentPermuationTokens != null)
            {
                _currentShingleLength++;

                if (_currentShingleLength + _currentPermutationTokensStartOffset <= _currentPermuationTokens.Count
                    && _currentShingleLength <= MaximumShingleSize)
                {
                    // it is possible to create at least one more shingle of the current matrix permutation

                    if (IsIgnoringSinglePrefixOrSuffixShingle && 
                        _currentShingleLength == 1 && 
                        (_currentPermutationRows[_currentPermutationTokensStartOffset].Column.IsFirst || _currentPermutationRows[_currentPermutationTokensStartOffset].Column.IsLast))
                    {
                        return Next();
                    }

                    var termLength = 0;

                    var shingle = new SupportClass.EquatableList<Token>();

                    for (int i = 0; i < _currentShingleLength; i++)
                    {
                        var shingleToken = _currentPermuationTokens[i + _currentPermutationTokensStartOffset];
                        termLength += shingleToken.TermLength();
                        shingle.Add(shingleToken);
                    }
                    if (SpacerCharacter != null)
                        termLength += _currentShingleLength - 1;

                    // only produce shingles that not already has been created
                    if (!_shinglesSeen.Add(shingle))
                        return _requestNextToken;

                    // shingle token factory
                    var sb = new StringBuilder(termLength + 10); // paranormal ability to foresee the future. ;)
                    foreach (var shingleToken in shingle)
                    {
                        if (SpacerCharacter != null &&  sb.Length > 0)
                            sb.Append(SpacerCharacter);

                        sb.Append(shingleToken.TermBuffer(), 0, shingleToken.TermLength());
                    }

                    reusableToken.SetTermBuffer(sb.ToString());
                    UpdateToken(reusableToken, shingle, _currentPermutationTokensStartOffset, _currentPermutationRows,
                                _currentPermuationTokens);

                    return reusableToken;
                }

                // it is NOT possible to create one more shingles of the current matrix permutation
                if (_currentPermutationTokensStartOffset < _currentPermuationTokens.Count - 1)
                {
                    // reset shingle size and move one step to the right in the current tokens permutation
                    _currentPermutationTokensStartOffset++;
                    _currentShingleLength = MinimumShingleSize - 1;
                    return _requestNextToken;
                }


                // todo does this ever occur?
                if (_permutations == null)
                    return null;

                if (!_permutations.HasNext())
                {
                    // load more data (if available) to the matrix

                    // don't really care, we just read it.
                    if (_input != null)
                        ReadColumn();

                    // get rid of resources

                    // delete the first column in the matrix
                    var deletedColumn = Matrix.Columns[0];
                    Matrix.Columns.RemoveAt(0);

                    // remove all shingles seen that include any of the tokens from the deleted column.
                    var deletedColumnTokens = deletedColumn.Rows.SelectMany(row => row.Tokens).ToList();
                    
                    // I'm a little concerned about this part of the code, because the unit tests currently 
                    // don't cover this scenario. (I put a break point here, and ran the unit tests in debug mode 
                    // and this code block was never hit... I also changed it significatly from the Java version
                    // to use RemoveWhere and LINQ. 
                    //
                    // TODO: Write a unit test to cover this and make sure this is a good port! -thoward

                    // linq version
                    _shinglesSeen.RemoveWhere(
                        shingle => (shingle.Find(deletedColumnTokens.Contains) != default(Token)));

                    //// initial conversion
                    //var shinglesSeenIterator = _shinglesSeen.ToList();
                    //foreach (var shingle in shinglesSeenIterator)
                    //{
                    //    foreach (var deletedColumnToken in deletedColumnTokens)
                    //    {
                    //        if (shingle.Contains(deletedColumnToken))
                    //        {
                    //            _shinglesSeen.Remove(shingle);
                    //            break;
                    //        }
                    //    }
                    //}

                    // exhausted
                    if (Matrix.Columns.Count < MinimumShingleSize)
                        return null;

                    // create permutations of the matrix it now looks
                    _permutations = Matrix.PermutationIterator();
                }

                NextTokensPermutation();
                return _requestNextToken;
            }

            if (_permutations == null)
                _permutations = Matrix.PermutationIterator();

            if (!_permutations.HasNext())
                return null;

            NextTokensPermutation();

            return _requestNextToken;
        }

        /// <summary>
        /// Get next permutation of row combinations,
        /// creates list of all tokens in the row and
        /// an index from each such token to what row they exist in.
        /// finally resets the current (next) shingle size and offset. 
        /// </summary>
        private void NextTokensPermutation()
        {
            var rowsPermutation = _permutations.Next();
            var currentPermutationRows = new List<Row>();
            var currentPermuationTokens = new List<Token>();

            foreach (var row in rowsPermutation)
            {
                foreach (var token in row.Tokens)
                {
                    currentPermuationTokens.Add(token);
                    currentPermutationRows.Add(row);
                }
            }
            _currentPermuationTokens = currentPermuationTokens;
            _currentPermutationRows = currentPermutationRows;

            _currentPermutationTokensStartOffset = 0;
            _currentShingleLength = MinimumShingleSize - 1;
        }

        /// <summary>
        /// Final touch of a shingle token before it is passed on to the consumer from method <see cref="Next(Token)"/>.
        /// 
        /// Calculates and sets type, flags, position increment, start/end offsets and weight.
        /// </summary>
        /// <param name="token">Shingle Token</param>
        /// <param name="shingle">Tokens used to produce the shingle token.</param>
        /// <param name="currentPermutationStartOffset">Start offset in parameter currentPermutationTokens</param>
        /// <param name="currentPermutationRows">index to Matrix.Column.Row from the position of tokens in parameter currentPermutationTokens</param>
        /// <param name="currentPermuationTokens">tokens of the current permutation of rows in the matrix. </param>
        public void UpdateToken(Token token, List<Token> shingle, int currentPermutationStartOffset, List<Row> currentPermutationRows, List<Token> currentPermuationTokens)
        {
            token.SetType(typeof(ShingleMatrixFilter).Name);
            token.SetFlags(0);
            token.SetPositionIncrement(1);
            token.SetStartOffset((shingle[0]).StartOffset());
            token.SetEndOffset(shingle[shingle.Count - 1].EndOffset());

            _settingsCodec.SetWeight(
                token, 
                CalculateShingleWeight(token, shingle, currentPermutationStartOffset, currentPermutationRows, currentPermuationTokens)
                );
        }

        /// <summary>
        /// Evaluates the new shingle token weight.
        /// 
        /// for (shingle part token in shingle)
        /// weight +=  shingle part token weight * (1 / sqrt(all shingle part token weights summed))
        /// 
        /// This algorithm gives a slightly greater score for longer shingles
        /// and is rather penalising to great shingle token part weights.
        /// </summary>
        /// <param name="shingleToken">token returned to consumer</param>
        /// <param name="shingle">tokens the tokens used to produce the shingle token.</param>
        /// <param name="currentPermutationStartOffset">start offset in parameter currentPermutationRows and currentPermutationTokens.</param>
        /// <param name="currentPermutationRows">an index to what matrix row a token in parameter currentPermutationTokens exist.</param>
        /// <param name="currentPermuationTokens">all tokens in the current row permutation of the matrix. A sub list (parameter offset, parameter shingle.size) equals parameter shingle.</param>
        /// <returns>weight to be set for parameter shingleToken </returns>
        public float CalculateShingleWeight(Token shingleToken, List<Token> shingle, int currentPermutationStartOffset, List<Row> currentPermutationRows, List<Token> currentPermuationTokens)
        {
            var weights = new double[shingle.Count];

            double total = 0f;
            double top = 0d;

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = _settingsCodec.GetWeight(shingle[i]);

                double tmp = weights[i];

                if (tmp > top)
                    top = tmp;

                total += tmp;
            }

            double factor = 1d/Math.Sqrt(total);

            double weight = weights.Sum(partWeight => partWeight*factor);

            return (float) weight;
        }

        /// <summary>
        /// Loads one column from the token stream.
        /// 
        /// When the last token is read from the token stream it will column.setLast(true);
        /// </summary>
        /// <returns>true if it manage to read one more column from the input token stream</returns>
        private bool ReadColumn()
        {
            Token token;

            if (_readColumnBuf != null)
            {
                token = _readColumnBuf;
                _readColumnBuf = null;
            }
            else
            {
                token = GetNextInputToken(new Token());
            }

            if (token == null)
                return false;

            var currentReaderColumn = new Column(Matrix);
            var currentReaderRow = new Row(currentReaderColumn);

            currentReaderRow.Tokens.AddLast(token);

            TokenPositioner tokenPositioner;
            while ((_readColumnBuf = GetNextInputToken(new Token())) != null &&
                   (tokenPositioner = _settingsCodec.GetTokenPositioner(_readColumnBuf)) != TokenPositioner.NewColumn)
            {
                if (tokenPositioner == TokenPositioner.SameRow)
                {
                    currentReaderRow.Tokens.AddLast(_readColumnBuf);
                }
                else
                {
                    currentReaderRow = new Row(currentReaderColumn);
                    currentReaderRow.Tokens.AddLast(_readColumnBuf);
                }
                _readColumnBuf = null;
            }

            if (_readColumnBuf == null)
            {
                _readColumnBuf = GetNextInputToken(new Token());

                if (_readColumnBuf == null)
                    currentReaderColumn.IsLast = true;
            }

            return true;
        }
    }
}