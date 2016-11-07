using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

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

        private volatile IDictionary<string, Type> services = CollectionsHelper.EmptyMap<string, Type>();
        private readonly Type clazz = typeof(S);
        private readonly string[] suffixes;

        public AnalysisSPILoader()
            : this(new string[] { typeof(S).Name })
        {
        }

        public AnalysisSPILoader(string[] suffixes)
        {
            this.suffixes = suffixes;

            Reload();
        }

        /// <summary>
        /// Reloads the internal SPI list from the given <seealso cref="ClassLoader"/>.
        /// Changes to the service list are visible after the method ends, all
        /// iterators (<seealso cref="#iterator()"/>,...) stay consistent.
        ///
        /// <p><b>NOTE:</b> Only new service providers are added, existing ones are
        /// never removed or replaced.
        ///
        /// <p><em>this method is expensive and should only be called for discovery
        /// of new service providers on the given classpath/classloader!</em>
        /// </summary>
        public void Reload()
        {
            lock (this)
            {
                IDictionary<string, Type> services = new Dictionary<string, Type>(this.services);
                SPIClassIterator<S> loader = SPIClassIterator<S>.Get();

                foreach (var service in loader)
                {
                    string clazzName = service.Name;
                    string name = null;
                    foreach (string suffix in suffixes)
                    {
                        if (clazzName.EndsWith(suffix))
                        {
                            name = clazzName.Substring(0, clazzName.Length - suffix.Length).ToLowerInvariant();
                            break;
                        }
                    }

                    if (name == null)
                    {
                        throw new InvalidOperationException("The class name " + service.Name +
                          " has wrong suffix, allowed are: " + Arrays.ToString(suffixes));
                    }
                    // only add the first one for each name, later services will be ignored
                    // this allows to place services before others in classpath to make
                    // them used instead of others
                    //
                    // LUCENETODO: Should we disallow duplicate names here?
                    // Allowing it may get confusing on collisions, as different packages
                    // could contain same factory class, which is a naming bug!
                    // When changing this be careful to allow reload()!
                    if (!services.ContainsKey(name))
                    {
                        services.Add(name, service);
                    }
                }
                this.services = CollectionsHelper.UnmodifiableMap(services);
            }
        }

        public S NewInstance(string name, IDictionary<string, string> args)
        {
            Type service = LookupClass(name);
            try
            {
                return (S)Activator.CreateInstance(service, new object[] { args });
            }
            catch (Exception e)
            {
                throw new System.ArgumentException("SPI class of type " + clazz.Name + " with name '" + name + "' cannot be instantiated. " +
                    "This is likely due to a missing reference of the .NET Assembly containing the class '" + service.Name + "' in your project or AppDomain: ", e);
            }
        }

        public Type LookupClass(string name)
        {
            Type service;
            if (this.services.TryGetValue(name.ToLowerInvariant(), out service))
            {
                return service;
            }
            else
            {
                throw new System.ArgumentException("A SPI class of type " + clazz.Name + " with name '" + name + "' does not exist. " +
                    "You need to add the corresponding reference supporting this SPI to your project or AppDomain. " +
                    "The current classpath supports the following names: " + Arrays.ToString(AvailableServices));
            }
        }

        public IEnumerable<string> AvailableServices
        {
            get { return services.Keys; }
        }
    }
}