using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

    [TestFixture]
    public class TestCustomFunctions : LuceneTestCase
    {
        private const double DELTA = 0.0000001;

        /// <summary>empty list of methods</summary>
        [Test]
        public virtual void TestEmpty()
        {
            IDictionary<string, MethodInfo> functions = Collections.EmptyMap<string,MethodInfo>();
            try
            {
                JavascriptCompiler.Compile("sqrt(20)", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("Unrecognized method"));
            }
        }

        /// <summary>using the default map explicitly</summary>
        [Test]
        public virtual void TestDefaultList()
        {
            IDictionary<string, MethodInfo> functions = JavascriptCompiler.DEFAULT_FUNCTIONS;
            var expr = JavascriptCompiler.Compile("sqrt(20)", functions);
            Assert.AreEqual(Math.Sqrt(20), expr.Evaluate(0, null), DELTA);
        }

        public static double ZeroArgMethod() => 5;

        /// <summary>tests a method with no arguments</summary>
        [Test]
        public virtual void TestNoArgMethod()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("ZeroArgMethod");
            var expr = JavascriptCompiler.Compile("foo()", functions);
            Assert.AreEqual(5, expr.Evaluate(0, null), DELTA);
        }

        public static double OneArgMethod(double arg1) => 3 + arg1; 

        /// <summary>tests a method with one arguments</summary>
        [Test]
        public virtual void TestOneArgMethod()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("OneArgMethod", new []{ typeof(double)});
            var expr = JavascriptCompiler.Compile("foo(3)", functions);
            Assert.AreEqual(6, expr.Evaluate(0, null), DELTA);
        }

        public static double ThreeArgMethod(double arg1, double arg2, double arg3) => arg1 + arg2 + arg3;

        /// <summary>tests a method with three arguments</summary>
        [Test]
        public virtual void TestThreeArgMethod()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("ThreeArgMethod", new []{ typeof(double), typeof(double), typeof(double)});
            var expr = JavascriptCompiler.Compile("foo(3, 4, 5)", functions);
            Assert.AreEqual(12, expr.Evaluate(0, null), DELTA);
        }

        /// <summary>tests a map with 2 functions</summary>
        [Test]
        public virtual void TestTwoMethods()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("ZeroArgMethod");
            functions["bar"] = GetType().GetMethod("OneArgMethod", new []{typeof(double)});
            var expr = JavascriptCompiler.Compile("foo() + bar(3)", functions);
            Assert.AreEqual(11, expr.Evaluate(0, null), DELTA);
        }

        public static string BogusReturnType() => "bogus!"; 

        /// <summary>wrong return type: must be double</summary>
        [Test]
        public virtual void TestWrongReturnType()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("BogusReturnType");
            try
            {
                JavascriptCompiler.Compile("foo()", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("does not return a double"));
            }
        }

        public static double BogusParameterType(string s) => 0; 

        /// <summary>wrong param type: must be doubles</summary>
        [Test]
        public virtual void TestWrongParameterType()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("BogusParameterType", new []{ typeof(string)});
            try
            {
                JavascriptCompiler.Compile("foo(2)", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("must take only double parameters"));
            }
        }

        public virtual double NonStaticMethod() => 0; 

        /// <summary>wrong modifiers: must be static</summary>
        [Test]
        public virtual void TestWrongNotStatic()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("NonStaticMethod");
            try
            {
                JavascriptCompiler.Compile("foo()", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("is not static"));
            }
        }

        internal static double NonPublicMethod() => 0;

        /// <summary>wrong modifiers: must be public</summary>
        [Test]
        public virtual void TestWrongNotPublic()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = GetType().GetMethod("NonPublicMethod",BindingFlags.NonPublic|BindingFlags.Static);
                
            try
            {
                JavascriptCompiler.Compile("foo()", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("is not public"));
            }
        }

        internal class NestedNotPublic
        {
            public static double Method() => 0;
        }

        /// <summary>wrong class modifiers: class containing method is not public</summary>
        [Test]
        public virtual void TestWrongNestedNotPublic()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = typeof(NestedNotPublic).GetMethod("Method");
            try
            {
                JavascriptCompiler.Compile("foo()", functions);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                Assert.IsTrue(e.Message.Contains("is not public"));
            }
        }


        //LUCENENET: testClassLoader() was not ported.  (May not apply to Lucene.Net)
        
        
        internal static string MESSAGE = "This should not happen but it happens";

        public static class StaticThrowingException // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
        {
            public static double Method()
            {
                throw new ArithmeticException(MESSAGE);
            }
        }

        /// <summary>the method throws an exception.</summary>
        /// <remarks>We should check the stack trace that it contains the source code of the expression as file name.
        /// 	</remarks>
        [Test]
        public virtual void TestThrowingException()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo"] = typeof(StaticThrowingException).GetMethod("Method");
            string source = "3 * foo() / 5";
            var expr = JavascriptCompiler.Compile(source, functions);
            try
            {
                expr.Evaluate(0, null);
                Assert.Fail();
            }
            catch (Exception e) when (e.IsArithmeticException())
            {
                Assert.AreEqual(MESSAGE, e.Message);
                StringWriter sw = new StringWriter();
                e.printStackTrace();
                //.NET Port
                Assert.IsTrue(
                    // LUCENENET: Apparently in .NET 7, they finally fixed this weird display issue with spaces before the comma
                    // and closing parenthesis. It is a pass.
                    e.StackTrace.Contains("Lucene.Net.Expressions.CompiledExpression.Evaluate(Int32, FunctionValues[])") ||
                    e.StackTrace.Contains("Lucene.Net.Expressions.CompiledExpression.Evaluate(Int32 , FunctionValues[] )")
                    );
            }
        }

        /// <summary>test that namespaces work with custom expressions.</summary>
        [Test]
        public virtual void TestNamespaces()
        {
            IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
            functions["foo.bar"] = GetType().GetMethod("ZeroArgMethod");
            string source = "foo.bar()";
            var expr = JavascriptCompiler.Compile(source, functions);
            Assert.AreEqual(5, expr.Evaluate(0, null), DELTA);
        }
    }
}
