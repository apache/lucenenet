// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Tokenizer for domain-like hierarchies.
    /// <para>
    /// Take something like:
    /// 
    /// <code>
    /// www.site.co.uk
    /// </code>
    /// 
    /// and make:
    /// 
    /// <code>
    /// www.site.co.uk
    /// site.co.uk
    /// co.uk
    /// uk
    /// </code>
    /// 
    /// </para>
    /// </summary>
    public class ReversePathHierarchyTokenizer : Tokenizer
    {
        public ReversePathHierarchyTokenizer(TextReader input)
            : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, int skip)
            : this(input, DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, int bufferSize, char delimiter)
            : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, char delimiter, char replacement)
            : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, int bufferSize, char delimiter, char replacement)
            : this(input, bufferSize, delimiter, replacement, DEFAULT_SKIP)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, char delimiter, int skip)
            : this(input, DEFAULT_BUFFER_SIZE, delimiter, delimiter, skip)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, char delimiter, char replacement, int skip)
            : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
        {
        }

        public ReversePathHierarchyTokenizer(AttributeFactory factory, TextReader input, char delimiter, char replacement, int skip)
            : this(factory, input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
        {
        }

        public ReversePathHierarchyTokenizer(TextReader input, int bufferSize, char delimiter, char replacement, int skip)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, bufferSize, delimiter, replacement, skip)
        {
        }

        public ReversePathHierarchyTokenizer(AttributeFactory factory, TextReader input, int bufferSize, char delimiter, char replacement, int skip)
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
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posAtt = AddAttribute<IPositionIncrementAttribute>();

            termAtt.ResizeBuffer(bufferSize);
            this.delimiter = delimiter;
            this.replacement = replacement;
            this.skip = skip;
            resultToken = new StringBuilder(bufferSize);
            resultTokenBuffer = new char[bufferSize];
            delimiterPositions = new JCG.List<int>(bufferSize / 10);
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

        private int endPosition = 0;
        private int finalOffset = 0;
        private int skipped = 0;
        private readonly StringBuilder resultToken;

        private readonly IList<int> delimiterPositions;
        private int delimitersCount = -1;
        private char[] resultTokenBuffer;

        public override sealed bool IncrementToken()
        {
            ClearAttributes();
            if (delimitersCount == -1)
            {
                int length = 0;
                delimiterPositions.Add(0);
                while (true)
                {
                    int c = m_input.Read();
                    if (c < 0)
                    {
                        break;
                    }
                    length++;
                    if (c == delimiter)
                    {
                        delimiterPositions.Add(length);
                        resultToken.Append(replacement);
                    }
                    else
                    {
                        resultToken.Append((char)c);
                    }
                }
                delimitersCount = delimiterPositions.Count;
                if (delimiterPositions[delimitersCount - 1] < length)
                {
                    delimiterPositions.Add(length);
                    delimitersCount++;
                }
                if (resultTokenBuffer.Length < resultToken.Length)
                {
                    resultTokenBuffer = new char[resultToken.Length];
                }
                resultToken.CopyTo(0, resultTokenBuffer, 0, resultToken.Length);
                resultToken.Length = 0;
                int idx = delimitersCount - 1 - skip;
                if (idx >= 0)
                {
                    // otherwise its ok, because we will skip and return false
                    endPosition = delimiterPositions[idx];
                }
                finalOffset = CorrectOffset(length);
                posAtt.PositionIncrement = 1;
            }
            else
            {
                posAtt.PositionIncrement = 0;
            }

            while (skipped < delimitersCount - skip - 1)
            {
                var start = delimiterPositions[skipped];
                termAtt.CopyBuffer(resultTokenBuffer, start, endPosition - start);
                offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(endPosition));
                skipped++;
                return true;
            }

            return false;
        }

        public override sealed void End()
        {
            base.End();
            // set final offset
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            base.Reset();
            resultToken.Length = 0;
            finalOffset = 0;
            endPosition = 0;
            skipped = 0;
            delimitersCount = -1;
            delimiterPositions.Clear();
        }
    }
}