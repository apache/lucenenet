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
    /// is no runtime dependency on a parsing library. The token rules below are the lexer rules of the former
    /// ANTLR grammar, which matched the longest token.
    /// </remarks>
    internal static partial class JavascriptExpressionParser
    {
        /// <summary>Parses <paramref name="text"/>; on failure, <paramref name="failure"/> is the offset of the furthest failure.</summary>
        public static partial bool TryParse(string text, out JavascriptNode value, out int failure);

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
                ReadOnlySpan<char> rest = sourceText.AsSpan(offset);
                int length = Math.Max(ScanNamespaceId(rest), ScanNumber(rest));
                if (length == 0 && OperatorChars.IndexOf(rest[0]) < 0)
                {
                    message = "unexpected character '" + rest[0] + "' at position (" + offset + ").";
                }
                else
                {
                    message = "invalid sequence of tokens near '" + rest.Slice(0, Math.Max(length, 1)).ToString() + "' at position (" + offset + ").";
                }
            }

            throw new ParseException(message, offset);
        }

        private const string OperatorChars = "(),?:<>=!&|+-*/%^~";

        // The white space of the generated parser (WhiteSpaceSet.SpaceTabCrLf)
        private static bool IsWhiteSpace(char c) => c is ' ' or '\t' or '\n' or '\r';

        private static bool IsIdStart(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

        private static bool IsIdPart(char c) => IsIdStart(c) || IsDigit(c);

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';

        private static bool IsHexDigit(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        /// <summary>NAMESPACE_ID: <c>ID ('.' ID)*</c>, where ID is <c>[a-zA-Z_][a-zA-Z0-9_]*</c>.</summary>
        internal static int ScanNamespaceId(ReadOnlySpan<char> span)
        {
            int size = 0;
            while (size < span.Length && IsIdStart(span[size]))
            {
                size++;
                while (size < span.Length && IsIdPart(span[size]))
                {
                    size++;
                }

                // A dot continues the name only when an ID follows it
                if (size + 1 < span.Length && span[size] == '.' && IsIdStart(span[size + 1]))
                {
                    size++;
                }
                else
                {
                    break;
                }
            }

            return size;
        }

        /// <summary>The longest of HEX, OCTAL and DECIMAL.</summary>
        internal static int ScanNumber(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
            {
                return 0;
            }

            if (span[0] == '0' && span.Length > 1)
            {
                // HEX: ('0x'|'0X') HEXDIGIT+
                if (span[1] is 'x' or 'X')
                {
                    int hex = 2;
                    while (hex < span.Length && IsHexDigit(span[hex]))
                    {
                        hex++;
                    }

                    if (hex > 2)
                    {
                        return hex;
                    }
                }

                // OCTAL: '0' OCTALDIGIT+, always longer than the DECIMAL '0' it starts with
                int octal = 1;
                while (octal < span.Length && IsOctalDigit(span[octal]))
                {
                    octal++;
                }

                if (octal > 1)
                {
                    return octal;
                }
            }

            // DECIMAL: DECIMALINTEGER ('.' DECIMALDIGIT*)? EXPONENT? | '.' DECIMALDIGIT+ EXPONENT?
            int size;
            if (span[0] == '.')
            {
                size = 1;
                while (size < span.Length && IsDigit(span[size]))
                {
                    size++;
                }

                if (size == 1)
                {
                    return 0;
                }
            }
            else if (IsDigit(span[0]))
            {
                // DECIMALINTEGER: '0' | [1-9] DECIMALDIGIT*
                size = 1;
                if (span[0] != '0')
                {
                    while (size < span.Length && IsDigit(span[size]))
                    {
                        size++;
                    }
                }

                if (size < span.Length && span[size] == '.')
                {
                    size++;
                    while (size < span.Length && IsDigit(span[size]))
                    {
                        size++;
                    }
                }
            }
            else
            {
                return 0;
            }

            // EXPONENT: ('e'|'E') ('+'|'-')? DECIMALDIGIT+, only when complete
            if (size < span.Length && span[size] is 'e' or 'E')
            {
                int exponent = size + 1;
                if (exponent < span.Length && span[exponent] is '+' or '-')
                {
                    exponent++;
                }

                int digits = exponent;
                while (digits < span.Length && IsDigit(span[digits]))
                {
                    digits++;
                }

                if (digits > exponent)
                {
                    size = digits;
                }
            }

            return size;
        }

        internal static NumberNode CreateNumber(string text)
        {
            NumberKind kind = NumberKind.Decimal;
            if (text.Length > 1 && text[0] == '0')
            {
                if (text[1] is 'x' or 'X')
                {
                    kind = NumberKind.Hex;
                }
                else if (IsOctalDigit(text[1]))
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
