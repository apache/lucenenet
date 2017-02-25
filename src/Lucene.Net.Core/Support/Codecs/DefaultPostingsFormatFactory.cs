using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lucene.Net.Codecs
{
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
    ///     <item>subclass <see cref="DefaultDPostingsFormatFactory"/> and override
    ///         <see cref="DefaultPostingsFormatFactory.GetPostingsFormatType(string)"/> so a type new type can be
    ///         supplied that is not in the <see cref="DefaultPostingsFormatFactory.m_postingsFormatNameToTypeMap"/>.</item>
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> to scan additional assemblies for <see cref="PostingsFormat"/>
    ///         subclasses in the constructor by calling <see cref="ScanForPostingsFormats(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    ///     <item>subclass <see cref="DefaultPostingsFormatFactory"/> to add override the default <see cref="PostingsFormat"/> 
    ///         types by explicitly setting them in the <see cref="DefaultPostingsFormatFactory.m_postingsFormatNameToTypeMap"/>.</item>
    /// </list>
    /// <para/>
    /// To set the <see cref="IPostingsFormatFactory"/>, call <see cref="DocValuesFormat.SetPostingsFormatFactory(IPostingsFormatFactory)"/>.
    /// </summary>
    public class DefaultPostingsFormatFactory : NamedServiceFactory<PostingsFormat>, IPostingsFormatFactory, IServiceListable
    {
        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        protected readonly IDictionary<string, Type> m_postingsFormatNameToTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, PostingsFormat> postingsFormatInstanceCache = new Dictionary<Type, PostingsFormat>();

        public DefaultPostingsFormatFactory()
        {
            ScanForPostingsFormats(new Assembly[] {
                typeof(Codec).GetTypeInfo().Assembly,
                this.CodecsAssembly
            });
        }

        /// <summary>
        /// Scans the given <paramref name="assemblies"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="m_postingsFormatNameToTypeMap"/>. Note that names will be
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
        /// and adds their names to the <see cref="m_postingsFormatNameToTypeMap"/>. Note that names will be
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
                    string name = GetServiceName(c);
                    m_postingsFormatNameToTypeMap[name] = c;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="PostingsFormat"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="PostingsFormat"/> instance to retrieve.</param>
        /// <returns>The <see cref="PostingsFormat"/> instance.</returns>
        public virtual PostingsFormat GetPostingsFormat(string name)
        {
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
                instance = (PostingsFormat)Activator.CreateInstance(type, true);
                postingsFormatInstanceCache[type] = instance;
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
            Type codecType;
            m_postingsFormatNameToTypeMap.TryGetValue(name, out codecType);
            if (codecType == null)
            {
                throw new ArgumentException(string.Format("PostingsFormat '{0}' cannot be loaded. If the format is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultPostingsFormatFactory and call ScanForPostingsFormats() with the " +
                    "target assembly from the subclass constructor.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="PostingsFormat"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="ICollection{string}"/> of <see cref="PostingsFormat"/> names.</returns>
        public ICollection<string> AvailableServices()
        {
            return m_postingsFormatNameToTypeMap.Keys;
        }
    }
}
