using Lucene.Net.Support.Threading;
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
    /// Implements the default functionality of <see cref="IPostingsFormatFactory"/>.
    /// <para/>
    /// To replace the <see cref="DefaultPostingsFormatFactory"/> instance, call
    /// <see cref="PostingsFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/> at application start up.
    /// <see cref="DefaultPostingsFormatFactory"/> can be subclassed or passed additional parameters to register
    /// additional codecs, inject dependencies, or change caching behavior, as shown in the following examples.
    /// Alternatively, <see cref="IPostingsFormatFactory"/> can be implemented to provide complete control over
    /// postings format creation and lifetimes.
    /// <para/>
    /// <h4>Register Additional PostingsFormats</h4>
    /// <para/>
    /// Additional codecs can be added by initializing the instance of <see cref="DefaultPostingsFormatFactory"/> and
    /// passing an array of <see cref="PostingsFormat"/>-derived types.
    /// <code>
    /// // Register the factory at application start up.
    /// PostingsFormat.SetPostingsFormatFactory(new DefaultPostingsFormatFactory {
    ///     CustomPostingsFormatTypes = new Type[] { typeof(MyPostingsFormat), typeof(AnotherPostingsFormat) }
    /// });
    /// </code>
    /// <para/>
    /// <h4>Only Use Explicitly Defined PostingsFormats</h4>
    /// <para/>
    /// <see cref="PutPostingsFormatType(Type)"/> can be used to explicitly add codec types. In this example,
    /// the call to <c>base.Initialize()</c> is excluded to skip the built-in codec registration.
    /// Since <c>AnotherPostingsFormat</c> doesn't have a default constructor, the <see cref="NewPostingsFormat(Type)"/>
    /// method is overridden to supply the required parameters.
    /// <code>
    /// public class ExplicitPostingsFormatFactory : DefaultPostingsFormatFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load specific codecs in a specific order.
    ///         PutPostingsFormatType(typeof(MyPostingsFormat));
    ///         PutPostingsFormatType(typeof(AnotherPostingsFormat));
    ///     }
    ///     
    ///     protected override PostingsFormat NewPostingsFormat(Type type)
    ///     {
    ///         // Special case: AnotherPostingsFormat has a required dependency
    ///         if (typeof(AnotherPostingsFormat).Equals(type))
    ///             return new AnotherPostingsFormat(new SomeDependency());
    ///         
    ///         return base.NewPostingsFormat(type);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// PostingsFormat.SetPostingsFormatFactory(new ExplicitPostingsFormatFactory());
    /// </code>
    /// See the <see cref="Lucene.Net.Codecs"/> namespace documentation for more examples of how to
    /// inject dependencies into <see cref="PostingsFormat"/> subclasses.
    /// <para/>
    /// <h4>Use Reflection to Scan an Assembly for PostingsFormats</h4>
    /// <para/>
    /// <see cref="ScanForPostingsFormats(Assembly)"/> or <see cref="ScanForPostingsFormats(IEnumerable{Assembly})"/> can be used
    /// to scan assemblies using .NET Reflection for codec types and add all subclasses that are found automatically.
    /// <code>
    /// public class ScanningPostingsFormatFactory : DefaultPostingsFormatFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load all default codecs
    ///         base.Initialize();
    ///         
    ///         // Load all of the codecs inside of the same assembly that MyPostingsFormat is defined in
    ///         ScanForPostingsFormats(typeof(MyPostingsFormat).Assembly);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// PostingsFormat.SetPostingsFormatFactory(new ScanningPostingsFormatFactory());
    /// </code>
    /// Postings formats in the target assembly can be excluded from the scan by decorating them with
    /// the <see cref="ExcludePostingsFormatFromScanAttribute"/>.
    /// </summary>
    /// <seealso cref="IPostingsFormatFactory"/>
    /// <seealso cref="IServiceListable"/>
    /// <seealso cref="ExcludePostingsFormatFromScanAttribute"/>
    // LUCENENET specific
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
            if (assembly is null) return;

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
            if (postingsFormat is null)
                throw new ArgumentNullException(nameof(postingsFormat));
            if (!typeof(PostingsFormat).IsAssignableFrom(postingsFormat))
                throw new ArgumentException($"The supplied type {postingsFormat.AssemblyQualifiedName} does not subclass {nameof(PostingsFormat)}.");

            PutPostingsFormatTypeImpl(postingsFormat);
        }

        private void PutPostingsFormatTypeImpl(Type postingsFormat)
        {
            string name = GetServiceName(postingsFormat);
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                postingsFormatNameToTypeMap[name] = postingsFormat;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
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
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                Type codecType = GetPostingsFormatType(name);
                return GetPostingsFormat(codecType);
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
            }
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="PostingsFormat"/> to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        protected virtual PostingsFormat GetPostingsFormat(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (!postingsFormatInstanceCache.TryGetValue(type, out PostingsFormat instance))
            {
                UninterruptableMonitor.Enter(m_initializationLock);
                try
                {
                    if (!postingsFormatInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = NewPostingsFormat(type);
                        postingsFormatInstanceCache[type] = instance;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_initializationLock);
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
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            EnsureInitialized();
            if (!postingsFormatNameToTypeMap.TryGetValue(name, out Type codecType) || codecType is null)
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
