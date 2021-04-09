using Lucene.Net.Expressions.JS;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    /// <summary>Tests validation of bindings</summary>
    public class TestExpressionValidation : LuceneTestCase
    {
        [Test]
        public virtual void TestValidExternals()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("valid0", SortFieldType.INT32));
            bindings.Add(new SortField("valid1", SortFieldType.INT32));
            bindings.Add(new SortField("valid2", SortFieldType.INT32));
            bindings.Add(new SortField("_score", SortFieldType.SCORE));
            bindings.Add("valide0", JavascriptCompiler.Compile("valid0 - valid1 + valid2 + _score"
                ));
            bindings.Validate();
            bindings.Add("valide1", JavascriptCompiler.Compile("valide0 + valid0"));
            bindings.Validate();
            bindings.Add("valide2", JavascriptCompiler.Compile("valide0 * valide1"));
            bindings.Validate();
        }

        [Test]
        public virtual void TestInvalidExternal()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("valid", SortFieldType.INT32));
            bindings.Add("invalid", JavascriptCompiler.Compile("badreference"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Invalid reference"));
            }
        }

        [Test]
        public virtual void TestInvalidExternal2()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add(new SortField("valid", SortFieldType.INT32));
            bindings.Add("invalid", JavascriptCompiler.Compile("valid + badreference"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Invalid reference"));
            }
        }

        [Test,Ignore("StackOverflowException can't be caught in .NET")]
        public virtual void TestSelfRecursion()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add("cycle0", JavascriptCompiler.Compile("cycle0"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Cycle detected"));
            }
        }

        [Test, Ignore("StackOverflowException can't be caught in .NET")]
        public virtual void TestCoRecursion()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add("cycle0", JavascriptCompiler.Compile("cycle1"));
            bindings.Add("cycle1", JavascriptCompiler.Compile("cycle0"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Cycle detected"));
            }
        }

        [Test, Ignore("StackOverflowException can't be caught in .NET")]
        public virtual void TestCoRecursion2()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add("cycle0", JavascriptCompiler.Compile("cycle1"));
            bindings.Add("cycle1", JavascriptCompiler.Compile("cycle2"));
            bindings.Add("cycle2", JavascriptCompiler.Compile("cycle0"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Cycle detected"));
            }
        }

        [Test, Ignore("StackOverflowException can't be caught in .NET")]
        public virtual void TestCoRecursion3()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add("cycle0", JavascriptCompiler.Compile("100"));
            bindings.Add("cycle1", JavascriptCompiler.Compile("cycle0 + cycle2"));
            bindings.Add("cycle2", JavascriptCompiler.Compile("cycle0 + cycle1"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Cycle detected"));
            }
        }

        [Test, Ignore("StackOverflowException can't be caught in .NET")]
        public virtual void TestCoRecursion4()
        {
            SimpleBindings bindings = new SimpleBindings();
            bindings.Add("cycle0", JavascriptCompiler.Compile("100"));
            bindings.Add("cycle1", JavascriptCompiler.Compile("100"));
            bindings.Add("cycle2", JavascriptCompiler.Compile("cycle1 + cycle0 + cycle3"));
            bindings.Add("cycle3", JavascriptCompiler.Compile("cycle0 + cycle1 + cycle2"));
            try
            {
                bindings.Validate();
                Assert.Fail("didn't get expected exception");
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("Cycle detected"));
            }
        }
    }
}
