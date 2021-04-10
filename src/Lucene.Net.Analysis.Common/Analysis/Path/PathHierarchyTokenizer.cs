// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Path
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
    /// Tokenizer for path-like hierarchies.
    /// <para>
    /// Take something like:
    /// 
    /// <code>
    ///  /something/something/else
    /// </code>
    /// 
    /// and make:
    /// 
    /// <code>
    ///  /something
    ///  /something/something
    ///  /something/something/else
    /// </code>
    /// </para>
    /// </summary>
    public class PathHierarchyTokenizer : Tokenizer
    {
        public PathHierarchyTokenizer(TextReader input)
            : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
        {
        }

        public PathHierarchyTokenizer(TextReader input, int skip)
            : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
        {
        }

        public PathHierarchyTokenizer(TextReader input, int bufferSize, char delimiter)
            : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
        {
        }

        public PathHierarchyTokenizer(TextReader input, char delimiter, char replacement)
            : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
        {
        }

        public PathHierarchyTokenizer(TextReader input, char delimiter, char replacement, int skip)
            : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
        {
        }

        public PathHierarchyTokenizer(AttributeFactory factory, TextReader input, char delimiter, char replacement, int skip)
            : this(factory, input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
        {
        }

        public PathHierarchyTokenizer(TextReader input, int bufferSize, char delimiter, char replacement, int skip)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, bufferSize, delimiter, replacement, skip)
        {
        }

        public PathHierarchyTokenizer(AttributeFactory factory, TextReader input, int bufferSize, char delimiter, char replacement, int skip)
            : base(factory, input)
        {
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "skip cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            offsetAtt = AddAttribute<IOffsetAttribute>();
            posAtt = AddAttribute<IPositionIncrementAttribute>();
            termAtt = AddAttribute<ICharTermAttribute>();
            termAtt.ResizeBuffer(bufferSize);

            this.delimiter = delimiter;
            this.replacement = replacement;
            this.skip = skip;
            resultToken = new StringBuilder(bufferSize);
        }

        private const int DEFAULT_BUFFER_SIZE = 1024;
        public const char DEFAULT_DELIMITER = '/';
        public const int DEFAULT_SKIP = 0;

        private readonly char delimiter;
        private readonly char replacement;
        private readonly int skip;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posAtt;
        private int startPosition = 0;
        private int skipped = 0;
        private bool endDelimiter = false;
        private readonly StringBuilder resultToken;

        private int charsRead = 0;

        public override sealed bool IncrementToken()
        {
            ClearAttributes();
            termAtt.Append(resultToken);
            if (resultToken.Length == 0)
            {
                posAtt.PositionIncrement = 1;
            }
            else
            {
                posAtt.PositionIncrement = 0;
            }
            int length = 0;
            bool added = false;
            if (endDelimiter)
            {
                termAtt.Append(replacement);
                length++;
                endDelimiter = false;
                added = true;
            }

            while (true)
            {
                int c = m_input.Read();
                if (c >= 0)
                {
                    charsRead++;
                }
                else
                {
                    if (skipped > skip)
                    {
                        length += resultToken.Length;
                        termAtt.Length = length;
                        offsetAtt.SetOffset(CorrectOffset(startPosition), CorrectOffset(startPosition + length));
                        if (added)
                        {
                            resultToken.Length = 0;
                            resultToken.Append(termAtt.Buffer, 0, length);
                        }
                        return added;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (!added)
                {
                    added = true;
                    skipped++;
                    if (skipped > skip)
                    {
                        termAtt.Append(c == delimiter ? replacement : (char)c);
                        length++;
                    }
                    else
                    {
                        startPosition++;
                    }
                }
                else
                {
                    if (c == delimiter)
                    {
                        if (skipped > skip)
                        {
                            endDelimiter = true;
                            break;
                        }
                        skipped++;
                        if (skipped > skip)
                        {
                            termAtt.Append(replacement);
                            length++;
                        }
                        else
                        {
                            startPosition++;
                        }
                    }
                    else
                    {
                        if (skipped > skip)
                        {
                            termAtt.Append((char)c);
                            length++;
                        }
                        else
                        {
                            startPosition++;
                        }
                    }
                }
            }
            length += resultToken.Length;
            termAtt.Length = length;
            offsetAtt.SetOffset(CorrectOffset(startPosition), CorrectOffset(startPosition + length));
            resultToken.Length = 0;
            resultToken.Append(termAtt.Buffer, 0, length);
            return true;
        }

        public override sealed void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(charsRead);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            resultToken.Length = 0;
            charsRead = 0;
            endDelimiter = false;
            skipped = 0;
            startPosition = 0;
        }
    }
}