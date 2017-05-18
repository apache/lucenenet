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
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> and override
    ///         <see cref="DefaultPostingsFormatFactory.GetPostingsFormat(Type)"/> so an external dependency injection
    ///         container can be used to supply the instances (lifetime should be singleton). Note that you could 
    ///         alternately use the "named type" feature that many DI containers have to supply the type based on name by 
    ///         overriding <see cref="GetDocValuesFormat(string)"/>.</item>
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> and override
    ///         <see cref="DefaultPostingsFormatFactory.GetPostingsFormatType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultPostingsFormatFactory.postingsFormatNameToTypeMap"/>.</item>
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> to add new or override the default <see cref="PostingsFormat"/> 
    ///         types by overriding <see cref="Initialize()"/> and calling <see cref="PutPostingsFormatType(Type)"/>.</item>
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> to scan additional assemblies for <see cref="PostingsFormat"/>
    ///         subclasses in by overriding <see cref="Initialize()"/> and calling <see cref="ScanForPostingsFormats(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    /// </list>
    /// <para/>
    /// To set the <see cref="IPostingsFormatFactory"/>, call <see cref="DocValuesFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/>.
    /// </summary>
    public class DefaultPostingsFormatFactory : NamedServiceFactory<PostingsFormat>, IPostingsFormatFactory, IServiceListable
    {
        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> postingsFormatNameToTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, PostingsFormat> postingsFormatInstanceCache = new Dictionary<Type, PostingsFormat>();
        private object syncLock = new object();

        /// <summary>
        /// Initializes the codec type cache with the known <see cref="PostingsFormat"/> types.
        /// Override this method (and optionally call <c>base.Initialize()</c>) to add your
        /// own <see cref="PostingsFormat"/> types by calling <see cref="PutDocPostingsFormatType(Type)"/> 
        /// or <see cref="ScanForPostingsFormats(Assembly)"/>.
        /// <para/>
        /// If two types have the same name by using the <see cref="PostingsFormatNameAttribute"/>, the
        /// last one registered wins.
        /// </summary>
        protected override void Initialize()
        {
            ScanForPostingsFormats(new Assembly[] {
                typeof(Codec).GetTypeInfo().Assembly,
                this.CodecsAssembly
            });
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
        /// Adds a <see cref="PostingsFormat"/> type to the <see cref="postingsFormaNameToTypeMap"/>, using 
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
            {
                throw new ArgumentNullException("postingsFormat", "postingsFormat may not be null");
            }
            if (!typeof(PostingsFormat).GetTypeInfo().IsAssignableFrom(postingsFormat))
            {
                throw new ArgumentException("The supplied postingsFormat does not subclass PostingsFormat.");
            }

            PutPostingsFormatTypeImpl(postingsFormat);
        }

        private void PutPostingsFormatTypeImpl(Type postingsFormat)
        {
            string name = GetServiceName(postingsFormat);
            postingsFormatNameToTypeMap[name] = postingsFormat;
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        public virtual PostingsFormat GetPostingsFormat(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            Type codecType = GetPostingsFormatType(name);
            return GetPostingsFormat(codecType);
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="PostingsFormat"/> to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        protected virtual PostingsFormat GetPostingsFormat(Type type)
        {
            PostingsFormat instance;
            if (!postingsFormatInstanceCache.TryGetValue(type, out instance))
            {
                lock (syncLock)
                {
                    if (!postingsFormatInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = (PostingsFormat)Activator.CreateInstance(type, IsFullyTrusted);
                        postingsFormatInstanceCache[type] = instance;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> <see cref="Type"/>.</returns>
        protected virtual Type GetPostingsFormatType(string name)
        {
            EnsureInitialized();
            Type codecType;
            if (!postingsFormatNameToTypeMap.TryGetValue(name, out codecType) && codecType == null)
            {
                throw new ArgumentException(string.Format("PostingsFormat '{0}' cannot be loaded. If the format is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultPostingsFormatFactory and call PutPostingsFormatType() " + 
                    "or ScanForPostingsFormats() from the Initialize() method.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="PostingsFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="ICollection{string}"/> of <see cref="PostingsFormat"/> names.</returns>
        public virtual ICollection<string> AvailableServices()
        {
            EnsureInitialized();
            return postingsFormatNameToTypeMap.Keys;
        }
    }
}
