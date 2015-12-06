using System;

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


	/// <summary>
	/// Fake resource loader for tests: works if you want to fake reading a single file </summary>
	public class StringMockResourceLoader : ResourceLoader
	{
	  internal string text;

	  public StringMockResourceLoader(string text)
	  {
		this.text = text;
	  }

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override public <T> Class<? extends T> findClass(String cname, Class<T> expectedType)
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override public <T> Class<? extends T> findClass(String cname, Class<T> expectedType)
	  public override Type<?> findClass<T>(string cname, Type<T> expectedType) where ? : T
	  {
		try
		{
		  return Type.GetType(cname).asSubclass(expectedType);
		}
		catch (Exception e)
		{
		  throw new Exception("Cannot load class: " + cname, e);
		}
	  }

	  public override T newInstance<T>(string cname, Type<T> expectedType)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends T> clazz = findClass(cname, expectedType);
		Type<?> clazz = findClass(cname, expectedType);
		try
		{
		  return clazz.newInstance();
		}
		catch (Exception e)
		{
		  throw new Exception("Cannot create instance: " + cname, e);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public java.io.InputStream openResource(String resource) throws java.io.IOException
	  public override System.IO.Stream openResource(string resource)
	  {
		return new ByteArrayInputStream(text.GetBytes(StandardCharsets.UTF_8));
	  }
	}

}