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
	/// Simple <seealso cref="ResourceLoader"/> that opens resource files
	/// from the local file system, optionally resolving against
	/// a base directory.
	/// 
	/// <para>This loader wraps a delegate <seealso cref="ResourceLoader"/>
	/// that is used to resolve all files, the current base directory
	/// does not contain. <seealso cref="#newInstance"/> is always resolved
	/// against the delegate, as a <seealso cref="ClassLoader"/> is needed.
	/// 
	/// </para>
	/// <para>You can chain several {@code FilesystemResourceLoader}s
	/// to allow lookup of files in more than one base directory.
	/// </para>
	/// </summary>
	public sealed class FilesystemResourceLoader : ResourceLoader
	{
	  private readonly File baseDirectory;
	  private readonly ResourceLoader @delegate;

	  /// <summary>
	  /// Creates a resource loader that requires absolute filenames or relative to CWD
	  /// to resolve resources. Files not found in file system and class lookups
	  /// are delegated to context classloader.
	  /// </summary>
	  public FilesystemResourceLoader() : this((File) null)
	  {
	  }

	  /// <summary>
	  /// Creates a resource loader that resolves resources against the given
	  /// base directory (may be {@code null} to refer to CWD).
	  /// Files not found in file system and class lookups are delegated to context
	  /// classloader.
	  /// </summary>
	  public FilesystemResourceLoader(File baseDirectory) : this(baseDirectory, new ClasspathResourceLoader())
	  {
	  }

	  /// <summary>
	  /// Creates a resource loader that resolves resources against the given
	  /// base directory (may be {@code null} to refer to CWD).
	  /// Files not found in file system and class lookups are delegated
	  /// to the given delegate <seealso cref="ResourceLoader"/>.
	  /// </summary>
	  public FilesystemResourceLoader(File baseDirectory, ResourceLoader @delegate)
	  {
		if (baseDirectory != null && !baseDirectory.Directory)
		{
		  throw new System.ArgumentException("baseDirectory is not a directory or null");
		}
		if (@delegate == null)
		{
		  throw new System.ArgumentException("delegate ResourceLoader may not be null");
		}
		this.baseDirectory = baseDirectory;
		this.@delegate = @delegate;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public java.io.InputStream openResource(String resource) throws java.io.IOException
	  public InputStream openResource(string resource)
	  {
		try
		{
		  File file = new File(resource);
		  if (baseDirectory != null && !file.Absolute)
		  {
			file = new File(baseDirectory, resource);
		  }
		  return new FileInputStream(file);
		}
		catch (FileNotFoundException)
		{
		  return @delegate.openResource(resource);
		}
	  }

	  public T newInstance<T>(string cname, Type expectedType)
	  {
		return @delegate.newInstance(cname, expectedType);
	  }

	  public Type findClass<T>(string cname, Type expectedType)
	  {
		return @delegate.findClass(cname, expectedType);
	  }
	}

}