/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions.JS
{
	public class TestJavascriptCompiler : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestValidCompiles()
		{
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("100"));
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("valid0+100"));
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("valid0+\n100"));
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("logn(2, 20+10-5.0)")
				);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestValidNamespaces()
		{
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("object.valid0"));
			NUnit.Framework.Assert.IsNotNull(JavascriptCompiler.Compile("object0.object1.valid1"
				));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestInvalidNamespaces()
		{
			try
			{
				JavascriptCompiler.Compile("object.0invalid");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("0.invalid");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("object..invalid");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile(".invalid");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
		}

		//expected
		/// <exception cref="System.Exception"></exception>
		public virtual void TestInvalidCompiles()
		{
			try
			{
				JavascriptCompiler.Compile("100 100");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("7*/-8");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("0y1234");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500EE");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500.5EE");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
		}

		// expected exception
		public virtual void TestEmpty()
		{
			try
			{
				JavascriptCompiler.Compile(string.Empty);
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("()");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("   \r\n   \n \t");
				NUnit.Framework.Assert.Fail();
			}
			catch (ParseException)
			{
			}
		}

		// expected exception
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNull()
		{
			try
			{
				JavascriptCompiler.Compile(null);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentNullException)
			{
			}
		}

		// expected exception
		/// <exception cref="System.Exception"></exception>
		public virtual void TestWrongArity()
		{
			try
			{
				JavascriptCompiler.Compile("tan()");
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException expected)
			{
				NUnit.Framework.Assert.IsTrue(expected.Message.Contains("arguments for method call"
					));
			}
			try
			{
				JavascriptCompiler.Compile("tan(1, 1)");
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException expected)
			{
				NUnit.Framework.Assert.IsTrue(expected.Message.Contains("arguments for method call"
					));
			}
		}
	}
}
