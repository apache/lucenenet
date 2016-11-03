using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using Microsoft.Extensions.DependencyModel;
#endif

namespace Lucene.Net.Util
{
    //LUCENE TO-DO Whole file
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
    /// Helper class for loading SPI classes from classpath (META-INF files).
    /// this is a light impl of <seealso cref="java.util.ServiceLoader"/> but is guaranteed to
    /// be bug-free regarding classpath order and does not instantiate or initialize
    /// the classes found.
    ///
    /// @lucene.internal
    /// </summary>
    ///

    public class SPIClassIterator<S> : IEnumerable<Type>
    {
        private static HashSet<Type> types;

        static SPIClassIterator()
        {
            types = new HashSet<Type>();

            // .NET Port Hack: We do a 2-level deep check here because if the assembly you're
            // hoping would be loaded hasn't been loaded yet into the app domain,
            // it is unavailable. So we go to the next level on each and check each referenced
            // assembly.
#if NETSTANDARD
            var dependencyContext = DependencyContext.Default;
            var assemblyNames = dependencyContext.RuntimeLibraries
                .SelectMany(lib => lib.GetDefaultAssemblyNames(dependencyContext))
                .Where(x => !DotNetFrameworkFilter.IsFrameworkAssembly(x))
                .Distinct();
            var assembliesLoaded = LoadAssemblyFromName(assemblyNames);
#else
            var assembliesLoaded = AppDomain.CurrentDomain.GetAssemblies();
#endif
            assembliesLoaded = assembliesLoaded.Where(x => !DotNetFrameworkFilter.IsFrameworkAssembly(x)).ToArray();

            var referencedAssemblies = assembliesLoaded
                .SelectMany(assembly =>
                {
                    return assembly
                        .GetReferencedAssemblies()
                        .Where(reference => !DotNetFrameworkFilter.IsFrameworkAssembly(reference))
                        .Select(assemblyName => LoadAssemblyFromName(assemblyName));
                })
                .Where(x => x != null)
                .Distinct();

            var assembliesToExamine = assembliesLoaded.Concat(referencedAssemblies).Distinct();

            foreach (var assembly in assembliesToExamine)
            {
                try
                {
                    foreach (var type in assembly.GetTypes().Where(x => x.IsPublic))
                    {
                        try
                        {
                            if (!IsInvokableSubclassOf<S>(type))
                            {
                                continue;
                            }

                            // We are looking for types with a default ctor
                            // (which is used in NamedSPILoader) or has a single parameter
                            // of type IDictionary<string, string> (for AnalysisSPILoader)
                            var matchingCtors = type.GetConstructors().Where(ctor =>
                            {
                                var parameters = ctor.GetParameters();

                                switch (parameters.Length)
                                {
                                    case 0: // default ctor
                                        return true;
                                    case 1:
                                        return typeof(IDictionary<string, string>).IsAssignableFrom(parameters[0].ParameterType);
                                    default:
                                        return false;
                                }
                            });

                            if (matchingCtors.Any())
                            {
                                types.Add(type);
                            }
                        }
                        catch
                        {
                            // swallow
                        }
                    }
                }
                catch
                {
                    // swallow
                }
            }
        }

        internal static bool IsInvokableSubclassOf<S>(Type type)
        {
            return typeof(S).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
        }

        public static SPIClassIterator<S> Get()
        {
            return new SPIClassIterator<S>();
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return types.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static IEnumerable<Assembly> LoadAssemblyFromName(IEnumerable<AssemblyName> assemblyNames)
        {
            return assemblyNames.Select(x => LoadAssemblyFromName(x)).Where(x => x != null);
        }

        private static Assembly LoadAssemblyFromName(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Assembly filter logic from:
        /// https://raw.githubusercontent.com/Microsoft/dotnet-apiport/master/src/Microsoft.Fx.Portability/Analyzer/DotNetFrameworkFilter.cs
        /// </summary>
        private static class DotNetFrameworkFilter
        {
            /// <summary>
            /// These keys are a collection of public key tokens derived from all the reference assemblies in
            /// "%ProgramFiles%\Reference Assemblies\Microsoft" on a Windows 10 machine with VS 2015 installed
            /// </summary>
            private static readonly ICollection<string> s_microsoftKeys = new HashSet<string>(new[]
            {
                "b77a5c561934e089", // ECMA
                "b03f5f7f11d50a3a", // DEVDIV
                "7cec85d7bea7798e", // SLPLAT
                "31bf3856ad364e35", // Windows
                "24eec0d8c86cda1e", // Phone
                "0738eb9f132ed756", // Mono
                "ddd0da4d3e678217", // Component model
                "84e04ff9cfb79065", // Mono Android
                "842cf8be1de50553"  // Xamarin.iOS
            }, StringComparer.OrdinalIgnoreCase);

            private static readonly IEnumerable<string> s_frameworkAssemblyNamePrefixes = new[]
            {
                "System.",
                "Microsoft.",
                "Mono."
            };

            /// <summary>
            /// Gets a best guess as to whether this assembly is a .NET Framework assembly or not.
            /// </summary>
            public static bool IsFrameworkAssembly(Assembly assembly)
            {
                return assembly != null && IsFrameworkAssembly(assembly.GetName());
            }

            /// <summary>
            /// Gets a best guess as to whether this assembly is a .NET Framework assembly or not.
            /// </summary>
            public static bool IsFrameworkAssembly(AssemblyName assembly)
            {
                if (assembly == null)
                {
                    return false;
                }

                var publicKey = assembly.GetPublicKeyToken();

                if (publicKey == default(byte[]))
                {
                    return false;
                }

                var publicKeyToken = string.Concat(publicKey.Select(i => i.ToString("x2")));

                return s_microsoftKeys.Contains(publicKeyToken)
                    || s_frameworkAssemblyNamePrefixes.Any(p => assembly.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /* Being Re-written
    public sealed class SPIClassIterator<S> : IEnumerator<Type>
    {
      private const string META_INF_SERVICES = "META-INF/services/";

      private readonly Type Clazz;
      private readonly ClassLoader Loader;
      private readonly IEnumerator<URL> ProfilesEnum;
      private IEnumerator<string> LinesIterator;

      public static SPIClassIterator<S> Get<S>(Type clazz)
      {
        return new SPIClassIterator<S>(clazz, Thread.CurrentThread.ContextClassLoader);
      }

      public static SPIClassIterator<S> Get<S>(Type clazz, ClassLoader loader)
      {
        return new SPIClassIterator<S>(clazz, loader);
      }

      /// <summary>
      /// Utility method to check if some class loader is a (grand-)parent of or the same as another one.
      /// this means the child will be able to load all classes from the parent, too.
      /// </summary>
      public static bool IsParentClassLoader(ClassLoader parent, ClassLoader child)
      {
        while (child != null)
        {
          if (child == parent)
          {
            return true;
          }
          child = child.Parent;
        }
        return false;
      }

      private SPIClassIterator(Type clazz, ClassLoader loader)
      {
        this.Clazz = clazz;
        try
        {
          string fullName = META_INF_SERVICES + clazz.Name;
          this.ProfilesEnum = (loader == null) ? ClassLoader.getSystemResources(fullName) : loader.getResources(fullName);
        }
        catch (System.IO.IOException ioe)
        {
          throw new ServiceConfigurationError("Error loading SPI profiles for type " + clazz.Name + " from classpath", ioe);
        }
        this.Loader = (loader == null) ? ClassLoader.SystemClassLoader : loader;
        this.LinesIterator = Collections.emptySet<string>().GetEnumerator();
      }

      private bool LoadNextProfile()
      {
        List<string> lines = null;
        while (ProfilesEnum.MoveNext())
        {
          if (lines != null)
          {
            lines.Clear();
          }
          else
          {
            lines = new List<string>();
          }
          URL url = ProfilesEnum.Current;
          try
          {
            InputStream @in = url.openStream();
            System.IO.IOException priorE = null;
            try
            {
              BufferedReader reader = new BufferedReader(new InputStreamReader(@in, IOUtils.CHARSET_UTF_8));
              string line;
              while ((line = reader.readLine()) != null)
              {
                int pos = line.IndexOf('#');
                if (pos >= 0)
                {
                  line = line.Substring(0, pos);
                }
                line = line.Trim();
                if (line.Length > 0)
                {
                  lines.Add(line);
                }
              }
            }
            catch (System.IO.IOException ioe)
            {
              priorE = ioe;
            }
            finally
            {
              IOUtils.CloseWhileHandlingException(priorE, @in);
            }
          }
          catch (System.IO.IOException ioe)
          {
            throw new ServiceConfigurationError("Error loading SPI class list from URL: " + url, ioe);
          }
          if (lines.Count > 0)
          {
            this.LinesIterator = lines.GetEnumerator();
            return true;
          }
        }
        return false;
      }

      public override bool HasNext()
      {
      }

      public override Type Next()
      {
        // hasNext() implicitely loads the next profile, so it is essential to call this here!
        if (!HasNext())
        {
          throw new NoSuchElementException();
        }
        string c = LinesIterator.next();
        try
        {
          // don't initialize the class (pass false as 2nd parameter):
          return Type.GetType(c, false, Loader).asSubclass(Clazz);
        }
        catch (ClassNotFoundException cnfe)
        {
          throw new ServiceConfigurationError(string.format(Locale.ROOT, "A SPI class of type %s with classname %s does not exist, " + "please fix the file '%s%1$s' in your classpath.", Clazz.Name, c, META_INF_SERVICES));
        }
      }

      public override void Remove()
      {
        throw new System.NotSupportedException();
      }
    }*/
}