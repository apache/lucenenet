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
    /// Implements the default functionality of <see cref="ICodecFactory"/>.
    /// <para/>
    /// To replace the <see cref="DefaultCodecFactory"/> instance, call
    /// <see cref="Codec.SetCodecFactory(ICodecFactory)"/> at application start up.
    /// <see cref="DefaultCodecFactory"/> can be subclassed or passed additional parameters to register
    /// additional codecs, inject dependencies, or change caching behavior, as shown in the following examples.
    /// Alternatively, <see cref="ICodecFactory"/> can be implemented to provide complete control over
    /// codec creation and lifetimes.
    /// <para/>
    /// <h4>Register Additional Codecs</h4>
    /// <para/>
    /// Additional codecs can be added by initializing the instance of <see cref="DefaultCodecFactory"/> and
    /// passing an array of <see cref="Codec"/>-derived types.
    /// <code>
    /// // Register the factory at application start up.
    /// Codec.SetCodecFactory(new DefaultCodecFactory {
    ///     CustomCodecTypes = new Type[] { typeof(MyCodec), typeof(AnotherCodec) }
    /// });
    /// </code>
    /// <para/>
    /// <h4>Only Use Explicitly Defined Codecs</h4>
    /// <para/>
    /// <see cref="PutCodecType(Type)"/> can be used to explicitly add codec types. In this example,
    /// the call to <c>base.Initialize()</c> is excluded to skip the built-in codec registration.
    /// Since <c>AnotherCodec</c> doesn't have a default constructor, the <see cref="NewCodec(Type)"/>
    /// method is overridden to supply the required parameters.
    /// <code>
    /// public class ExplicitCodecFactory : DefaultCodecFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load specific codecs in a specific order.
    ///         PutCodecType(typeof(MyCodec));
    ///         PutCodecType(typeof(AnotherCodec));
    ///     }
    ///     
    ///     protected override Codec NewCodec(Type type)
    ///     {
    ///         // Special case: AnotherCodec has a required dependency
    ///         if (typeof(AnotherCodec).Equals(type))
    ///             return new AnotherCodec(new SomeDependency());
    ///         
    ///         return base.NewCodec(type);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// Codec.SetCodecFactory(new ExplicitCodecFactory());
    /// </code>
    /// See the <see cref="Lucene.Net.Codecs"/> namespace documentation for more examples of how to
    /// inject dependencies into <see cref="Codec"/> subclasses.
    /// <para/>
    /// <h4>Use Reflection to Scan an Assembly for Codecs</h4>
    /// <para/>
    /// <see cref="ScanForCodecs(Assembly)"/> or <see cref="ScanForCodecs(IEnumerable{Assembly})"/> can be used
    /// to scan assemblies using .NET Reflection for codec types and add all subclasses that are found automatically.
    /// This example calls <c>base.Initialize()</c> to load the default codecs prior to scanning for additional codecs.
    /// <code>
    /// public class ScanningCodecFactory : DefaultCodecFactory
    /// {
    ///     protected override void Initialize()
    ///     {
    ///         // Load all default codecs
    ///         base.Initialize();
    ///         
    ///         // Load all of the codecs inside of the same assembly that MyCodec is defined in
    ///         ScanForCodecs(typeof(MyCodec).Assembly);
    ///     }
    /// }
    /// 
    /// // Register the factory at application start up.
    /// Codec.SetCodecFactory(new ScanningCodecFactory());
    /// </code>
    /// Codecs in the target assemblie(s) can be excluded from the scan by decorating them with
    /// the <see cref="ExcludeCodecFromScanAttribute"/>.
    /// </summary>
    /// <seealso cref="ICodecFactory"/>
    /// <seealso cref="IServiceListable"/>
    /// <seealso cref="ExcludeCodecFromScanAttribute"/>
    // LUCENENET specific
    public class DefaultCodecFactory : NamedServiceFactory<Codec>, ICodecFactory, IServiceListable
    {
        private static readonly Type[] localCodecTypes = new Type[] {
            typeof(Lucene46.Lucene46Codec),
#pragma warning disable 612, 618
            typeof(Lucene3x.Lucene3xCodec), // Optimize 3.x codec over < 4.6 codecs
            typeof(Lucene45.Lucene45Codec),
            typeof(Lucene42.Lucene42Codec),
            typeof(Lucene41.Lucene41Codec),
            typeof(Lucene40.Lucene40Codec),
#pragma warning restore 612, 618
        };

        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> codecNameToTypeMap;
        private readonly IDictionary<Type, Codec> codecInstanceCache;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultCodecFactory"/>.
        /// </summary>
        public DefaultCodecFactory()
        {
            codecNameToTypeMap = new Dictionary<string, Type>();
            codecInstanceCache = new Dictionary<Type, Codec>();
        }

        /// <summary>
        /// An array of custom <see cref="Codec"/>-derived types to be registered. This property
        /// can be initialized during construction of <see cref="DefaultCodecFactory"/>
        /// to make your custom codecs known to Lucene.
        /// <para/>
        /// These types will be registered after the default Lucene types, so if a custom type has the same
        /// name as a Lucene <see cref="Codec"/> (via <see cref="CodecNameAttribute"/>) 
        /// the custom type will replace the Lucene type with the same name.
        /// </summary>
        public IEnumerable<Type> CustomCodecTypes { get; set; }

        /// <summary>
        /// Initializes the codec type cache with the known <see cref="Codec"/> types.
        /// Override this method (and optionally call <c>base.Initialize()</c>) to add your
        /// own <see cref="Codec"/> types by calling <see cref="PutCodecType(Type)"/> 
        /// or <see cref="ScanForCodecs(Assembly)"/>.
        /// <para/>
        /// If two types have the same name by using the <see cref="CodecNameAttribute"/>, the
        /// last one registered wins.
        /// </summary>
        protected override void Initialize()
        {
            foreach (var codecType in localCodecTypes)
                PutCodecTypeImpl(codecType);
            ScanForCodecs(this.CodecsAssembly);
            if (CustomCodecTypes != null)
            {
                foreach (var codecType in CustomCodecTypes)
                    PutCodecType(codecType);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assemblies"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="codecNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="Codec"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assemblies">A list of assemblies to scan. The assemblies will be scanned from first to last, 
        /// and the last match for each <see cref="Codec"/> name wins.</param>
        protected virtual void ScanForCodecs(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanForCodecs(assembly);
            }
        }

        /// <summary>
        /// Scans the given <paramref name="assembly"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="codecNameToTypeMap"/>. Note that names will be
        /// automatically overridden if the <see cref="Codec"/> name appears multiple times - the last match wins.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        protected virtual void ScanForCodecs(Assembly assembly)
        {
            if (assembly is null) return;

            foreach (var c in assembly.GetTypes())
            {
                if (IsServiceType(c))
                {
                    PutCodecTypeImpl(c);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="Codec"/> type to the <see cref="codecNameToTypeMap"/>, using 
        /// the name provided in the <see cref="CodecNameAttribute"/>, if present, or the name
        /// of the codec class minus the "Codec" suffix as the name by default.
        /// <para/>
        /// Note that if a <see cref="Codec"/> with the same name already exists in the map,
        /// calling this method will update it to the new type.
        /// </summary>
        /// <param name="codec">A type that subclasses <see cref="Codec"/>.</param>
        protected virtual void PutCodecType(Type codec)
        {
            if (codec is null)
                throw new ArgumentNullException(nameof(codec));
            if (!typeof(Codec).IsAssignableFrom(codec))
                throw new ArgumentException($"The supplied type {codec.AssemblyQualifiedName} does not subclass {nameof(Codec)}.");

            PutCodecTypeImpl(codec);
        }

        private void PutCodecTypeImpl(Type codec)
        {
            string name = GetServiceName(codec);
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                codecNameToTypeMap[name] = codec;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
            }
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> instance to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        public virtual Codec GetCodec(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            UninterruptableMonitor.Enter(m_initializationLock);
            try
            {
                Type codecType = GetCodecType(name);
                return GetCodec(codecType);
            }
            finally
            {
                UninterruptableMonitor.Exit(m_initializationLock);
            }
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Codec"/> to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        protected virtual Codec GetCodec(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (!codecInstanceCache.TryGetValue(type, out Codec instance))
            {
                UninterruptableMonitor.Enter(m_initializationLock);
                try
                {
                    if (!codecInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = NewCodec(type);
                        codecInstanceCache[type] = instance;
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
        /// Instantiates a <see cref="Codec"/> based on the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Codec"/> to instantiate.</param>
        /// <returns>The new instance.</returns>
        protected virtual Codec NewCodec(Type type)
        {
            return (Codec)Activator.CreateInstance(type, IsFullyTrusted);
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="Codec"/> <see cref="Type"/>.</returns>
        protected virtual Type GetCodecType(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            EnsureInitialized();
            if (!codecNameToTypeMap.TryGetValue(name, out Type codecType) || codecType is null)
            {
                throw new ArgumentException($"Codec '{name}' cannot be loaded. If the codec is not " +
                    $"in a Lucene.Net assembly, you must subclass {typeof(DefaultCodecFactory).FullName}, " +
                    "override the Initialize() method, and call PutCodecType() or ScanForCodecs() to add " +
                    $"the type manually. Call {typeof(Codec).FullName}.SetCodecFactory() at application " +
                    "startup to initialize your custom codec.");
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="Codec"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="T:ICollection{string}"/> of <see cref="Codec"/> names.</returns>
        public virtual ICollection<string> AvailableServices
        {
            get
            {
                EnsureInitialized();
                return codecNameToTypeMap.Keys;
            }
        }
    }
}
