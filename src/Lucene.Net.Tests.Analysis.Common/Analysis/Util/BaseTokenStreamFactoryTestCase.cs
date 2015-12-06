using System;
using System.Collections;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.util
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


	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Base class for testing tokenstream factories. 
	/// <para>
	/// Example usage:
	/// <code><pre>
	///   Reader reader = new StringReader("Some Text to Analyze");
	///   reader = charFilterFactory("htmlstrip").create(reader);
	///   TokenStream stream = tokenizerFactory("standard").create(reader);
	///   stream = tokenFilterFactory("lowercase").create(stream);
	///   stream = tokenFilterFactory("asciifolding").create(stream);
	///   assertTokenStreamContents(stream, new String[] { "some", "text", "to", "analyze" });
	/// </pre></code>
	/// </para>
	/// </summary>
	// TODO: this has to be here, since the abstract factories are not in lucene-core,
	// so test-framework doesnt know about them...
	// this also means we currently cannot use this in other analysis modules :(
	// TODO: maybe after we improve the abstract factory/SPI apis, they can sit in core and resolve this.
	public abstract class BaseTokenStreamFactoryTestCase : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private AbstractAnalysisFactory analysisFactory(Class<? extends AbstractAnalysisFactory> clazz, org.apache.lucene.util.Version matchVersion, ResourceLoader loader, String... keysAndValues) throws Exception
	  private AbstractAnalysisFactory analysisFactory<T1>(Type<T1> clazz, Version matchVersion, ResourceLoader loader, params string[] keysAndValues) where T1 : AbstractAnalysisFactory
	  {
		if (keysAndValues.Length % 2 == 1)
		{
		  throw new System.ArgumentException("invalid keysAndValues map");
		}
		IDictionary<string, string> args = new Dictionary<string, string>();
		for (int i = 0; i < keysAndValues.Length; i += 2)
		{
		  string previous = args[keysAndValues[i]] = keysAndValues[i + 1];
		  assertNull("duplicate values for key: " + keysAndValues[i], previous);
		}
		if (matchVersion != null)
		{
		  string previous = args["luceneMatchVersion"] = matchVersion.ToString();
		  assertNull("duplicate values for key: luceneMatchVersion", previous);
		}
		AbstractAnalysisFactory factory = null;
		try
		{
		  factory = clazz.GetConstructor(typeof(IDictionary)).newInstance(args);
		}
		catch (InvocationTargetException e)
		{
		  // to simplify tests that check for illegal parameters
		  if (e.InnerException is System.ArgumentException)
		  {
			throw (System.ArgumentException) e.InnerException;
		  }
		  else
		  {
			throw e;
		  }
		}
		if (factory is ResourceLoaderAware)
		{
		  ((ResourceLoaderAware) factory).inform(loader);
		}
		return factory;
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenizerFactory with the specified name and key-value arguments.
	  /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
	  /// be on the test classpath.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenizerFactory tokenizerFactory(String name, String... keysAndValues) throws Exception
	  protected internal virtual TokenizerFactory tokenizerFactory(string name, params string[] keysAndValues)
	  {
		return tokenizerFactory(name, TEST_VERSION_CURRENT, keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenizerFactory with the specified name and key-value arguments.
	  /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
	  /// be on the test classpath.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenizerFactory tokenizerFactory(String name, org.apache.lucene.util.Version version, String... keysAndValues) throws Exception
	  protected internal virtual TokenizerFactory tokenizerFactory(string name, Version version, params string[] keysAndValues)
	  {
		return tokenizerFactory(name, version, new ClasspathResourceLoader(this.GetType()), keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenizerFactory with the specified name, version, resource loader, 
	  /// and key-value arguments.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenizerFactory tokenizerFactory(String name, org.apache.lucene.util.Version matchVersion, ResourceLoader loader, String... keysAndValues) throws Exception
	  protected internal virtual TokenizerFactory tokenizerFactory(string name, Version matchVersion, ResourceLoader loader, params string[] keysAndValues)
	  {
		return (TokenizerFactory) analysisFactory(TokenizerFactory.lookupClass(name), matchVersion, loader, keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenFilterFactory with the specified name and key-value arguments.
	  /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
	  /// be on the test classpath.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenFilterFactory tokenFilterFactory(String name, org.apache.lucene.util.Version version, String... keysAndValues) throws Exception
	  protected internal virtual TokenFilterFactory tokenFilterFactory(string name, Version version, params string[] keysAndValues)
	  {
		return tokenFilterFactory(name, version, new ClasspathResourceLoader(this.GetType()), keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenFilterFactory with the specified name and key-value arguments.
	  /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
	  /// be on the test classpath.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenFilterFactory tokenFilterFactory(String name, String... keysAndValues) throws Exception
	  protected internal virtual TokenFilterFactory tokenFilterFactory(string name, params string[] keysAndValues)
	  {
		return tokenFilterFactory(name, TEST_VERSION_CURRENT, keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized TokenFilterFactory with the specified name, version, resource loader, 
	  /// and key-value arguments.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TokenFilterFactory tokenFilterFactory(String name, org.apache.lucene.util.Version matchVersion, ResourceLoader loader, String... keysAndValues) throws Exception
	  protected internal virtual TokenFilterFactory tokenFilterFactory(string name, Version matchVersion, ResourceLoader loader, params string[] keysAndValues)
	  {
		return (TokenFilterFactory) analysisFactory(TokenFilterFactory.lookupClass(name), matchVersion, loader, keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized CharFilterFactory with the specified name and key-value arguments.
	  /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
	  /// be on the test classpath.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected CharFilterFactory charFilterFactory(String name, String... keysAndValues) throws Exception
	  protected internal virtual CharFilterFactory charFilterFactory(string name, params string[] keysAndValues)
	  {
		return charFilterFactory(name, TEST_VERSION_CURRENT, new ClasspathResourceLoader(this.GetType()), keysAndValues);
	  }

	  /// <summary>
	  /// Returns a fully initialized CharFilterFactory with the specified name, version, resource loader, 
	  /// and key-value arguments.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected CharFilterFactory charFilterFactory(String name, org.apache.lucene.util.Version matchVersion, ResourceLoader loader, String... keysAndValues) throws Exception
	  protected internal virtual CharFilterFactory charFilterFactory(string name, Version matchVersion, ResourceLoader loader, params string[] keysAndValues)
	  {
		return (CharFilterFactory) analysisFactory(CharFilterFactory.lookupClass(name), matchVersion, loader, keysAndValues);
	  }
	}

}