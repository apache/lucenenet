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
			IsNotNull(JavascriptCompiler.Compile("100"));
			IsNotNull(JavascriptCompiler.Compile("valid0+100"));
			IsNotNull(JavascriptCompiler.Compile("valid0+\n100"));
			IsNotNull(JavascriptCompiler.Compile("logn(2, 20+10-5.0)")
				);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestValidNamespaces()
		{
			IsNotNull(JavascriptCompiler.Compile("object.valid0"));
			IsNotNull(JavascriptCompiler.Compile("object0.object1.valid1"
				));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestInvalidNamespaces()
		{
			try
			{
				JavascriptCompiler.Compile("object.0invalid");
				Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("0.invalid");
				Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile("object..invalid");
				Fail();
			}
			catch (ParseException)
			{
			}
			//expected
			try
			{
				JavascriptCompiler.Compile(".invalid");
				Fail();
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
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("7*/-8");
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("0y1234");
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500EE");
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("500.5EE");
				Fail();
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
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("()");
				Fail();
			}
			catch (ParseException)
			{
			}
			// expected exception
			try
			{
				JavascriptCompiler.Compile("   \r\n   \n \t");
				Fail();
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
				Fail();
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
