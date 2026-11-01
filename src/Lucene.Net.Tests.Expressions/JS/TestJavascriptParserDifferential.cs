using J2N.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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

    // LUCENENET specific: compares the Parlot parser with the former ANTLR parser
    public class TestJavascriptParserDifferential : LuceneTestCase
    {
        // The chars the generated expressions use, '=' only appears in "==", "!=", "<=" and ">="
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_.,()<>=!&|?:+-*/%^~ \t\r\n";

        private static readonly string[] BinaryOperators =
        {
            "||", "&&", "|", "^", "&", "==", "!=", "<", ">", "<=", ">=", "<<", ">>", ">>>", "+", "-", "*", "/", "%",
        };

        private static readonly string[] UnaryOperators = { "+", "-", "~", "!" };

        private static readonly string[] Numbers =
        {
            "0", "1", "42", "3.5", "0.25", ".5", "1.", "1e3", "2.5E-2", "1e+2", ".5e1", "0x1F", "0XaB", "017", "00", "0777",
        };

        private static readonly string[] Names = { "a", "b_1", "_x", "doc.score", "x.y.z", "Popularity", "_" };

        private static readonly (string Name, int Arity)[] Functions =
        {
            ("abs", 1), ("max", 2), ("min", 2), ("haversin", 4), ("sqrt", 1), ("logn", 2), ("atan2", 2),
        };

        private static readonly string[] Junk =
        {
            "(", ")", ",", "?", ":", ".", "..", "0x", "1e", "08", "a.", "+", "*", "<", "<<<", "&&&", "!=", "=", "$", "@", "#", "'", "\"",
        };

        [Test]
        public void TestValidExpressionsCompileToTheSameIL()
        {
            var random = new Random(Random.Next());
            for (int i = 0; i < AtLeast(2000); i++)
            {
                string text = GenerateConditional(random, depth: 0);
                AssertSameCompilation(text);
            }
        }

        [Test]
        public void TestMutatedExpressionsAreRejectedAlike()
        {
            var random = new Random(Random.Next());
            int sameOffset = 0, differentOffset = 0, parlotOnlyRejects = 0;
            var samples = new List<string>();

            for (int i = 0; i < AtLeast(5000); i++)
            {
                string text = Mutate(random, GenerateConditional(random, depth: 0));
                Outcome antlr = Run(text, useAntlr: true);
                Outcome parlot = Run(text, useAntlr: false);

                bool antlrParsed = antlr.Error is not ParseException;
                bool parlotParsed = parlot.Error is not ParseException;

                if (antlrParsed && parlotParsed)
                {
                    AssertSameOutcome(text, antlr, parlot);
                    continue;
                }

                if (!antlrParsed && !parlotParsed)
                {
                    int antlrOffset = ((ParseException)antlr.Error).ErrorOffset;
                    int parlotOffset = ((ParseException)parlot.Error).ErrorOffset;
                    if (antlrOffset == parlotOffset)
                    {
                        sameOffset++;
                    }
                    else
                    {
                        differentOffset++;
                        if (samples.Count < 10 && text.Length < 80)
                        {
                            samples.Add($"'{Escape(text)}': ANTLR {antlrOffset}, Parlot {parlotOffset}");
                        }
                    }
                    continue;
                }

                // The ANTLR lexer drops the chars it has no token for, such as '$' or a lone '=', where Parlot
                // reports them. Without them, Parlot must accept the text too.
                Assert.IsTrue(antlrParsed, $"ANTLR rejects '{text}' but Parlot accepts it");
                string tokens = GetAntlrTokens(text);
                Assert.AreNotEqual(text.Replace(" ", ""), tokens.Replace(" ", ""), $"Parlot rejects '{text}' ({parlot.Error.Message}) but ANTLR accepts it");
                AssertSameOutcome(text, antlr, Run(tokens, useAntlr: false));
                parlotOnlyRejects++;
            }

            Console.WriteLine($"Same error offset: {sameOffset}, different: {differentOffset}, rejected by Parlot only (unknown chars): {parlotOnlyRejects}");
            foreach (string sample in samples)
            {
                Console.WriteLine(sample);
            }
        }

        [Test]
        public void TestKnownInputs()
        {
            foreach (string text in new[]
            {
                "1", " 1 ", "a<<b<<c", "(a<<b)<<c", "a>>>b>>c", "0x10<<2", "a&&b||c&&d", "a&b&c", "(a&b)&c", "a?b:c?d:e", "(a?b:c)?d:e",
                "!a", "!!a", "--a", "+-~!a", "a-  -b", "max(1,2)", "max ( 1 , 2 )", "haversin(1,2,3,4)", "doc.score*2", "1.e5", "017+0x1f",
                "a<=b>=c<d>e", "a==b!=c", "a\t\r\n+\nb",
            })
            {
                AssertSameCompilation(text);
            }

            foreach (string text in new[]
            {
                "", "   ", "()", "100 100", "7*/-8", "0y1234", "500EE", "500.5EE", "object.0invalid", "0.invalid", "object..invalid",
                ".invalid", "a.", "(1", "f(1", "f(1,)", "a ?", "a ? b", "a ? b :", "1 +", "a < < b", "a & & b", "a ! = b", "08", "0x",
                "1e", "a>>>>b", "max(1 2)", "1)", "a b", "unknown(1)", "max(1)", "sqrt(1, 2)",
            })
            {
                Outcome antlr = Run(text, useAntlr: true);
                Outcome parlot = Run(text, useAntlr: false);
                Assert.IsNotNull(antlr.Error, text);
                Assert.IsNotNull(parlot.Error, text);
                Assert.AreEqual(antlr.Error.GetType(), parlot.Error.GetType(), text);
            }

            // The ANTLR lexer skips the chars it has no token for
            foreach (string text in new[] { "a=b", "$a", "a+b#", "a<=b=", "1 @ + 2" })
            {
                Assert.IsNull(Run(text, useAntlr: true).Error, text);
                Assert.IsTrue(Run(text, useAntlr: false).Error is ParseException, text);
            }
        }

        // The text of the tokens the ANTLR lexer finds, separated by spaces
        private static string GetAntlrTokens(string text)
        {
            var lexer = new JavascriptLexer(new Antlr4.Runtime.AntlrInputStream(text));
            lexer.RemoveErrorListeners();
            return string.Join(" ", lexer.GetAllTokens().Select(t => t.Text));
        }

        private static void AssertSameCompilation(string text)
        {
            Outcome antlr = Run(text, useAntlr: true);
            Outcome parlot = Run(text, useAntlr: false);
            Assert.IsFalse(antlr.Error is ParseException, text + ": " + antlr.Error);
            Assert.IsFalse(parlot.Error is ParseException, text + ": " + parlot.Error);
            AssertSameOutcome(text, antlr, parlot);
        }

        // Either the same IL, or the same error when emitting it, such as a branch too long for Br_S
        private static void AssertSameOutcome(string text, Outcome antlr, Outcome parlot)
        {
            if (antlr.Error is not null || parlot.Error is not null)
            {
                Assert.AreEqual(antlr.Error?.GetType(), parlot.Error?.GetType(), text + ": " + antlr.Error + " / " + parlot.Error);
                return;
            }

            Assert.AreEqual(antlr.Expression.Variables, parlot.Expression.Variables, text);
            Assert.AreEqual(GetIL(antlr.Expression), GetIL(parlot.Expression), text);
        }

        private static string Escape(string text)
        {
            return text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static byte[] GetIL(Expression expression)
        {
            return expression.GetType().GetMethod(nameof(Expression.Evaluate)).GetMethodBody().GetILAsByteArray();
        }

        private struct Outcome
        {
            public Expression Expression;
            public Exception Error;
        }

        private static Outcome Run(string text, bool useAntlr)
        {
            try
            {
                return new Outcome
                {
                    Expression = useAntlr
                        ? JavascriptCompiler.CompileWithAntlr(text, JavascriptCompiler.DEFAULT_FUNCTIONS)
                        : JavascriptCompiler.Compile(text),
                };
            }
            catch (Exception e)
            {
                return new Outcome { Error = e };
            }
        }

        private static string Mutate(Random random, string text)
        {
            var builder = new StringBuilder(text);
            int mutations = 1 + random.Next(2);
            for (int i = 0; i < mutations; i++)
            {
                int position = random.Next(builder.Length + 1);
                switch (random.Next(3))
                {
                    case 0:
                        if (position < builder.Length)
                        {
                            builder.Remove(position, 1 + random.Next(Math.Min(3, builder.Length - position)));
                        }
                        break;
                    case 1:
                        builder.Insert(position, Junk[random.Next(Junk.Length)]);
                        break;
                    default:
                        builder.Insert(position, Alphabet[random.Next(Alphabet.Length)]);
                        break;
                }
            }
            return builder.ToString();
        }

        private static string Space(Random random)
        {
            return random.Next(4) switch
            {
                0 => " ",
                1 => random.Next(5) == 0 ? "\t\n " : "  ",
                _ => "",
            };
        }

        private static string GenerateConditional(Random random, int depth)
        {
            string condition = GenerateBinary(random, depth);
            if (depth < 3 && random.Next(8) == 0)
            {
                return condition + Space(random) + "?" + Space(random) + GenerateConditional(random, depth + 1)
                    + Space(random) + ":" + Space(random) + GenerateConditional(random, depth + 1);
            }
            return condition;
        }

        private static string GenerateBinary(Random random, int depth)
        {
            var builder = new StringBuilder(GenerateUnary(random, depth));
            int operators = depth > 3 ? 0 : random.Next(4);
            for (int i = 0; i < operators; i++)
            {
                builder.Append(Space(random)).Append(BinaryOperators[random.Next(BinaryOperators.Length)]).Append(Space(random));
                builder.Append(GenerateUnary(random, depth));
            }
            return builder.ToString();
        }

        private static string GenerateUnary(Random random, int depth)
        {
            if (random.Next(6) == 0)
            {
                // A space keeps "- -" from becoming "--", which is still two tokens for both parsers
                return UnaryOperators[random.Next(UnaryOperators.Length)] + Space(random) + GenerateUnary(random, depth);
            }

            switch (depth > 3 ? random.Next(2) : random.Next(5))
            {
                case 0:
                    return Numbers[random.Next(Numbers.Length)];
                case 1:
                    return Names[random.Next(Names.Length)];
                case 2:
                    return "(" + Space(random) + GenerateConditional(random, depth + 1) + Space(random) + ")";
                default:
                    var (name, arity) = Functions[random.Next(Functions.Length)];
                    var builder = new StringBuilder(name).Append(Space(random)).Append('(');
                    for (int i = 0; i < arity; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(Space(random)).Append(',');
                        }
                        builder.Append(Space(random)).Append(GenerateConditional(random, depth + 1));
                    }
                    return builder.Append(Space(random)).Append(')').ToString();
            }
        }
    }
}
