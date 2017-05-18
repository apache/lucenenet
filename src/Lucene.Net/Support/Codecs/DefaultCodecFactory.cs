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
    ///     <item>subclass <see cref="DefaultCodecFactory"/> and override
    ///         <see cref="DefaultCodecFactory.GetCodec(Type)"/> so an external dependency injection
    ///         container can be used to supply the instances (lifetime should be singleton). Note that you could 
    ///         alternately use the "named type" feature that many DI containers have to supply the type based on name by 
    ///         overriding <see cref="GetCodec(string)"/>.</item>
    ///     <item>subclass <see cref="DefaultCodecFactory"/> and override
    ///         <see cref="DefaultCodecFactory.GetCodecType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultCodecFactory.codecNameToTypeMap"/>.</item>
    ///     <item>subclass <see cref="DefaultCodecFactory"/> to add new or override the default <see cref="Codec"/> 
    ///         types by overriding <see cref="Initialize()"/> and calling <see cref="PutCodecType(Type)"/>.</item>
    ///     <item>subclass <see cref="DefaultCodecFactory"/> to scan additional assemblies for <see cref="Codec"/>
    ///         subclasses in by overriding <see cref="Initialize()"/> and calling <see cref="ScanForCodecs(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    /// </list>
    /// <para/>
    /// To set the <see cref="ICodecFactory"/>, call <see cref="Codec.SetCodecFactory(ICodecFactory)"/>.
    /// </summary>
    public class DefaultCodecFactory : NamedServiceFactory<Codec>, ICodecFactory, IServiceListable
    {
        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> codecNameToTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, Codec> codecInstanceCache = new Dictionary<Type, Codec>();
        private object syncLock = new object();

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
            ScanForCodecs(new Assembly[] {
                typeof(Codec).GetTypeInfo().Assembly,
                this.CodecsAssembly
            });
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
            {
                throw new ArgumentNullException("codec", "codec may not be null");
            }
            if (!typeof(Codec).GetTypeInfo().IsAssignableFrom(codec))
            {
                throw new ArgumentException("The supplied codec does not subclass Codec.");
            }

            PutCodecTypeImpl(codec);
        }

        private void PutCodecTypeImpl(Type codec)
        {
            string name = GetServiceName(codec);
            codecNameToTypeMap[name] = codec;
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> instance to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        public virtual Codec GetCodec(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            Type codecType = GetCodecType(name);
            return GetCodec(codecType);
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Codec"/> to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        protected virtual Codec GetCodec(Type type)
        {
            Codec instance;
            if (!codecInstanceCache.TryGetValue(type, out instance))
            {
                lock (syncLock)
                {
                    if (!codecInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = (Codec)Activator.CreateInstance(type, IsFullyTrusted);
                        codecInstanceCache[type] = instance;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="Codec"/> <see cref="Type"/>.</returns>
        protected virtual Type GetCodecType(string name)
        {
            EnsureInitialized();
            Type codecType;
            if (!codecNameToTypeMap.TryGetValue(name, out codecType) && codecType == null)
            {
                throw new ArgumentException(string.Format("Codec '{0}' cannot be loaded. If the codec is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultCodecFactory and call PutCodecType() or " + 
                    "ScanForCodecs() from the Initialize() method.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="Codec"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="T:ICollection{string}"/> of <see cref="Codec"/> names.</returns>
        public virtual ICollection<string> AvailableServices()
        {
            EnsureInitialized();
            return codecNameToTypeMap.Keys;
        }
    }
}
