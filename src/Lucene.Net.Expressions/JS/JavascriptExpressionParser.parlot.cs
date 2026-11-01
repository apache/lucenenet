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
    // Each token reports its end to the parse state, which locates syntax errors.
    internal static partial class JavascriptExpressionParser
    {
        [GenerateParser(nameof(TryParse))]
        private static Parser<JavascriptNode> Build(JavascriptParseState state)
        {
            var conditional = Deferred<JavascriptNode>();
            var unary = Deferred<JavascriptNode>();

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
                lparen.SkipAnd(conditional).AndSkip(rparen),
                number,
                nameOrCall);

            // unary: postfix | '+' unary | ('-' | '~' | '!') unary
            var unaryOperator = OneOf(
                    Terms.Char('+').Then(UnaryOperator.Plus),
                    Terms.Char('-').Then(UnaryOperator.Negate),
                    Terms.Char('~').Then(UnaryOperator.BitNot),
                    Terms.Char('!').Then(UnaryOperator.BoolNot))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            unary.Parser = OneOf(
                unaryOperator.And(unary).Then(static x => (JavascriptNode)new UnaryNode(x.Item1, x.Item2)),
                primary);

            var multiplicativeOperator = OneOf(
                    Terms.Char('*').Then(BinaryOperator.Multiply),
                    Terms.Char('/').Then(BinaryOperator.Divide),
                    Terms.Char('%').Then(BinaryOperator.Modulo))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var multiplicative = unary.And(ZeroOrMany(multiplicativeOperator.And(unary)))
                .Then(static x => CreateChain(ChainLevel.Multiplicative, x.Item1, x.Item2));

            var additiveOperator = OneOf(
                    Terms.Char('+').Then(BinaryOperator.Add),
                    Terms.Char('-').Then(BinaryOperator.Subtract))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var additive = multiplicative.And(ZeroOrMany(additiveOperator.And(multiplicative)))
                .Then(static x => CreateChain(ChainLevel.Additive, x.Item1, x.Item2));

            var shiftOperator = OneOf(
                    Terms.Text(">>>").Then(BinaryOperator.ShiftRightUnsigned),
                    Terms.Text(">>").Then(BinaryOperator.ShiftRight),
                    Terms.Text("<<").Then(BinaryOperator.ShiftLeft))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var shift = additive.And(ZeroOrMany(shiftOperator.And(additive)))
                .Then(static x => CreateChain(ChainLevel.Shift, x.Item1, x.Item2));

            // '<' and '>' never start an operand, so trying them where "<<" or ">>" failed can't succeed
            var relationalOperator = OneOf(
                    Terms.Text("<=").Then(BinaryOperator.LessThanOrEqual),
                    Terms.Text(">=").Then(BinaryOperator.GreaterThanOrEqual),
                    Terms.Char('<').Then(BinaryOperator.LessThan),
                    Terms.Char('>').Then(BinaryOperator.GreaterThan))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var relational = shift.And(ZeroOrMany(relationalOperator.And(shift)))
                .Then(static x => CreateChain(ChainLevel.Relational, x.Item1, x.Item2));

            var equalityOperator = OneOf(
                    Terms.Text("==").Then(BinaryOperator.Equal),
                    Terms.Text("!=").Then(BinaryOperator.NotEqual))
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var equality = relational.And(ZeroOrMany(equalityOperator.And(relational)))
                .Then(static x => CreateChain(ChainLevel.Equality, x.Item1, x.Item2));

            // Likewise '&' and '|' where "&&" or "||" follows: the second char doesn't start an operand
            var bitwiseAndOperator = Terms.Char('&').Then(BinaryOperator.BitAnd)
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var bitwiseAnd = equality.And(ZeroOrMany(bitwiseAndOperator.And(equality)))
                .Then(static x => CreateChain(ChainLevel.BitwiseAnd, x.Item1, x.Item2));

            var bitwiseXorOperator = Terms.Char('^').Then(BinaryOperator.BitXor)
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var bitwiseXor = bitwiseAnd.And(ZeroOrMany(bitwiseXorOperator.And(bitwiseAnd)))
                .Then(static x => CreateChain(ChainLevel.BitwiseXor, x.Item1, x.Item2));

            var bitwiseOrOperator = Terms.Char('|').Then(BinaryOperator.BitOr)
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var bitwiseOr = bitwiseXor.And(ZeroOrMany(bitwiseOrOperator.And(bitwiseXor)))
                .Then(static x => CreateChain(ChainLevel.BitwiseOr, x.Item1, x.Item2));

            var logicalAndOperator = Terms.Text("&&").Then(BinaryOperator.BoolAnd)
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var logicalAnd = bitwiseOr.And(ZeroOrMany(logicalAndOperator.And(bitwiseOr)))
                .Then(static x => CreateChain(ChainLevel.LogicalAnd, x.Item1, x.Item2));

            var logicalOrOperator = Terms.Text("||").Then(BinaryOperator.BoolOr)
                .Then((context, start, end, value) => { state.Track(end); return value; });

            var logicalOr = logicalAnd.And(ZeroOrMany(logicalOrOperator.And(logicalAnd)))
                .Then(static x => CreateChain(ChainLevel.LogicalOr, x.Item1, x.Item2));

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
