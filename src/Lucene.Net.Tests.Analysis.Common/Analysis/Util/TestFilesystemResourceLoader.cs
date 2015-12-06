using System;
using System.Text;

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


	using IOUtils = org.apache.lucene.util.IOUtils;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestFilesystemResourceLoader : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void assertNotFound(ResourceLoader rl) throws Exception
	  private void assertNotFound(ResourceLoader rl)
	  {
		try
		{
		  IOUtils.closeWhileHandlingException(rl.openResource("/this-directory-really-really-really-should-not-exist/foo/bar.txt"));
		  fail("The resource does not exist, should fail!");
		}
		catch (IOException)
		{
		  // pass
		}
		try
		{
		  rl.newInstance("org.apache.lucene.analysis.FooBarFilterFactory", typeof(TokenFilterFactory));
		  fail("The class does not exist, should fail!");
		}
		catch (Exception)
		{
		  // pass
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void assertClasspathDelegation(ResourceLoader rl) throws Exception
	  private void assertClasspathDelegation(ResourceLoader rl)
	  {
		// try a stopwords file from classpath
		CharArraySet set = WordlistLoader.getSnowballWordSet(new System.IO.StreamReader(rl.openResource("org/apache/lucene/analysis/snowball/english_stop.txt"), Encoding.UTF8), TEST_VERSION_CURRENT);
		assertTrue(set.contains("you"));
		// try to load a class; we use string comparison because classloader may be different...
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
		assertEquals("org.apache.lucene.analysis.util.RollingCharBuffer", rl.newInstance("org.apache.lucene.analysis.util.RollingCharBuffer", typeof(object)).GetType().FullName);
		// theoretically classes should also be loadable:
		IOUtils.closeWhileHandlingException(rl.openResource("java/lang/String.class"));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBaseDir() throws Exception
	  public virtual void testBaseDir()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.File super = createTempDir("fsResourceLoaderBase").getAbsoluteFile();
		File @base = createTempDir("fsResourceLoaderBase").AbsoluteFile;
		try
		{
		  @base.mkdirs();
		  Writer os = new System.IO.StreamWriter(new System.IO.FileStream(@base, "template.txt", System.IO.FileMode.Create, System.IO.FileAccess.Write), Encoding.UTF8);
		  try
		  {
			os.write("foobar\n");
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(os);
		  }

		  ResourceLoader rl = new FilesystemResourceLoader(@base);
		  assertEquals("foobar", WordlistLoader.getLines(rl.openResource("template.txt"), StandardCharsets.UTF_8).get(0));
		  // Same with full path name:
		  string fullPath = (new File(@base, "template.txt")).ToString();
		  assertEquals("foobar", WordlistLoader.getLines(rl.openResource(fullPath), StandardCharsets.UTF_8).get(0));
		  assertClasspathDelegation(rl);
		  assertNotFound(rl);

		  // now use RL without base dir:
		  rl = new FilesystemResourceLoader();
		  assertEquals("foobar", WordlistLoader.getLines(rl.openResource((new File(@base, "template.txt")).ToString()), StandardCharsets.UTF_8).get(0));
		  assertClasspathDelegation(rl);
		  assertNotFound(rl);
		}
		finally
		{
		  TestUtil.rm(@base);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDelegation() throws Exception
	  public virtual void testDelegation()
	  {
		ResourceLoader rl = new FilesystemResourceLoader(null, new StringMockResourceLoader("foobar\n"));
		assertEquals("foobar", WordlistLoader.getLines(rl.openResource("template.txt"), StandardCharsets.UTF_8).get(0));
	  }

	}

}