using System;
using System.Collections;
using System.Collections.Generic;
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
	/// Helper class for loading named SPIs from classpath (e.g. Tokenizers, TokenStreams).
	/// @lucene.internal
	/// </summary>
	internal sealed class AnalysisSPILoader<S> where S : AbstractAnalysisFactory
	{

	  private volatile IDictionary<string, Type> services = Collections.emptyMap();
	  private readonly Type clazz;
	  private readonly string[] suffixes;

	  public AnalysisSPILoader(Type clazz) : this(clazz, new string[] {clazz.SimpleName})
	  {
	  }

	  public AnalysisSPILoader(Type clazz, ClassLoader loader) : this(clazz, new string[] {clazz.SimpleName}, loader)
	  {
	  }

	  public AnalysisSPILoader(Type clazz, string[] suffixes) : this(clazz, suffixes, Thread.CurrentThread.ContextClassLoader)
	  {
	  }

	  public AnalysisSPILoader(Type clazz, string[] suffixes, ClassLoader classloader)
	  {
		this.clazz = clazz;
		this.suffixes = suffixes;
		// if clazz' classloader is not a parent of the given one, we scan clazz's classloader, too:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ClassLoader clazzClassloader = clazz.getClassLoader();
		ClassLoader clazzClassloader = clazz.ClassLoader;
		if (clazzClassloader != null && !SPIClassIterator.isParentClassLoader(clazzClassloader, classloader))
		{
		  reload(clazzClassloader);
		}
		reload(classloader);
	  }

	  /// <summary>
	  /// Reloads the internal SPI list from the given <seealso cref="ClassLoader"/>.
	  /// Changes to the service list are visible after the method ends, all
	  /// iterators (e.g., from <seealso cref="#availableServices()"/>,...) stay consistent. 
	  /// 
	  /// <para><b>NOTE:</b> Only new service providers are added, existing ones are
	  /// never removed or replaced.
	  /// 
	  /// </para>
	  /// <para><em>This method is expensive and should only be called for discovery
	  /// of new service providers on the given classpath/classloader!</em>
	  /// </para>
	  /// </summary>
	  public void reload(ClassLoader classloader)
	  {
		  lock (this)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.LinkedHashMap<String,Class> services = new java.util.LinkedHashMap<>(this.services);
			LinkedHashMap<string, Type> services = new LinkedHashMap<string, Type>(this.services);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.SPIClassIterator<S> loader = org.apache.lucene.util.SPIClassIterator.get(clazz, classloader);
			SPIClassIterator<S> loader = SPIClassIterator.get(clazz, classloader);
			while (loader.hasNext())
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Class service = loader.next();
			  Type service = loader.next();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String clazzName = service.getSimpleName();
			  string clazzName = service.SimpleName;
			  string name = null;
			  foreach (string suffix in suffixes)
			  {
				if (clazzName.EndsWith(suffix, StringComparison.Ordinal))
				{
				  name = clazzName.Substring(0, clazzName.Length - suffix.Length).ToLower(Locale.ROOT);
				  break;
				}
			  }
			  if (name == null)
			  {
				throw new ServiceConfigurationError("The class name " + service.Name + " has wrong suffix, allowed are: " + Arrays.ToString(suffixes));
			  }
			  // only add the first one for each name, later services will be ignored
			  // this allows to place services before others in classpath to make 
			  // them used instead of others
			  //
			  // TODO: Should we disallow duplicate names here?
			  // Allowing it may get confusing on collisions, as different packages
			  // could contain same factory class, which is a naming bug!
			  // When changing this be careful to allow reload()!
			  if (!services.containsKey(name))
			  {
				services.put(name, service);
			  }
			}
			this.services = Collections.unmodifiableMap(services);
		  }
	  }

	  public S newInstance(string name, IDictionary<string, string> args)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Class service = lookupClass(name);
		Type service = lookupClass(name);
		try
		{
		  return service.getConstructor(typeof(IDictionary)).newInstance(args);
		}
		catch (Exception e)
		{
		  throw new System.ArgumentException("SPI class of type " + clazz.Name + " with name '" + name + "' cannot be instantiated. " + "This is likely due to a misconfiguration of the java class '" + service.Name + "': ", e);
		}
	  }

	  public Type lookupClass(string name)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Class service = services.get(name.toLowerCase(java.util.Locale.ROOT));
		Type service = services[name.ToLower(Locale.ROOT)];
		if (service != null)
		{
		  return service;
		}
		else
		{
		  throw new System.ArgumentException("A SPI class of type " + clazz.Name + " with name '" + name + "' does not exist. " + "You need to add the corresponding JAR file supporting this SPI to your classpath. " + "The current classpath supports the following names: " + availableServices());
		}
	  }

	  public HashSet<string> availableServices()
	  {
		return services.Keys;
	  }
	}

}