using Lucene.Net.Expressions.JS;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Expressions
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

    public class TestExpressionSortField : LuceneTestCase
    {
        [Test]
        public virtual void TestToString()
        {
            Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("_score", SortFieldType.SCORE));
            bindings.Add(new SortField("popularity", SortFieldType.INT32));
            SortField sf = expr.GetSortField(bindings, true);
            Assert.AreEqual("<expr \"sqrt(_score) + ln(popularity)\">!", sf.ToString());
        }

        [Test]
        public virtual void TestEquals()
        {
            Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");

            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("_score", SortFieldType.SCORE));
            bindings.Add(new SortField("popularity", SortFieldType.INT32));

            SimpleBindings otherBindings = new SimpleBindings();
            otherBindings.Add(new SortField("_score", SortFieldType.INT64));
            otherBindings.Add(new SortField("popularity", SortFieldType.INT32));

            SortField sf1 = expr.GetSortField(bindings, true);

            // different order
            SortField sf2 = expr.GetSortField(bindings, false);
            Assert.IsFalse(sf1.Equals(sf2));

            // different bindings
            sf2 = expr.GetSortField(otherBindings, true);
            Assert.IsFalse(sf1.Equals(sf2));

            // different expression
            Expression other = JavascriptCompiler.Compile("popularity/2");
            sf2 = other.GetSortField(bindings, true);
            Assert.IsFalse(sf1.Equals(sf2));

            // null
            Assert.IsFalse(sf1.Equals(null));

            // same instance:
            Assert.AreEqual(sf1, sf1);
        }

        [Test]
        public virtual void TestNeedsScores()
        {
            SimpleBindings bindings = new SimpleBindings();
            // refers to score directly
            Expression exprA = JavascriptCompiler.Compile("_score");
            // constant
            Expression exprB = JavascriptCompiler.Compile("0");
            // field
            Expression exprC = JavascriptCompiler.Compile("intfield");

            // score + constant
            Expression exprD = JavascriptCompiler.Compile("_score + 0");
            // field + constant
            Expression exprE = JavascriptCompiler.Compile("intfield + 0");

            // expression + constant (score ref'd)
            Expression exprF = JavascriptCompiler.Compile("a + 0");
            // expression + constant
            Expression exprG = JavascriptCompiler.Compile("e + 0");

            // several variables (score ref'd)
            Expression exprH = JavascriptCompiler.Compile("b / c + e * g - sqrt(f)");
            // several variables
            Expression exprI = JavascriptCompiler.Compile("b / c + e * g");

            bindings.Add(new SortField("_score", SortFieldType.SCORE));
            bindings.Add(new SortField("intfield", SortFieldType.INT32));
            bindings.Add("a", exprA);
            bindings.Add("b", exprB);
            bindings.Add("c", exprC);
            bindings.Add("d", exprD);
            bindings.Add("e", exprE);
            bindings.Add("f", exprF);
            bindings.Add("g", exprG);
            bindings.Add("h", exprH);
            bindings.Add("i", exprI);

            Assert.IsTrue(exprA.GetSortField(bindings, true).NeedsScores);
            Assert.IsFalse(exprB.GetSortField(bindings, true).NeedsScores);
            Assert.IsFalse(exprC.GetSortField(bindings, true).NeedsScores);
            Assert.IsTrue(exprD.GetSortField(bindings, true).NeedsScores);
            Assert.IsFalse(exprE.GetSortField(bindings, true).NeedsScores);
            Assert.IsTrue(exprF.GetSortField(bindings, true).NeedsScores);
            Assert.IsFalse(exprG.GetSortField(bindings, true).NeedsScores);
            Assert.IsTrue(exprH.GetSortField(bindings, true).NeedsScores);
            Assert.IsFalse(exprI.GetSortField(bindings, false).NeedsScores);
        }
    }
}
