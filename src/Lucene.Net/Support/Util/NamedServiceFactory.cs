using System;
using System.Reflection;
using System.Threading;

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
    /// LUCENENET specific abstract class containing common fuctionality for named service factories.
    /// </summary>
    /// <typeparam name="TService">The type of service this factory applies to.</typeparam>
    public abstract class NamedServiceFactory<TService>
    {
        private static Assembly codecsAssembly = null;
        private bool initialized = false;
        protected object m_initializationLock = new object();
        private object initializationTarget; // Dummy variable required by LazyInitializer.EnsureInitialized

        /// <summary>
        /// Ensures the <see cref="Initialize"/> method has been called since the
        /// last application start. This method is thread-safe.
        /// </summary>
        protected void EnsureInitialized()
        {
            LazyInitializer.EnsureInitialized(ref this.initializationTarget, ref this.initialized, ref this.m_initializationLock, () =>
            {
                Initialize();
                return null;
            });
        }

        /// <summary>
        /// Initializes the dependencies of this factory (such as using Reflection to populate the type cache).
        /// </summary>
        protected abstract void Initialize();


        /// <summary>
        /// The Lucene.Net.Codecs assembly or <c>null</c> if the assembly is not referenced
        /// in the host project.
        /// </summary>
        protected Assembly CodecsAssembly
        {
            get
            {
                if (codecsAssembly is null)
                {
                    // Attempt to load the SimpleTextCodec type. If it loads it will not be null, 
                    // which means the assembly is referenced so we can load all of the named services from that assembly.
                    Type simpleTextType = Type.GetType("Lucene.Net.Codecs.SimpleText.SimpleTextCodec, Lucene.Net.Codecs");
                    if (simpleTextType != null)
                    {
                        codecsAssembly = simpleTextType.Assembly;
                    }
                }

                return codecsAssembly;
            }
        }

        /// <summary>
        /// Determines whether the given type is corresponding service for this class,
        /// based on its generic closing type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of service to analyze.</param>
        /// <returns><c>true</c> if the service subclasses <typeparamref name="TService"/>, is public, and is not abstract; otherwise <c>false</c>.</returns>
        protected virtual bool IsServiceType(Type type)
        {
            return
                type != null &&
                type.IsPublic &&
                !type.IsAbstract &&
                typeof(TService).IsAssignableFrom(type) &&
                type.GetCustomAttribute<ExcludeServiceAttribute>(inherit: true) is null;
        }

        /// <summary>
        /// Get the service name for the class (either by convention or by attribute).
        /// </summary>
        /// <param name="type">A service to get the name for.</param>
        /// <returns>The canonical name of the service or the name provided in the corresponding name attribute, if supplied.</returns>
        public static string GetServiceName(Type type)
        {
            // Check for ServiceName attribute
            var nameAttributes = type.GetCustomAttributes(typeof(ServiceNameAttribute), inherit: true);
            if (nameAttributes.Length > 0)
            {
                if (nameAttributes[0] is ServiceNameAttribute nameAttribute)
                {
                    string name = nameAttribute.Name;
                    CheckServiceName(name);
                    return name;
                }
            }

            return GetCanonicalName(type);
        }

        /// <summary>
        /// Gets the type name without the suffix of the abstract base class it implements.
        /// If the class is generic, it will add the word "Generic" to the suffix in place of "`"
        /// to ensure the name is ASCII-only.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to get the name for.</param>
        /// <returns>The canonical name of the service.</returns>
        protected static string GetCanonicalName(Type type)
        {
            string name = type.Name;
            string genericSuffix = string.Empty;
            int genericIndex = name.IndexOf('`');
            if (genericIndex > -1)
            {
                genericSuffix = "Generic" + name.Substring(genericIndex + 1);
                name = name.Substring(0, genericIndex);
            }
            string serviceName = typeof(TService).Name;
            if (name.EndsWith(serviceName, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - serviceName.Length);
            }
            return name + genericSuffix;
        }

        /// <summary>
        /// Validates that a service name meets the requirements of Lucene
        /// </summary>
        private static void CheckServiceName(string name)
        {
            // based on harmony charset.java
            if (name.Length >= 128)
            {
                throw new ArgumentException("Illegal service name: '" + name + "' is too long (must be < 128 chars).");
            }
            for (int i = 0, len = name.Length; i < len; i++)
            {
                char c = name[i];
                if (!IsLetterOrDigit(c))
                {
                    throw new ArgumentException("Illegal service name: '" + name + "' must be simple ascii alphanumeric.");
                }
            }
        }

        /// <summary>
        /// Checks whether a character is a letter or digit (ascii) which are defined in the spec.
        /// </summary>
        private static bool IsLetterOrDigit(char c)
        {
            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9');
        }

        /// <summary>
        /// Gets a value that indicates whether the current application domain executes with full trust.
        /// </summary>
        protected bool IsFullyTrusted => AppDomain.CurrentDomain.IsFullyTrusted; // Partial trust support
    }
}
