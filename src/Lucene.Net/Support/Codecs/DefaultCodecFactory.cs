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
    /// <see cref="ICodecFactory"/>.
    /// <para/>
    /// The most common use cases are:
    /// <list type="bullet">
    ///     <item><description>Initialize <see cref="DefaultCodecFactory"/> with a set of <see cref="CustomCodecTypes"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultCodecFactory"/> and override
    ///         <see cref="DefaultCodecFactory.GetCodec(Type)"/> so an external dependency injection
    ///         container can be used to supply the instances (lifetime should be singleton). Note that you could 
    ///         alternately use the "named type" feature that many DI containers have to supply the type based on name by 
    ///         overriding <see cref="GetCodec(string)"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultCodecFactory"/> and override
    ///         <see cref="DefaultCodecFactory.GetCodecType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultCodecFactory.codecNameToTypeMap"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultCodecFactory"/> to add new or override the default <see cref="Codec"/> 
    ///         types by overriding <see cref="Initialize()"/> and calling <see cref="PutCodecType(Type)"/>.</description></item>
    ///     <item><description>Subclass <see cref="DefaultCodecFactory"/> to scan additional assemblies for <see cref="Codec"/>
    ///         subclasses in by overriding <see cref="Initialize()"/> and calling <see cref="ScanForCodecs(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</description></item>
    /// </list>
    /// <para/>
    /// To set the <see cref="ICodecFactory"/>, call <see cref="Codec.SetCodecFactory(ICodecFactory)"/>.
    /// </summary>
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
            if (assembly == null) return;

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
            if (codec == null)
                throw new ArgumentNullException(nameof(codec));
            if (!typeof(Codec).IsAssignableFrom(codec))
                throw new ArgumentException($"The supplied type {codec.AssemblyQualifiedName} does not subclass {nameof(Codec)}.");

            PutCodecTypeImpl(codec);
        }

        private void PutCodecTypeImpl(Type codec)
        {
            string name = GetServiceName(codec);
            lock (m_initializationLock)
            {
                codecNameToTypeMap[name] = codec;
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
            lock (m_initializationLock)
            {
                Type codecType = GetCodecType(name);
                return GetCodec(codecType);
            }
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Codec"/> to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        protected virtual Codec GetCodec(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!codecInstanceCache.TryGetValue(type, out Codec instance))
            {
                lock (m_initializationLock)
                {
                    if (!codecInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = NewCodec(type);
                        codecInstanceCache[type] = instance;
                    }
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
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            EnsureInitialized();
            if (!codecNameToTypeMap.TryGetValue(name, out Type codecType) && codecType == null)
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
