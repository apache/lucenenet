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
    /// Implements the default functionality of <see cref="IDocValuesFormatFactory"/>.
    /// <para/>
    /// To replace the <see cref="DefaultDocValuesFormatFactory"/> instance, call
    /// <see cref="DocValuesFormat.SetDocValuesFormatFactory(IDocValuesFormatFactory)"/> at application start up.
    /// <see cref="DefaultDocValuesFormatFactory"/> can be subclassed or passed additional parameters to register
    /// additional codecs, inject dependencies, or change caching behavior, as shown in the following examples.
    /// Alternatively, <see cref="IDocValuesFormatFactory"/> can be implemented to provide complete control over
    /// doc values format creation and lifetimes.
    /// <para/>
    /// <h4>Register Additional DocValuesFormats</h4>
    /// <para/>
    /// Additional codecs can be added by initializing the instance of <see cref="DefaultDocValuesFormatFactory"/> and
    /// passing an array of <see cref="DocValuesFormat"/>-derived types.
    /// <code>
    /// // Register the factory at application start up.
    /// DocValuesFormat.SetDocValuesFormatFactory(new DefaultDocValuesFormatFactory {
    ///     CustomDocValuesFormatTypes = new Type[] { typeof(MyDocValuesFormat), typeof(AnotherDocValuesFormat) }
    /// });
    /// </code>
    /// <para/>
    /// <h4>Only Use Explicitly Defined DocValuesFormats</h4>
    /// <para/>
    /// <see cref="PutDocValuesFormatType(Type)"/> can be used to explicitly add codec types. In this example,
    /// the call to <c>base.Initialize()</c> is excluded to skip the built-in codec registration.
    /// Since <c>AnotherDocValuesFormat</c> doesn't have a default constructor, the <see cref="NewDocValuesFormat(Type)"/>
    /// method is overridden to supply the required parameters.
    /// <code>
    /// public class ExplicitDocValuesFormatFactory : DefaultDocValuesFormatFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load specific codecs in a specific order.
    ///         PutDocValuesFormatType(typeof(MyDocValuesFormat));
    ///         PutDocValuesFormatType(typeof(AnotherDocValuesFormat));
    ///     }
    ///     
    ///     protected override DocValuesFormat NewDocValuesFormat(Type type)
    ///     {
    ///         // Special case: AnotherDocValuesFormat has a required dependency
    ///         if (typeof(AnotherDocValuesFormat).Equals(type))
    ///             return new AnotherDocValuesFormat(new SomeDependency());
    ///         
    ///         return base.NewDocValuesFormat(type);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// DocValuesFormat.SetDocValuesFormatFactory(new ExplicitDocValuesFormatFactory());
    /// </code>
    /// See the <see cref="Lucene.Net.Codecs"/> namespace documentation for more examples of how to
    /// inject dependencies into <see cref="DocValuesFormat"/> subclasses.
    /// <para/>
    /// <h4>Use Reflection to Scan an Assembly for DocValuesFormats</h4>
    /// <para/>
    /// <see cref="ScanForDocValuesFormats(Assembly)"/> or <see cref="ScanForDocValuesFormats(IEnumerable{Assembly})"/> can be used
    /// to scan assemblies using .NET Reflection for codec types and add all subclasses that are found automatically.
    /// <code>
    /// public class ScanningDocValuesFormatFactory : DefaultDocValuesFormatFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load all default codecs
    ///         base.Initialize();
    ///         
    ///         // Load all of the codecs inside of the same assembly that MyDocValuesFormat is defined in
    ///         ScanForDocValuesFormats(typeof(MyDocValuesFormat).Assembly);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// DocValuesFormat.SetDocValuesFormatFactory(new ScanningDocValuesFormatFactory());
    /// </code>
    /// Doc values formats in the target assembly can be excluded from the scan by decorating them with
    /// the <see cref="ExcludeDocValuesFormatFromScanAttribute"/>.
    /// </summary>
    /// <seealso cref="IDocValuesFormatFactory"/>
    /// <seealso cref="IServiceListable"/>
    /// <seealso cref="ExcludeDocValuesFormatFromScanAttribute"/>
    // LUCENENET specific
    public class DefaultDocValuesFormatFactory : NamedServiceFactory<DocValuesFormat>, IDocValuesFormatFactory, IServiceListable
    {
        private static readonly Type[] localDocValuesFormatTypes = new Type[] {
            typeof(Lucene45.Lucene45DocValuesFormat),
#pragma warning disable 612, 618
            typeof(Lucene42.Lucene42DocValuesFormat),
            typeof(Lucene40.Lucene40DocValuesFormat),
#pragma warning restore 612, 618
        };

        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> docValuesFormatNameToTypeMap;
        private readonly IDictionary<Type, DocValuesFormat> docValuesFormatInstanceCache;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultDocValuesFormatFactory"/>.
        /// </summary>
        public DefaultDocValuesFormatFactory()
        {
            docValuesFormatNameToTypeMap = new Dictionary<string, Type>();
            docValuesFormatInstanceCache = new Dictionary<Type, DocValuesFormat>();
        }

        /// <summary>
        /// An array of custom <see cref="DocValuesFormat"/>-derived types to be registered. This property
        /// can be initialized during construction of <see cref="DefaultDocValuesFormatFactory"/>
        /// to make your custom codecs known to Lucene.
        /// <para/>
        /// These types will be registered after the default Lucene types, so if a custom type has the same
        /// name as a Lucene <see cref="DocValuesFormat"/> (via <see cref="DocValuesFormatNameAttribute"/>) 
        /// the custom type will replace the Lucene type with the same name.
        /// </summary>
        public IEnumerable<Type> CustomDocValuesFormatTypes { get; set; }

        /// <summary>
        /// Initializes the doc values type cache with the known <see cref="DocValuesFormat"/> types.
        /// Override this method (and optionally call <c>base.Initialize()</c>) to add your
        /// own <see cref="DocValuesFormat"/> types by calling <see cref="PutDocValuesFormatType(Type)"/> 
        /// or <see cref="ScanForDocValuesFormats(Assembly)"/>.
        /// <para/>
        /// If two types have the same name by using the <see cref="DocValuesFormatNameAttribute"/>, the
        /// last one registered wins.
        /// </summary>
        protected override void Initialize()
        {
            foreach (var docValuesFormatType in localDocValuesFormatTypes)
                PutDocValuesFormatTypeImpl(docValuesFormatType);
            ScanForDocValuesFormats(this.CodecsAssembly);
            if (CustomDocValuesFormatTypes != null)
            {
                foreach (var docValuesFormatType in CustomDocValuesFormatTypes)
                    PutDocValuesFormatType(docValuesFormatType);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assemblies"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="docValuesFormatNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="DocValuesFormat"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assemblies">A list of assemblies to scan. The assemblies will be scanned from first to last, 
        /// and the last match for each <see cref="DocValuesFormat"/> name wins.</param>
        protected virtual void ScanForDocValuesFormats(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanForDocValuesFormats(assembly);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assembly"/> for subclasses of <see cref="DocValuesFormat"/>
        /// and adds their names to the <see cref="docValuesFormatNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="DocValuesFormat"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        protected virtual void ScanForDocValuesFormats(Assembly assembly)
        {
            if (assembly is null) return;

            foreach (var c in assembly.GetTypes())
            {
                if (IsServiceType(c))
                {
                    PutDocValuesFormatTypeImpl(c);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="DocValuesFormat"/> type to the <see cref="docValuesFormatNameToTypeMap"/>, using 
        /// the name provided in the <see cref="DocValuesFormatNameAttribute"/>, if present, or the name
        /// of the codec class minus the "DocValuesFormat" suffix as the name by default.
        /// <para/>
        /// Note that if a <see cref="DocValuesFormat"/> with the same name already exists in the map,
        /// calling this method will update it to the new type.
        /// </summary>
        /// <param name="docValuesFormat">A type that subclasses <see cref="DocValuesFormat"/>.</param>
        protected virtual void PutDocValuesFormatType(Type docValuesFormat)
        {
            if (docValuesFormat is null)
                throw new ArgumentNullException(nameof(docValuesFormat));
            if (!typeof(DocValuesFormat).IsAssignableFrom(docValuesFormat))
                throw new ArgumentException($"The supplied type {docValuesFormat.AssemblyQualifiedName} does not subclass {nameof(DocValuesFormat)}.");

            PutDocValuesFormatTypeImpl(docValuesFormat);
        }

        private void PutDocValuesFormatTypeImpl(Type docValuesFormat)
        {
            string name = GetServiceName(docValuesFormat);
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                docValuesFormatNameToTypeMap[name] = docValuesFormat;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
            }
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="DocValuesFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> instance.</returns>
        public virtual DocValuesFormat GetDocValuesFormat(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                Type codecType = GetDocValuesFormatType(name);
                return GetDocValuesFormat(codecType);
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
            }
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="DocValuesFormat"/> to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> instance.</returns>
        protected virtual DocValuesFormat GetDocValuesFormat(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (!docValuesFormatInstanceCache.TryGetValue(type, out DocValuesFormat instance))
            {
                UninterruptableMonitor.Enter(m_initializationLock);
                try
                {
                    if (!docValuesFormatInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = NewDocValuesFormat(type);
                        docValuesFormatInstanceCache[type] = instance;
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
        /// Instantiates a <see cref="DocValuesFormat"/> based on the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="DocValuesFormat"/> to instantiate.</param>
        /// <returns>The new instance.</returns>
        protected virtual DocValuesFormat NewDocValuesFormat(Type type)
        {
            return (DocValuesFormat)Activator.CreateInstance(type, IsFullyTrusted);
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="DocValuesFormat"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> <see cref="Type"/>.</returns>
        protected virtual Type GetDocValuesFormatType(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            EnsureInitialized();
            if (!docValuesFormatNameToTypeMap.TryGetValue(name, out Type codecType) || codecType is null)
            {
                throw new ArgumentException($"DocValuesFormat '{name}' cannot be loaded. If the format is not " +
                    $"in a Lucene.Net assembly, you must subclass {typeof(DefaultDocValuesFormatFactory).FullName}, " +
                    "override the Initialize() method, and call PutDocValuesFormatType() or ScanForDocValuesFormats() to add " +
                    $"the type manually. Call {typeof(DocValuesFormat).FullName}.SetDocValuesFormatFactory() at application " +
                    "startup to initialize your custom format.");
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="DocValuesFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="T:ICollection{string}"/> of <see cref="DocValuesFormat"/> names.</returns>
        public virtual ICollection<string> AvailableServices
        {
            get
            {
                EnsureInitialized();
                return docValuesFormatNameToTypeMap.Keys;
            }
        }
    }
}
