using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lucene.Net.Codecs
{
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
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> to scan additional assemblies for <see cref="DocValuesFormat"/>
    ///         subclasses in the constructor by calling <see cref="ScanForDocValuesFormats(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    ///     <item>subclass <see cref="DefaultDocValuesFormatFactory"/> to add override the default <see cref="DocValuesFormat"/> 
    ///         types by calling <see cref="PutDocValuesFormatType(Type)"/>.</item>
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

        public DefaultDocValuesFormatFactory()
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
                throw new ArgumentException("System.Type passed dose not subclass DocValuesFormat.");
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
                instance = (DocValuesFormat)Activator.CreateInstance(type, true);
                docValuesFormatInstanceCache[type] = instance;
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
            Type codecType;
            docValuesFormatNameToTypeMap.TryGetValue(name, out codecType);
            if (codecType == null)
            {
                throw new ArgumentException(string.Format("DocValuesFormat '{0}' cannot be loaded. If the format is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultDocValuesFormatFactory and call ScanForDocValuesFormats() with the " +
                    "target assembly from the subclass constructor.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="DocValuesFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="ICollection{string}"/> of <see cref="DocValuesFormat"/> names.</returns>
        public ICollection<string> AvailableServices()
        {
            return docValuesFormatNameToTypeMap.Keys;
        }
    }
}
