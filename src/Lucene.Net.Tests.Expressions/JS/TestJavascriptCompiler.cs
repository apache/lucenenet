using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    public class TestJavascriptCompiler : LuceneTestCase
    {
        [Test]
        public virtual void TestValidCompiles()
        {
            Assert.IsNotNull(JavascriptCompiler.Compile("100"));
            Assert.IsNotNull(JavascriptCompiler.Compile("valid0+100"));
            Assert.IsNotNull(JavascriptCompiler.Compile("valid0+\n100"));
            Assert.IsNotNull(JavascriptCompiler.Compile("logn(2, 20+10-5.0)"));
        }

        [Test]
        public virtual void TestValidNamespaces()
        {
            Assert.IsNotNull(JavascriptCompiler.Compile("object.valid0"));
            Assert.IsNotNull(JavascriptCompiler.Compile("object0.object1.valid1"));
        }

        [Test]
        public virtual void TestInvalidNamespaces()
        {
            try
            {
                JavascriptCompiler.Compile("object.0invalid");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                //expected
            }

            try
            {
                JavascriptCompiler.Compile("0.invalid");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                //expected
            }

            try
            {
                JavascriptCompiler.Compile("object..invalid");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                //expected
            }

            try
            {
                JavascriptCompiler.Compile(".invalid");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                //expected
            }
        }

        [Test]
        public virtual void TestInvalidCompiles()
        {
            try
            {
                JavascriptCompiler.Compile("100 100");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
            try
            {
                JavascriptCompiler.Compile("7*/-8");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }

            try
            {
                JavascriptCompiler.Compile("0y1234");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
            
            try
            {
                JavascriptCompiler.Compile("500EE");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
            
            try
            {
                JavascriptCompiler.Compile("500.5EE");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestEmpty()
        {
            try
            {
                JavascriptCompiler.Compile(string.Empty);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
            
            try
            {
                JavascriptCompiler.Compile("()");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
            
            try
            {
                JavascriptCompiler.Compile("   \r\n   \n \t");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsParseException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestNull()
        {
            try
            {
                JavascriptCompiler.Compile(null);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsNullPointerException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestWrongArity()
        {
            try
            {
                JavascriptCompiler.Compile("tan()");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("arguments for method call"));
            }

            try
            {
                JavascriptCompiler.Compile("tan(1, 1)");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                Assert.IsTrue(expected.Message.Contains("arguments for method call"));
            }
        }
    }
}
