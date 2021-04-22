// LUCENENET specific - removed this class and replaced it with abstract factories
// DefaultCodecFactory, DefaultDocValuesFormatFactory, and DefaultPostingsFormatFactory
// and their common base class, NamedServiceFactory<TService>.
// The factories have a more efficient default scanning implementation and make
// it possible to use an external dependency injection container to supply the constructed
// codecs without any constraints on the constructor.

//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;

//namespace Lucene.Net.Util
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// Helper class for loading named SPIs from classpath (e.g. Codec, PostingsFormat).
//    /// @lucene.internal
//    /// </summary>
//    public sealed class NamedSPILoader<S> : IEnumerable<S> where S : NamedSPILoader.INamedSPI
//    {
//        private volatile IDictionary<string, S> services = Collections.EmptyMap<string, S>();
//        private readonly Type clazz;

//        public NamedSPILoader(Type clazz)
//        {
//            this.clazz = clazz;
//            // if clazz' classloader is not a parent of the given one, we scan clazz's classloader, too:

//            Reload();
//        }

//        /// <summary>
//        /// Reloads the internal SPI list from the given <seealso cref="ClassLoader"/>.
//        /// Changes to the service list are visible after the method ends, all
//        /// iterators (<seealso cref="#iterator()"/>,...) stay consistent.
//        ///
//        /// <p><b>NOTE:</b> Only new service providers are added, existing ones are
//        /// never removed or replaced.
//        ///
//        /// <p><em>this method is expensive and should only be called for discovery
//        /// of new service providers on the given classpath/classloader!</em>
//        /// </summary>
//        public void Reload()
//        {
//            lock (this)
//            {
//                IDictionary<string, S> services = new Dictionary<string, S>(this.services);
//                SPIClassIterator<S> loader = SPIClassIterator<S>.Get();

//                // Ensure there is a default constructor (the SPIClassIterator contains types that don't)
//                foreach (Type c in loader.Where(t => t.GetConstructor(Type.EmptyTypes) != null))
//                {
//                    try
//                    {
//                        S service = (S)Activator.CreateInstance(c);
//                        string name = service.Name;
//                        // only add the first one for each name, later services will be ignored
//                        // this allows to place services before others in classpath to make
//                        // them used instead of others
//                        if (!services.ContainsKey(name))
//                        {
//                            CheckServiceName(name);
//                            services[name] = service;
//                        }
//                    }
//                    catch (Exception e)
//                    {
//                        throw ServiceConfigurationError.Create("Cannot instantiate SPI class: " + c.Name, e);
//                    }
//                }
//                this.services = Collections.UnmodifiableMap(services);
//            }
//        }

//        /// <summary>
//        /// Validates that a service name meets the requirements of <seealso cref="INamedSPI"/>
//        /// </summary>
//        public static void CheckServiceName(string name)
//        {
//            // based on harmony charset.java
//            if (name.Length >= 128)
//            {
//                throw new ArgumentException("Illegal service name: '" + name + "' is too long (must be < 128 chars).");
//            }
//            for (int i = 0, len = name.Length; i < len; i++)
//            {
//                char c = name[i];
//                if (!IsLetterOrDigit(c))
//                {
//                    throw new ArgumentException("Illegal service name: '" + name + "' must be simple ascii alphanumeric.");
//                }
//            }
//        }

//        /// <summary>
//        /// Checks whether a character is a letter or digit (ascii) which are defined in the spec.
//        /// </summary>
//        private static bool IsLetterOrDigit(char c)
//        {
//            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9');
//        }

//        public S Lookup(string name)
//        {
//            S service;
//            if (services.TryGetValue(name, out service))
//            {
//                return service;
//            }
//            throw new ArgumentException("A SPI class of type " + clazz.Name + " with name '" + name + "' does not exist. " + "You need to add the corresponding JAR file supporting this SPI to your classpath." + "The current classpath supports the following names: " + AvailableServices());
//        }

//        public ISet<string> AvailableServices()
//        {
//            return new HashSet<string>(services.Keys);
//        }

//        public IEnumerator<S> GetEnumerator()
//        {
//            return services.Values.GetEnumerator();
//        }

//        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//        {
//            return GetEnumerator();
//        }
//    }

//    public class NamedSPILoader
//    {
//        private NamedSPILoader() { } // prevent direct creation

//        /// <summary>
//        /// Interface to support <seealso cref="NamedSPILoader#lookup(String)"/> by name.
//        /// <p>
//        /// Names must be all ascii alphanumeric, and less than 128 characters in length.
//        /// </summary>
//        public interface INamedSPI
//        {
//            string Name { get; }
//        }
//    }
//}