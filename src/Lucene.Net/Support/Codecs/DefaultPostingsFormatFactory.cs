using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lucene.Net.Codecs
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
    /// LUCENENET specific class that implements the default functionality for the 
    /// <see cref="IPostingsFormatFactory"/>.
    /// <para/>
    /// The most common use cases are:
    /// <list type="bullet">
    ///     <item><description>Initialize <see cref="DefaultPostingsFormatFactory"/> with a set of <see cref="CustomPostingsFormatTypes"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultPostingsFormatFactory"/> and override
    ///         <see cref="DefaultPostingsFormatFactory.GetPostingsFormat(Type)"/> so an external dependency injection
    ///         container can be used to supply the instances (lifetime should be singleton). Note that you could 
    ///         alternately use the "named type" feature that many DI containers have to supply the type based on name by 
    ///         overriding <see cref="GetPostingsFormat(string)"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultPostingsFormatFactory"/> and override
    ///         <see cref="DefaultPostingsFormatFactory.GetPostingsFormatType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultPostingsFormatFactory.postingsFormatNameToTypeMap"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultPostingsFormatFactory"/> to add new or override the default <see cref="PostingsFormat"/> 
    ///         types by overriding <see cref="Initialize()"/> and calling <see cref="PutPostingsFormatType(Type)"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultPostingsFormatFactory"/> to scan additional assemblies for <see cref="PostingsFormat"/>
    ///         subclasses in by overriding <see cref="Initialize()"/> and calling <see cref="ScanForPostingsFormats(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</description></item>
    /// </list>
    /// <para/>
    /// To set the <see cref="IPostingsFormatFactory"/>, call <see cref="PostingsFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/>.
    /// </summary>
    public class DefaultPostingsFormatFactory : NamedServiceFactory<PostingsFormat>, IPostingsFormatFactory, IServiceListable
    {
        private static readonly Type[] localPostingsFormatTypes = new Type[]
        {
            typeof(Lucene41.Lucene41PostingsFormat),
#pragma warning disable 612, 618
            typeof(Lucene40.Lucene40PostingsFormat),
#pragma warning restore 612, 618
        };

        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> postingsFormatNameToTypeMap;
        private readonly IDictionary<Type, PostingsFormat> postingsFormatInstanceCache;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultPostingsFormatFactory"/>.
        /// </summary>
        public DefaultPostingsFormatFactory()
        {
            postingsFormatNameToTypeMap = new Dictionary<string, Type>();
            postingsFormatInstanceCache = new Dictionary<Type, PostingsFormat>();
        }

        /// <summary>
        /// An array of custom <see cref="PostingsFormat"/>-derived types to be registered. This property
        /// can be initialized during construction of <see cref="DefaultPostingsFormatFactory"/>
        /// to make your custom codecs known to Lucene.
        /// <para/>
        /// These types will be registered after the default Lucene types, so if a custom type has the same
        /// name as a Lucene <see cref="PostingsFormat"/> (via <see cref="PostingsFormatNameAttribute"/>) 
        /// the custom type will replace the Lucene type with the same name.
        /// </summary>
        public IEnumerable<Type> CustomPostingsFormatTypes { get; set; }

        /// <summary>
        /// Initializes the codec type cache with the known <see cref="PostingsFormat"/> types.
        /// Override this method (and optionally call <c>base.Initialize()</c>) to add your
        /// own <see cref="PostingsFormat"/> types by calling <see cref="PutPostingsFormatType(Type)"/> 
        /// or <see cref="ScanForPostingsFormats(Assembly)"/>.
        /// <para/>
        /// If two types have the same name by using the <see cref="PostingsFormatNameAttribute"/>, the
        /// last one registered wins.
        /// </summary>
        protected override void Initialize()
        {
            foreach (var postingsFormatType in localPostingsFormatTypes)
                PutPostingsFormatTypeImpl(postingsFormatType);
            ScanForPostingsFormats(this.CodecsAssembly);
            if (CustomPostingsFormatTypes != null)
            {
                foreach (var postingsFormatType in CustomPostingsFormatTypes)
                    PutPostingsFormatType(postingsFormatType);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assemblies"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="postingsFormatNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="PostingsFormat"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assemblies">A list of assemblies to scan. The assemblies will be scanned from first to last, 
        /// and the last match for each <see cref="PostingsFormat"/> name wins.</param>
        protected virtual void ScanForPostingsFormats(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanForPostingsFormats(assembly);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assembly"/> for subclasses of <see cref="PostingsFormat"/>
        /// and adds their names to the <see cref="postingsFormatNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="PostingsFormat"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        protected virtual void ScanForPostingsFormats(Assembly assembly)
        {
            if (assembly == null) return;

            foreach (var c in assembly.GetTypes())
            {
                if (IsServiceType(c))
                {
                    PutPostingsFormatTypeImpl(c);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="PostingsFormat"/> type to the <see cref="postingsFormatNameToTypeMap"/>, using 
        /// the name provided in the <see cref="PostingsFormatNameAttribute"/>, if present, or the name
        /// of the codec class minus the "Codec" suffix as the name by default.
        /// <para/>
        /// Note that if a <see cref="PostingsFormat"/> with the same name already exists in the map,
        /// calling this method will update it to the new type.
        /// </summary>
        /// <param name="postingsFormat">A type that subclasses <see cref="PostingsFormat"/>.</param>
        protected virtual void PutPostingsFormatType(Type postingsFormat)
        {
            if (postingsFormat == null)
                throw new ArgumentNullException(nameof(postingsFormat));
            if (!typeof(PostingsFormat).IsAssignableFrom(postingsFormat))
                throw new ArgumentException($"The supplied type {postingsFormat.AssemblyQualifiedName} does not subclass {nameof(PostingsFormat)}.");

            PutPostingsFormatTypeImpl(postingsFormat);
        }

        private void PutPostingsFormatTypeImpl(Type postingsFormat)
        {
            string name = GetServiceName(postingsFormat);
            lock (m_initializationLock)
            {
                postingsFormatNameToTypeMap[name] = postingsFormat;
            }
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        public virtual PostingsFormat GetPostingsFormat(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            lock (m_initializationLock)
            {
                Type codecType = GetPostingsFormatType(name);
                return GetPostingsFormat(codecType);
            }
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="PostingsFormat"/> to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        protected virtual PostingsFormat GetPostingsFormat(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!postingsFormatInstanceCache.TryGetValue(type, out PostingsFormat instance))
            {
                lock (m_initializationLock)
                {
                    if (!postingsFormatInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = NewPostingsFormat(type);
                        postingsFormatInstanceCache[type] = instance;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Instantiates a <see cref="PostingsFormat"/> based on the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="PostingsFormat"/> to instantiate.</param>
        /// <returns>The new instance.</returns>
        protected virtual PostingsFormat NewPostingsFormat(Type type)
        {
            return (PostingsFormat)Activator.CreateInstance(type, IsFullyTrusted);
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> <see cref="Type"/>.</returns>
        protected virtual Type GetPostingsFormatType(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            EnsureInitialized();
            if (!postingsFormatNameToTypeMap.TryGetValue(name, out Type codecType) && codecType == null)
            {
                throw new ArgumentException($"PostingsFormat '{name}' cannot be loaded. If the format is not " +
                    $"in a Lucene.Net assembly, you must subclass {typeof(DefaultPostingsFormatFactory).FullName}, " +
                    "override the Initialize() method, and call PutPostingsFormatType() or ScanForPostingsFormats() to add " +
                    $"the type manually. Call {typeof(PostingsFormat).FullName}.SetPostingsFormatFactory() at application " +
                    "startup to initialize your custom format.");
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="PostingsFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="T:ICollection{string}"/> of <see cref="PostingsFormat"/> names.</returns>
        public virtual ICollection<string> AvailableServices
        {
            get
            {
                EnsureInitialized();
                return postingsFormatNameToTypeMap.Keys;
            }
        }
    }
}
