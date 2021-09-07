using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            return types;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsInvokableSubclassOf<T>(Type type)
        {
            return typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SPIClassIterator<S> Get()
        {
            return new SPIClassIterator<S>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<Type> GetEnumerator()
        {
            return types.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}