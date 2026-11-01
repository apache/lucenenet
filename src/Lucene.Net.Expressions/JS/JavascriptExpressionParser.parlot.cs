using Parlot;
using Parlot.Fluent;
using Parlot.SourceGenerator;
using System.Collections.Generic;
using static Parlot.Fluent.Parsers;

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

    // LUCENENET specific: the grammar of the javascript expression syntax, a port of the
    // former ANTLR grammar (Javascript.g4) to Parlot. This file is build-only: the Parlot
    // source generator runs it at compile time and emits JavascriptExpressionParser.TryParse.
    // The tokens and the start of each operand report their position to the parse state,
    // which locates syntax errors.
    internal static partial class JavascriptExpressionParser
    {
        [GenerateParser(nameof(TryParse))]
        private static Parser<JavascriptNode> Build(JavascriptParseState state)
        {
            var conditional = Deferred<JavascriptNode>();

            var lparen = Terms.Char('(').Then((context, start, end, value) => { state.Track(end); return value; });
            var rparen = Terms.Char(')').Then((context, start, end, value) => { state.Track(end); return value; });
            var comma = Terms.Char(',').Then((context, start, end, value) => { state.Track(end); return value; });
            var question = Terms.Char('?').Then((context, start, end, value) => { state.Track(end); return value; });
            var colon = Terms.Char(':').Then((context, start, end, value) => { state.Track(end); return value; });

            var number = Terms.Scan(ScanNumber, "0123456789.")
                .Then((context, start, end, value) => { state.Track(end); return (JavascriptNode)CreateNumber(value.ToString()); });

            var namespaceId = Terms.Scan(ScanNamespaceId, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_")
                .Then((context, start, end, value) => { state.Track(end); return value.ToString(); });

            // arguments: '(' (conditional (',' conditional)*)? ')'
            var arguments = lparen.SkipAnd(Separated(comma, conditional, min: 0)).AndSkip(rparen);

            // postfix: primary | call, where a name without arguments is a variable
            var nameOrCall = namespaceId.And(arguments.Optional())
                .Then(static x => x.Item2.TryGetValue(out var args) ? CreateCall(x.Item1, args) : new VariableNode(x.Item1));

            // primary: NAMESPACE_ID | numeric | '(' conditional ')'
            var primary = OneOf(
                lparen.SkipAnd(conditional).AndSkip(rparen).Then(static node => Parenthesize(node)),
                number,
                nameOrCall);

            // Records where an operand starts, after an operator, so that a missing operand reports
            // the end of the operator rather than the end of the previous operand
            var operand = Always(0)
                .Then((context, start, end, value) => { state.Track(start); return value; })
                .SkipAnd(primary);

            // unary: postfix | '+' unary | ('-' | '~' | '!') unary, then the binary operators from the tightest
            // level. An operator not followed by an operand, such as '&' at the start of "&&", is tried again in
            // the looser levels.
            var logicalOr = operand.Operators()
                .Unary(
                    (Terms.Char('+'), static x => new UnaryNode(UnaryOperator.Plus, x)),
                    (Terms.Char('-'), static x => new UnaryNode(UnaryOperator.Negate, x)),
                    (Terms.Char('~'), static x => new UnaryNode(UnaryOperator.BitNot, x)),
                    (Terms.Char('!'), static x => new UnaryNode(UnaryOperator.BoolNot, x)))
                .LeftAssociative(
                    (Terms.Char('*'), static (a, b) => CreateChain(ChainLevel.Multiplicative, BinaryOperator.Multiply, a, b)),
                    (Terms.Char('/'), static (a, b) => CreateChain(ChainLevel.Multiplicative, BinaryOperator.Divide, a, b)),
                    (Terms.Char('%'), static (a, b) => CreateChain(ChainLevel.Multiplicative, BinaryOperator.Modulo, a, b)))
                .LeftAssociative(
                    (Terms.Char('+'), static (a, b) => CreateChain(ChainLevel.Additive, BinaryOperator.Add, a, b)),
                    (Terms.Char('-'), static (a, b) => CreateChain(ChainLevel.Additive, BinaryOperator.Subtract, a, b)))
                .LeftAssociative(
                    (Terms.Text(">>>"), static (a, b) => CreateChain(ChainLevel.Shift, BinaryOperator.ShiftRightUnsigned, a, b)),
                    (Terms.Text(">>"), static (a, b) => CreateChain(ChainLevel.Shift, BinaryOperator.ShiftRight, a, b)),
                    (Terms.Text("<<"), static (a, b) => CreateChain(ChainLevel.Shift, BinaryOperator.ShiftLeft, a, b)))
                .LeftAssociative(
                    (Terms.Text("<="), static (a, b) => CreateChain(ChainLevel.Relational, BinaryOperator.LessThanOrEqual, a, b)),
                    (Terms.Text(">="), static (a, b) => CreateChain(ChainLevel.Relational, BinaryOperator.GreaterThanOrEqual, a, b)),
                    (Terms.Text("<"), static (a, b) => CreateChain(ChainLevel.Relational, BinaryOperator.LessThan, a, b)),
                    (Terms.Text(">"), static (a, b) => CreateChain(ChainLevel.Relational, BinaryOperator.GreaterThan, a, b)))
                .LeftAssociative(
                    (Terms.Text("=="), static (a, b) => CreateChain(ChainLevel.Equality, BinaryOperator.Equal, a, b)),
                    (Terms.Text("!="), static (a, b) => CreateChain(ChainLevel.Equality, BinaryOperator.NotEqual, a, b)))
                .LeftAssociative(
                    (Terms.Char('&'), static (a, b) => CreateChain(ChainLevel.BitwiseAnd, BinaryOperator.BitAnd, a, b)))
                .LeftAssociative(
                    (Terms.Char('^'), static (a, b) => CreateChain(ChainLevel.BitwiseXor, BinaryOperator.BitXor, a, b)))
                .LeftAssociative(
                    (Terms.Char('|'), static (a, b) => CreateChain(ChainLevel.BitwiseOr, BinaryOperator.BitOr, a, b)))
                .LeftAssociative(
                    (Terms.Text("&&"), static (a, b) => CreateChain(ChainLevel.LogicalAnd, BinaryOperator.BoolAnd, a, b)))
                .LeftAssociative(
                    (Terms.Text("||"), static (a, b) => CreateChain(ChainLevel.LogicalOr, BinaryOperator.BoolOr, a, b)));

            // conditional: logical_or ('?' conditional ':' conditional)?
            conditional.Parser = logicalOr.And(question.SkipAnd(conditional).AndSkip(colon).And(conditional).Optional())
                .Then(static x => x.Item2.TryGetValue(out var branches)
                    ? new ConditionalNode(x.Item1, branches.Item1, branches.Item2)
                    : x.Item1);

            // expression: conditional EOF, where Eof() itself doesn't skip trailing white space
            return conditional.AndSkip(SkipWhiteSpace(Eof())).WithWhiteSpaceSet(WhiteSpaceSet.Ascii);
        }
    }
}
