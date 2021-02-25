using Lucene.Net.Util;
using NUnit.Framework;
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

    public class TestJavascriptOperations : LuceneTestCase
    {
        
        private void AssertEvaluatesTo(string expression, long expected)
        {
            Expression evaluator = JavascriptCompiler.Compile(expression);
            long actual = (long)evaluator.Evaluate(0, null);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public virtual void TestNegationOperation()
        {
            AssertEvaluatesTo("-1", -1);
            AssertEvaluatesTo("--1", 1);
            AssertEvaluatesTo("-(-1)", 1);
            AssertEvaluatesTo("-0", 0);
            AssertEvaluatesTo("--0", 0);
        }

        [Test]
        public virtual void TestAddOperation()
        {
            AssertEvaluatesTo("1+1", 2);
            AssertEvaluatesTo("1+0.5+0.5", 2);
            AssertEvaluatesTo("5+10", 15);
            AssertEvaluatesTo("1+1+2", 4);
            AssertEvaluatesTo("(1+1)+2", 4);
            AssertEvaluatesTo("1+(1+2)", 4);
            AssertEvaluatesTo("0+1", 1);
            AssertEvaluatesTo("1+0", 1);
            AssertEvaluatesTo("0+0", 0);
        }

        [Test]
        public virtual void TestSubtractOperation()
        {
            AssertEvaluatesTo("1-1", 0);
            AssertEvaluatesTo("5-10", -5);
            AssertEvaluatesTo("1-0.5-0.5", 0);
            AssertEvaluatesTo("1-1-2", -2);
            AssertEvaluatesTo("(1-1)-2", -2);
            AssertEvaluatesTo("1-(1-2)", 2);
            AssertEvaluatesTo("0-1", -1);
            AssertEvaluatesTo("1-0", 1);
            AssertEvaluatesTo("0-0", 0);
        }

        [Test]
        public virtual void TestMultiplyOperation()
        {
            AssertEvaluatesTo("1*1", 1);
            AssertEvaluatesTo("5*10", 50);
            AssertEvaluatesTo("50*0.1", 5);
            AssertEvaluatesTo("1*1*2", 2);
            AssertEvaluatesTo("(1*1)*2", 2);
            AssertEvaluatesTo("1*(1*2)", 2);
            AssertEvaluatesTo("10*0", 0);
            AssertEvaluatesTo("0*0", 0);
        }

        [Test]
        public virtual void TestDivisionOperation()
        {
            AssertEvaluatesTo("1*1", 1);
            AssertEvaluatesTo("10/5", 2);
            AssertEvaluatesTo("10/0.5", 20);
            AssertEvaluatesTo("10/5/2", 1);
            AssertEvaluatesTo("(27/9)/3", 1);
            AssertEvaluatesTo("27/(9/3)", 9);
            //.NET Port division overflow cast to double evals to long.MinValue
            AssertEvaluatesTo("1/0", -9223372036854775808);
        }

        [Test]
        public virtual void TestModuloOperation()
        {
            AssertEvaluatesTo("1%1", 0);
            AssertEvaluatesTo("10%3", 1);
            AssertEvaluatesTo("10%3%2", 1);
            AssertEvaluatesTo("(27%10)%4", 3);
            AssertEvaluatesTo("27%(9%5)", 3);
        }

        [Test]
        public virtual void TestLessThanOperation()
        {
            AssertEvaluatesTo("1 < 1", 0);
            AssertEvaluatesTo("2 < 1", 0);
            AssertEvaluatesTo("1 < 2", 1);
            AssertEvaluatesTo("2 < 1 < 3", 1);
            AssertEvaluatesTo("2 < (1 < 3)", 0);
            AssertEvaluatesTo("(2 < 1) < 1", 1);
            AssertEvaluatesTo("-1 < -2", 0);
            AssertEvaluatesTo("-1 < 0", 1);
        }

        [Test]
        public virtual void TestLessThanEqualsOperation()
        {
            AssertEvaluatesTo("1 <= 1", 1);
            AssertEvaluatesTo("2 <= 1", 0);
            AssertEvaluatesTo("1 <= 2", 1);
            AssertEvaluatesTo("1 <= 1 <= 0", 0);
            AssertEvaluatesTo("-1 <= -1", 1);
            AssertEvaluatesTo("-1 <= 0", 1);
            AssertEvaluatesTo("-1 <= -2", 0);
            AssertEvaluatesTo("-1 <= 0", 1);
        }

        [Test]
        public virtual void TestGreaterThanOperation()
        {
            AssertEvaluatesTo("1 > 1", 0);
            AssertEvaluatesTo("2 > 1", 1);
            AssertEvaluatesTo("1 > 2", 0);
            AssertEvaluatesTo("2 > 1 > 3", 0);
            AssertEvaluatesTo("2 > (1 > 3)", 1);
            AssertEvaluatesTo("(2 > 1) > 1", 0);
            AssertEvaluatesTo("-1 > -2", 1);
            AssertEvaluatesTo("-1 > 0", 0);
        }

        [Test]
        public virtual void TestGreaterThanEqualsOperation()
        {
            AssertEvaluatesTo("1 >= 1", 1);
            AssertEvaluatesTo("2 >= 1", 1);
            AssertEvaluatesTo("1 >= 2", 0);
            AssertEvaluatesTo("1 >= 1 >= 0", 1);
            AssertEvaluatesTo("-1 >= -1", 1);
            AssertEvaluatesTo("-1 >= 0", 0);
            AssertEvaluatesTo("-1 >= -2", 1);
            AssertEvaluatesTo("-1 >= 0", 0);
        }

        [Test]
        public virtual void TestEqualsOperation()
        {
            AssertEvaluatesTo("1 == 1", 1);
            AssertEvaluatesTo("0 == 0", 1);
            AssertEvaluatesTo("-1 == -1", 1);
            AssertEvaluatesTo("1.1 == 1.1", 1);
            AssertEvaluatesTo("0.9 == 0.9", 1);
            AssertEvaluatesTo("-0 == 0", 1);
            AssertEvaluatesTo("0 == 1", 0);
            AssertEvaluatesTo("1 == 2", 0);
            AssertEvaluatesTo("-1 == 1", 0);
            AssertEvaluatesTo("-1 == 0", 0);
            AssertEvaluatesTo("-2 == 1", 0);
            AssertEvaluatesTo("-2 == -1", 0);
        }

        [Test]
        public virtual void TestNotEqualsOperation()
        {
            AssertEvaluatesTo("1 != 1", 0);
            AssertEvaluatesTo("0 != 0", 0);
            AssertEvaluatesTo("-1 != -1", 0);
            AssertEvaluatesTo("1.1 != 1.1", 0);
            AssertEvaluatesTo("0.9 != 0.9", 0);
            AssertEvaluatesTo("-0 != 0", 0);
            AssertEvaluatesTo("0 != 1", 1);
            AssertEvaluatesTo("1 != 2", 1);
            AssertEvaluatesTo("-1 != 1", 1);
            AssertEvaluatesTo("-1 != 0", 1);
            AssertEvaluatesTo("-2 != 1", 1);
            AssertEvaluatesTo("-2 != -1", 1);
        }

        [Test]
        public virtual void TestBoolNotOperation()
        {
            AssertEvaluatesTo("!1", 0);
            AssertEvaluatesTo("!!1", 1);
            AssertEvaluatesTo("!0", 1);
            AssertEvaluatesTo("!!0", 0);
            AssertEvaluatesTo("!-1", 0);
            AssertEvaluatesTo("!2", 0);
            AssertEvaluatesTo("!-2", 0);
        }

        [Test]
        public virtual void TestBoolAndOperation()
        {
            AssertEvaluatesTo("1 && 1", 1);
            AssertEvaluatesTo("1 && 0", 0);
            AssertEvaluatesTo("0 && 1", 0);
            AssertEvaluatesTo("0 && 0", 0);
            AssertEvaluatesTo("-1 && -1", 1);
            AssertEvaluatesTo("-1 && 0", 0);
            AssertEvaluatesTo("0 && -1", 0);
            AssertEvaluatesTo("-0 && -0", 0);
        }

        [Test]
        public virtual void TestBoolOrOperation()
        {
            AssertEvaluatesTo("1 || 1", 1);
            AssertEvaluatesTo("1 || 0", 1);
            AssertEvaluatesTo("0 || 1", 1);
            AssertEvaluatesTo("0 || 0", 0);
            AssertEvaluatesTo("-1 || -1", 1);
            AssertEvaluatesTo("-1 || 0", 1);
            AssertEvaluatesTo("0 || -1", 1);
            AssertEvaluatesTo("-0 || -0", 0);
        }

        [Test]
        public virtual void TestConditionalOperation()
        {
            AssertEvaluatesTo("1 ? 2 : 3", 2);
            AssertEvaluatesTo("-1 ? 2 : 3", 2);
            AssertEvaluatesTo("0 ? 2 : 3", 3);
            AssertEvaluatesTo("1 ? 2 ? 3 : 4 : 5", 3);
            AssertEvaluatesTo("0 ? 2 ? 3 : 4 : 5", 5);
            AssertEvaluatesTo("1 ? 0 ? 3 : 4 : 5", 4);
            AssertEvaluatesTo("1 ? 2 : 3 ? 4 : 5", 2);
            AssertEvaluatesTo("0 ? 2 : 3 ? 4 : 5", 4);
            AssertEvaluatesTo("0 ? 2 : 0 ? 4 : 5", 5);
            AssertEvaluatesTo("(1 ? 1 : 0) ? 3 : 4", 3);
            AssertEvaluatesTo("(0 ? 1 : 0) ? 3 : 4", 4);
        }

        [Test]
        public virtual void TestBitShiftLeft()
        {
            AssertEvaluatesTo("1 << 1", 2);
            AssertEvaluatesTo("2 << 1", 4);
            AssertEvaluatesTo("-1 << 31", -2147483648);
            AssertEvaluatesTo("3 << 5", 96);
            AssertEvaluatesTo("-5 << 3", -40);
            AssertEvaluatesTo("4195 << 7", 536960);
            AssertEvaluatesTo("4195 << 66", 16780);
            AssertEvaluatesTo("4195 << 6", 268480);
            AssertEvaluatesTo("4195 << 70", 268480);
            AssertEvaluatesTo("-4195 << 70", -268480);
            AssertEvaluatesTo("-15 << 62", 1073741824);
        }

        [Test]
        public virtual void TestBitShiftRight()
        {
            AssertEvaluatesTo("1 >> 1", 0);
            AssertEvaluatesTo("2 >> 1", 1);
            AssertEvaluatesTo("-1 >> 5", -1);
            AssertEvaluatesTo("-2 >> 30", -1);
            AssertEvaluatesTo("-5 >> 1", -3);
            AssertEvaluatesTo("536960 >> 7", 4195);
            AssertEvaluatesTo("16780 >> 66", 4195);
            AssertEvaluatesTo("268480 >> 6", 4195);
            AssertEvaluatesTo("268480 >> 70", 4195);
            AssertEvaluatesTo("-268480 >> 70", -4195);
            AssertEvaluatesTo("-2147483646 >> 1", -1073741823);
        }

        [Test]
        public virtual void TestBitShiftRightUnsigned()
        {
            AssertEvaluatesTo("1 >>> 1", 0);
            AssertEvaluatesTo("2 >>> 1", 1);
            AssertEvaluatesTo("-1 >>> 37", 134217727);
            AssertEvaluatesTo("-2 >>> 62", 3);
            //.NET Port. CLR returns different values for unsigned shift ops
            AssertEvaluatesTo("-5 >>> 33", 2147483645);
            AssertEvaluatesTo("536960 >>> 7", 4195);
            AssertEvaluatesTo("16780 >>> 66", 4195);
            AssertEvaluatesTo("268480 >>> 6", 4195);
            AssertEvaluatesTo("268480 >>> 70", 4195);
            AssertEvaluatesTo("-268480 >>> 102", 67104669);
            AssertEvaluatesTo("2147483648 >>> 1", 1073741824);
        }

        [Test]
        public virtual void TestBitwiseAnd()
        {
            AssertEvaluatesTo("4 & 4", 4);
            AssertEvaluatesTo("3 & 2", 2);
            AssertEvaluatesTo("7 & 3", 3);
            AssertEvaluatesTo("-1 & -1", -1);
            AssertEvaluatesTo("-1 & 25", 25);
            AssertEvaluatesTo("3 & 7", 3);
            AssertEvaluatesTo("0 & 1", 0);
            AssertEvaluatesTo("1 & 0", 0);
        }

        [Test]
        public virtual void TestBitwiseOr()
        {
            AssertEvaluatesTo("4 | 4", 4);
            AssertEvaluatesTo("5 | 2", 7);
            AssertEvaluatesTo("7 | 3", 7);
            AssertEvaluatesTo("-1 | -5", -1);
            AssertEvaluatesTo("-1 | 25", -1);
            AssertEvaluatesTo("-100 | 15", -97);
            AssertEvaluatesTo("0 | 1", 1);
            AssertEvaluatesTo("1 | 0", 1);
        }

        [Test]
        public virtual void TestBitwiseXor()
        {
            AssertEvaluatesTo("4 ^ 4", 0);
            AssertEvaluatesTo("5 ^ 2", 7);
            AssertEvaluatesTo("15 ^ 3", 12);
            AssertEvaluatesTo("-1 ^ -5", 4);
            AssertEvaluatesTo("-1 ^ 25", -26);
            AssertEvaluatesTo("-100 ^ 15", -109);
            AssertEvaluatesTo("0 ^ 1", 1);
            AssertEvaluatesTo("1 ^ 0", 1);
            AssertEvaluatesTo("0 ^ 0", 0);
        }

        [Test]
        public virtual void TestBitwiseNot()
        {
            AssertEvaluatesTo("~-5", 4);
            AssertEvaluatesTo("~25", -26);
            AssertEvaluatesTo("~0", -1);
            AssertEvaluatesTo("~-1", 0);
        }

        [Test]
        public virtual void TestDecimalConst()
        {
            AssertEvaluatesTo("0", 0);
            AssertEvaluatesTo("1", 1);
            AssertEvaluatesTo("123456789", 123456789);
            AssertEvaluatesTo("5.6E2", 560);
            AssertEvaluatesTo("5.6E+2", 560);
            AssertEvaluatesTo("500E-2", 5);
        }

        [Test]
        public virtual void TestHexConst()
        {
            AssertEvaluatesTo("0x0", 0);
            AssertEvaluatesTo("0x1", 1);
            AssertEvaluatesTo("0xF", 15);
            AssertEvaluatesTo("0x1234ABCDEF", 78193085935L);
            AssertEvaluatesTo("1 << 0x1", 1 << 0x1);
            AssertEvaluatesTo("1 << 0xA", 1 << 0xA);
            AssertEvaluatesTo("0x1 << 2", 0x1 << 2);
            AssertEvaluatesTo("0xA << 2", 0xA << 2);
        }

        [Test]
        public virtual void TestHexConst2()
        {
            AssertEvaluatesTo("0X0", 0);
            AssertEvaluatesTo("0X1", 1);
            AssertEvaluatesTo("0XF", 15);
            AssertEvaluatesTo("0X1234ABCDEF", 78193085935L);  
        }

        [Test]
        public virtual void TestOctalConst()
        {
            AssertEvaluatesTo("00", 0);
            AssertEvaluatesTo("01", 1);
            AssertEvaluatesTo("010", 8);
            AssertEvaluatesTo("0123456777", 21913087);  // LUCENENET Comment: Javascript octal value via leading 0, compared with decimal value.
            AssertEvaluatesTo("1 << 01", 1 << 0x1);
            AssertEvaluatesTo("1 << 010", 1 << 0x8);
            AssertEvaluatesTo("01 << 2", 0x1 << 2);
            AssertEvaluatesTo("010 << 2", 0x8 << 2);
        }
    }
}
