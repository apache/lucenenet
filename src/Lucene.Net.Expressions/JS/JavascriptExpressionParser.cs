using J2N.Text;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Expressions.JS
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
    /// LUCENENET specific: parses the javascript expression syntax into a <see cref="JavascriptNode"/> tree.
    /// </summary>
    /// <remarks>
    /// The grammar is in <c>JavascriptExpressionParser.parlot.cs</c>. That file is only read at build time by
    /// the Parlot source generator, which emits the body of <see cref="TryParse"/> into this assembly, so there
    /// is no runtime dependency on a parsing library. The token rules are there too.
    /// </remarks>
    internal static partial class JavascriptExpressionParser
    {
        /// <summary>Parses <paramref name="text"/>; on failure, <paramref name="failure"/> is the offset of the furthest failure.</summary>
        public static partial bool TryParse(string text, out JavascriptNode value, out int failure);

        /// <summary>Gets the length of the name or number <paramref name="text"/> starts with, to report it in an error.</summary>
        private static partial bool TryParseToken(string text, out int length);

        /// <summary>
        /// Parses <paramref name="sourceText"/> or throws a <see cref="ParseException"/> with the offset of the
        /// first token that could not be parsed.
        /// </summary>
        public static JavascriptNode Parse(string sourceText)
        {
            if (TryParse(sourceText, out JavascriptNode node, out int offset))
            {
                return node;
            }

            while (offset < sourceText.Length && IsWhiteSpace(sourceText[offset]))
            {
                offset++;
            }

            // The messages of the ANTLR 3 parser in Lucene.NET 4.8.0-beta00017 and earlier
            string message;
            if (offset >= sourceText.Length)
            {
                message = "unexpected end of expression.";
            }
            else
            {
                string rest = sourceText.Substring(offset);
                if (!TryParseToken(rest, out int length))
                {
                    length = 0;
                }

                if (length == 0 && OperatorChars.IndexOf(rest[0]) < 0)
                {
                    message = "unexpected character '" + rest[0] + "' at position (" + offset + ").";
                }
                else
                {
                    message = "invalid sequence of tokens near '" + rest.Substring(0, Math.Max(length, 1)) + "' at position (" + offset + ").";
                }
            }

            throw new ParseException(message, offset);
        }

        private const string OperatorChars = "(),?:<>=!&|+-*/%^~";

        // The white space of the generated parser (WhiteSpaceSet.SpaceTabCrLf)
        private static bool IsWhiteSpace(char c) => c is ' ' or '\t' or '\n' or '\r';

        internal static NumberNode CreateNumber(string text)
        {
            NumberKind kind = NumberKind.Decimal;
            if (text.Length > 1 && text[0] == '0')
            {
                if (text[1] is 'x' or 'X')
                {
                    kind = NumberKind.Hex;
                }
                else if (text[1] >= '0' && text[1] <= '7')
                {
                    kind = NumberKind.Octal;
                }
            }

            return new NumberNode(kind, text);
        }

        /// <summary>
        /// Combines two operands, extending <paramref name="left"/> when it is a chain of the same
        /// <paramref name="level"/> not in parentheses, as the ANTLR grammar rules did.
        /// </summary>
        internal static JavascriptNode CreateChain(ChainLevel level, BinaryOperator op, JavascriptNode left, JavascriptNode right)
        {
            if (left is ChainNode chain && chain.Level == level && !chain.Parenthesized)
            {
                int count = chain.Operators.Length;
                var operands = new JavascriptNode[count + 2];
                var operators = new BinaryOperator[count + 1];
                Array.Copy(chain.Operands, operands, count + 1);
                Array.Copy(chain.Operators, operators, count);
                operands[count + 1] = right;
                operators[count] = op;
                return new ChainNode(level, operands, operators);
            }

            return new ChainNode(level, new[] { left, right }, new[] { op });
        }

        internal static JavascriptNode Parenthesize(JavascriptNode node)
        {
            if (node is ChainNode chain)
            {
                chain.Parenthesized = true;
            }

            return node;
        }

        internal static JavascriptNode CreateCall(string name, IReadOnlyList<JavascriptNode> arguments)
        {
            var array = new JavascriptNode[arguments.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = arguments[i];
            }

            return new CallNode(name, array);
        }
    }
}
