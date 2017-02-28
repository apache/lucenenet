using Lucene.Net.Util;
using NUnit.Framework;
using System;

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
			IsNotNull(JavascriptCompiler.Compile("100"));
			IsNotNull(JavascriptCompiler.Compile("valid0+100"));
			IsNotNull(JavascriptCompiler.Compile("valid0+\n100"));
			IsNotNull(JavascriptCompiler.Compile("logn(2, 20+10-5.0)"));
		}

		[Test]
		public virtual void TestValidNamespaces()
		{
			IsNotNull(JavascriptCompiler.Compile("object.valid0"));
			IsNotNull(JavascriptCompiler.Compile("object0.object1.valid1"));
		}

        //TODO: change all exceptions to ParseExceptions
		[Test]
		public virtual void TestInvalidNamespaces()
		{
			try
			{
				JavascriptCompiler.Compile("object.0invalid");
				Fail();
			}
			catch (Exception)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("0.invalid");
				Fail();
			}
			catch (Exception)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("object..invalid");
				Fail();
			}
			catch (Exception)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile(".invalid");
				Fail();
			}
			catch (Exception)
			{
			}
		}

		//expected
		[Test]
		public virtual void TestInvalidCompiles()
		{
			try
			{
				JavascriptCompiler.Compile("100 100");
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("7*/-8");
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("0y1234");
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500EE");
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500.5EE");
				Fail();
			}
			catch (Exception)
			{
			}
		}

		[Test]
		public virtual void TestEmpty()
		{
			try
			{
				JavascriptCompiler.Compile(string.Empty);
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("()");
				Fail();
			}
			catch (Exception)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("   \r\n   \n \t");
				Fail();
			}
			catch (Exception)
			{
			}
		}

		// expected exception
		[Test]
		public virtual void TestNull()
		{
			try
			{
				JavascriptCompiler.Compile(null);
				Fail();
			}
			catch (ArgumentNullException)
			{
			}
		}

		// expected exception
		[Test]
		public virtual void TestWrongArity()
		{
			try
			{
				JavascriptCompiler.Compile("tan()");
				Fail();
			}
			catch (ArgumentException expected)
			{
				IsTrue(expected.Message.Contains("arguments for method call"
					));
			}
			try
			{
				JavascriptCompiler.Compile("tan(1, 1)");
				Fail();
			}
			catch (ArgumentException expected)
			{
				IsTrue(expected.Message.Contains("arguments for method call"
					));
			}
		}
	}
}
