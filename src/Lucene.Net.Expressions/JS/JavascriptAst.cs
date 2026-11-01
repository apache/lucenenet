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

    // LUCENENET specific: syntax tree produced by JavascriptExpressionParser and
    // compiled to IL by JavascriptCompiler. Its shape mirrors the rules of the former
    // ANTLR grammar (Javascript.g4): an operator chain such as "a + b - c" is one
    // ChainNode with all its operands, and a parenthesized expression is its own node,
    // so the emitted IL is identical to what the parse-tree listener produced.

    internal abstract class JavascriptNode
    {
    }

    internal enum NumberKind
    {
        Decimal,
        Octal,
        Hex,
    }

    internal sealed class NumberNode : JavascriptNode
    {
        public NumberNode(NumberKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public NumberKind Kind { get; }

        /// <summary>The literal as written, including the <c>0x</c> or <c>0</c> prefix.</summary>
        public string Text { get; }
    }

    /// <summary>An external variable, such as <c>score</c> or <c>doc.field</c>.</summary>
    internal sealed class VariableNode : JavascriptNode
    {
        public VariableNode(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    internal sealed class CallNode : JavascriptNode
    {
        public CallNode(string name, JavascriptNode[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public JavascriptNode[] Arguments { get; }
    }

    internal enum UnaryOperator
    {
        Plus,
        Negate,
        BitNot,
        BoolNot,
    }

    internal sealed class UnaryNode : JavascriptNode
    {
        public UnaryNode(UnaryOperator op, JavascriptNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        public UnaryOperator Operator { get; }

        public JavascriptNode Operand { get; }
    }

    /// <summary>The precedence levels of the binary operators, lowest first.</summary>
    internal enum ChainLevel
    {
        LogicalOr,
        LogicalAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseAnd,
        Equality,
        Relational,
        Shift,
        Additive,
        Multiplicative,
    }

    internal enum BinaryOperator
    {
        BoolOr,
        BoolAnd,
        BitOr,
        BitXor,
        BitAnd,
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        ShiftLeft,
        ShiftRight,
        ShiftRightUnsigned,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
    }

    /// <summary>
    /// Two or more operands of the same precedence level, such as <c>a + b - c</c>:
    /// <c>Operators[i]</c> sits between <c>Operands[i]</c> and <c>Operands[i + 1]</c>.
    /// </summary>
    internal sealed class ChainNode : JavascriptNode
    {
        public ChainNode(ChainLevel level, JavascriptNode[] operands, BinaryOperator[] operators)
        {
            Level = level;
            Operands = operands;
            Operators = operators;
        }

        public ChainLevel Level { get; }

        public JavascriptNode[] Operands { get; }

        public BinaryOperator[] Operators { get; }

        /// <summary>Whether the chain is in parentheses, such that an operator following them starts a new chain.</summary>
        public bool Parenthesized { get; set; }
    }

    internal sealed class ConditionalNode : JavascriptNode
    {
        public ConditionalNode(JavascriptNode condition, JavascriptNode whenTrue, JavascriptNode whenFalse)
        {
            Condition = condition;
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        public JavascriptNode Condition { get; }

        public JavascriptNode WhenTrue { get; }

        public JavascriptNode WhenFalse { get; }
    }

    /// <summary>
    /// Mutable state shared by one parse: the end of the furthest token matched, used to
    /// report where a syntax error is when the parse fails.
    /// </summary>
    internal sealed class JavascriptParseState
    {
        public int FurthestTokenEnd;

        public void Track(int end)
        {
            if (end > FurthestTokenEnd)
            {
                FurthestTokenEnd = end;
            }
        }
    }
}
