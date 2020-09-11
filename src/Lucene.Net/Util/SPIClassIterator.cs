using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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
    /// Helper class for loading SPI classes from classpath (META-INF files).
    /// This is a light impl of <c>java.util.ServiceLoader</c> but is guaranteed to
    /// be bug-free regarding classpath order and does not instantiate or initialize
    /// the classes found.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class SPIClassIterator<S> : IEnumerable<Type>
    {
        private static readonly JCG.HashSet<Type> types = LoadTypes();

        private static JCG.HashSet<Type> LoadTypes() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var types = new JCG.HashSet<Type>();

            var assembliesToExamine = Support.AssemblyUtils.GetReferencedAssemblies();

            // LUCENENET NOTE: The following hack is not required because we are using abstract factories 
            // and pure DI to ensure the order of the codecs are always correct during testing.

            //// LUCENENET HACK:
            //// Tests such as TestImpersonation.cs expect that the assemblies
            //// are probed in a certain order. NamedSPILoader, lines 68 - 75 adds
            //// the first item it sees with that name. So if you have multiple
            //// codecs, it may not add the right one, depending on the order of
            //// the assemblies that were examined.
            //// This results in many test failures if Types from Lucene.Net.Codecs
            //// are examined and added to NamedSPILoader first before
            //// Lucene.Net.TestFramework.
            //var testFrameworkAssembly = assembliesToExamine.FirstOrDefault(x => string.Equals(x.GetName().Name, "Lucene.Net.TestFramework", StringComparison.Ordinal));
            //if (testFrameworkAssembly != null)
            //{
            //    //assembliesToExamine.Remove(testFrameworkAssembly);
            //    //assembliesToExamine.Insert(0, testFrameworkAssembly);
            //    assembliesToExamine = new Assembly[] { testFrameworkAssembly }.Concat(assembliesToExamine.Where(a => !testFrameworkAssembly.Equals(a)));
            //}

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
                                        return false; // LUCENENET NOTE: Now that we have factored Codecs into Abstract Factories, we don't need default constructors here
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
#pragma warning disable CA1031 // Do not catch general exception types
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
#pragma warning restore CA1031 // Do not catch general exception types
            }
            return types;
        }

        internal static bool IsInvokableSubclassOf<T>(Type type)
        {
            return typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
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
        catch (IOException ioe)
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
            IOException priorE = null;
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
            catch (IOException ioe)
            {
              priorE = ioe;
            }
            finally
            {
              IOUtils.CloseWhileHandlingException(priorE, @in);
            }
          }
          catch (IOException ioe)
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
        throw new NotSupportedException();
      }
    }*/
}