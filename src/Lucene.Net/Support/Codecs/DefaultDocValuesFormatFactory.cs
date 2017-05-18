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
    /// <see cref="IDocValuesFormatFactory"/>.
    /// <para/>
    /// The most common use cases are:
    /// <list type="bullet">
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> and override
    ///         <see cref="DefaultDocValuesFormatFactory.GetDocValuesFormat(Type)"/> so an external dependency injection
    ///         container can be used to supply the instances (lifetime should be singleton). Note that you could 
    ///         alternately use the "named type" feature that many DI containers have to supply the type based on name by 
    ///         overriding <see cref="GetDocValuesFormat(string)"/>.</item>
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> and override
    ///         <see cref="DefaultDocValuesFormatFactory.GetDocValuesFormatType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultDocValuesFormatFactory.docValuesFormatNameToTypeMap"/>.</item>
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> to add new or override the default <see cref="DocValuesFormat"/> 
    ///         types by overriding <see cref="Initialize()"/> and calling <see cref="PutDocValuesFormatType(Type)"/>.</item>
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> to scan additional assemblies for <see cref="DocValuesFormat"/>
    ///         subclasses in by overriding <see cref="Initialize()"/> and calling <see cref="ScanForDocValuesFormats(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    /// </list>
    /// <para/>
    /// To set the <see cref="IDocValuesFormatFactory"/>, call <see cref="DocValuesFormat.SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>.
    /// </summary>
    public class DefaultDocValuesFormatFactory : NamedServiceFactory<DocValuesFormat>, IDocValuesFormatFactory, IServiceListable
    {
        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        private readonly IDictionary<string, Type> docValuesFormatNameToTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, DocValuesFormat> docValuesFormatInstanceCache = new Dictionary<Type, DocValuesFormat>();
        private object syncLock = new object();

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
            ScanForDocValuesFormats(new Assembly[] {
                typeof(Codec).GetTypeInfo().Assembly,
                this.CodecsAssembly
            });
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
            if (docValuesFormat == null)
            {
                throw new ArgumentNullException("docValuesFormat", "docValuesFormat may not be null");
            }
            if (!typeof(DocValuesFormat).GetTypeInfo().IsAssignableFrom(docValuesFormat))
            {
                throw new ArgumentException("The supplied docValuesFormat does not subclass DocValuesFormat.");
            }

            PutCodecTypeImpl(docValuesFormat);
        }

        private void PutCodecTypeImpl(Type docValuesFormat)
        {
            string name = GetServiceName(docValuesFormat);
            docValuesFormatNameToTypeMap[name] = docValuesFormat;
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="DocValuesFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> instance.</returns>
        public virtual DocValuesFormat GetDocValuesFormat(string name)
        {
            EnsureInitialized(); // Safety in case a subclass doesn't call it
            Type codecType = GetDocValuesFormatType(name);
            return GetDocValuesFormat(codecType);
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> instance from the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="DocValuesFormat"/> to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> instance.</returns>
        protected virtual DocValuesFormat GetDocValuesFormat(Type type)
        {
            DocValuesFormat instance;
            if (!docValuesFormatInstanceCache.TryGetValue(type, out instance))
            {
                lock (syncLock)
                {
                    if (!docValuesFormatInstanceCache.TryGetValue(type, out instance))
                    {
                        instance = (DocValuesFormat)Activator.CreateInstance(type, IsFullyTrusted);
                        docValuesFormatInstanceCache[type] = instance;
                    }
                }
            }

            return instance;
        }

        /// <summary>
        /// Gets the <see cref="DocValuesFormat"/> <see cref="Type"/> from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="DocValuesFormat"/> <see cref="Type"/> to retrieve.</param>
        /// <returns>The <see cref="DocValuesFormat"/> <see cref="Type"/>.</returns>
        protected virtual Type GetDocValuesFormatType(string name)
        {
            EnsureInitialized();
            Type codecType;
            if (!docValuesFormatNameToTypeMap.TryGetValue(name, out codecType) && codecType == null)
            {
                throw new ArgumentException(string.Format("DocValuesFormat '{0}' cannot be loaded. If the format is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultDocValuesFormatFactory and call PutDocValuesFormatType() " + 
                    "or ScanForDocValuesFormats() from the Initialize() method.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="DocValuesFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="T:ICollection{string}"/> of <see cref="DocValuesFormat"/> names.</returns>
        public virtual ICollection<string> AvailableServices()
        {
            EnsureInitialized();
            return docValuesFormatNameToTypeMap.Keys;
        }
    }
}
