using System;
using System.IO;
using System.Threading;

namespace Lucene.Net.Analysis.Util
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
	/// Simple <seealso cref="ResourceLoader"/> that uses <seealso cref="ClassLoader#getResourceAsStream(String)"/>
	/// and <seealso cref="Class#forName(String,boolean,ClassLoader)"/> to open resources and
	/// classes, respectively.
	/// </summary>
	public sealed class ClasspathResourceLoader : ResourceLoader
	{
	  private readonly Type clazz;
	  private readonly ClassLoader loader;

	  /// <summary>
	  /// Creates an instance using the context classloader to load Resources and classes.
	  /// Resource paths must be absolute.
	  /// </summary>
	  public ClasspathResourceLoader() : this(Thread.CurrentThread.ContextClassLoader)
	  {
	  }

	  /// <summary>
	  /// Creates an instance using the given classloader to load Resources and classes.
	  /// Resource paths must be absolute.
	  /// </summary>
	  public ClasspathResourceLoader(ClassLoader loader) : this(null, loader)
	  {
	  }

	  /// <summary>
	  /// Creates an instance using the context classloader to load Resources and classes
	  /// Resources are resolved relative to the given class, if path is not absolute.
	  /// </summary>
	  public ClasspathResourceLoader(Type clazz) : this(clazz, clazz.ClassLoader)
	  {
	  }

	  private ClasspathResourceLoader(Type clazz, ClassLoader loader)
	  {
		this.clazz = clazz;
		this.loader = loader;
	  }

	  public Stream openResource(string resource)
	  {
		Stream stream = (clazz != null) ? clazz.getResourceAsStream(resource) : loader.getResourceAsStream(resource);
		if (stream == null)
		{
		  throw new IOException("Resource not found: " + resource);
		}
		return stream;
	  }

	  public Type findClass<T>(string cname, Type expectedType)
	  {
		try
		{
		  return Type.GetType(cname, true, loader).asSubclass(expectedType);
		}
		catch (Exception e)
		{
		  throw new Exception("Cannot load class: " + cname, e);
		}
	  }

	  public T newInstance<T>(string cname, Type expectedType)
	  {
		Type clazz = findClass(cname, expectedType);
		try
		{
		  return clazz.newInstance();
		}
		catch (Exception e)
		{
		  throw new Exception("Cannot create instance: " + cname, e);
		}
	  }
	}

}