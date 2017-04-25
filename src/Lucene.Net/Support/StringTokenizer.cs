using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Support
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

    public sealed class StringTokenizer
    {
        private readonly TokenParsingStrategy _strategy;

        private const int _preprocessThreshold = 1024; // 1024 chars -- TODO: empirically determine best threshold

        public StringTokenizer(string str, string delim, bool returnDelims)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            if (string.IsNullOrEmpty(delim))
                throw new ArgumentException("No delimiter characters given!");

            var delimSet = new HashSet<char>(delim.ToCharArray());

            if (str.Length > _preprocessThreshold)
            {
                _strategy = new StringBuilderTokenParsingStrategy(str, delimSet, returnDelims);
            }
            else
            {
                _strategy = new PreProcessTokenParsingStrategy(str, delimSet, returnDelims);
            }
        }

        public StringTokenizer(string str, string delim)
            : this(str, delim, false)
        {
        }

        public StringTokenizer(string str)
            : this(str, " \t\n\r\f", false)
        {
        }

        public bool HasMoreTokens()
        {
            return _strategy.HasMoreTokens();
        }

        public string NextToken()
        {
            return _strategy.NextToken();
        }

        //public string NextToken(string delim)
        //{
        //    if (string.IsNullOrEmpty(delim))
        //        throw new ArgumentException("No delimiter characters given!");

        //    _delims.Clear();
        //    _delims.UnionWith(delim.ToCharArray());

        //    return NextToken();
        //}

        public int CountTokens()
        {
            return _strategy.CountTokens();
        }

        private abstract class TokenParsingStrategy
        {
            public abstract bool HasMoreTokens();

            public abstract string NextToken();

            public abstract int CountTokens();
        }

        private class StringBuilderTokenParsingStrategy : TokenParsingStrategy
        {
            private readonly string _str;
            private readonly ISet<char> _delims;
            private readonly bool _returnDelims;

            private int _position = 0;

            public StringBuilderTokenParsingStrategy(string str, ISet<char> delims, bool returnDelims)
            {
                _str = str;
                _delims = delims;
                _returnDelims = returnDelims;
            }

            public override bool HasMoreTokens()
            {
                if (_position >= _str.Length)
                    return false;

                if (_returnDelims)
                    return true; // since we're not at end of string, there has to be a token left if returning delimiters

                for (int i = _position; i < _str.Length; i++)
                {
                    if (!_delims.Contains(_str[i]))
                        return true;
                }

                return false; // only delims left
            }

            public override string NextToken()
            {
                if (_position >= _str.Length)
                    throw new InvalidOperationException("Past end of string.");

                if (_returnDelims && _delims.Contains(_str[_position]))
                {
                    _position++;
                    return _str[_position].ToString();
                }

                StringBuilder sb = new StringBuilder();

                for (int i = _position; i < _str.Length; i++)
                {
                    char c = _str[i];

                    _position = i;

                    if (_delims.Contains(c))
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }

            public override int CountTokens()
            {
                if (_position >= _str.Length)
                    return 0;

                int count = 0;
                bool lastWasDelim = true; // consider start of string/substring a delim

                for (int i = _position; i < _str.Length; i++)
                {
                    char c = _str[i];

                    if (_delims.Contains(c))
                    {
                        if (!lastWasDelim)
                            count++; // increase since now we're at a delim

                        lastWasDelim = true;

                        if (_returnDelims)
                            count++; // this delim counts as a token
                    }
                    else
                    {
                        lastWasDelim = false;
                    }
                }

                if (!lastWasDelim)
                    count++; // string ended with non-delim

                return count;
            }
        }

        private class PreProcessTokenParsingStrategy : TokenParsingStrategy
        {
            private readonly string _str;
            private readonly ISet<char> _delims;
            private readonly bool _returnDelims;
            private readonly List<string> _tokens = new List<string>();
            private int _index = 0;

            public PreProcessTokenParsingStrategy(string str, ISet<char> delims, bool returnDelims)
            {
                _str = str;
                _delims = delims;
                _returnDelims = returnDelims;

                Preprocess();
            }

            private void Preprocess()
            {
                StringBuilder sb = new StringBuilder();

                foreach (char c in _str)
                {
                    if (_delims.Contains(c))
                    {
                        if (sb.Length > 0)
                        {
                            _tokens.Add(sb.ToString());
                            sb.Clear();
                        }

                        if (_returnDelims)
                            _tokens.Add(c.ToString());
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                if (sb.Length > 0)
                    _tokens.Add(sb.ToString());
            }

            public override bool HasMoreTokens()
            {
                return _index < _tokens.Count;
            }

            public override string NextToken()
            {
                return _tokens[_index++];
            }

            public override int CountTokens()
            {
                return _tokens.Count - _index;
            }
        }
    }
}