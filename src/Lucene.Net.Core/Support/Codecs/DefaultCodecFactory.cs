using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lucene.Net.Codecs
{
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
    ///         supplied that is not in the <see cref="DefaultCodecFactory.m_codecNameToTypeMap"/>.</item>
    ///     <item>subclass <see cref="DefaultCodecFactory"/> to scan additional assemblies for <see cref="Codec"/>
    ///         subclasses in the constructor by calling <see cref="ScanForCodecs(Assembly)"/>. 
    ///         For performance reasons, the default behavior only loads Lucene.Net codecs.</item>
    ///     <item>subclass <see cref="DefaultCodecFactory"/> to add override the default <see cref="Codec"/> 
    ///         types by explicitly setting them in the <see cref="DefaultCodecFactory.m_codecNameToTypeMap"/>.</item>
    /// </list>
    /// <para/>
    /// To set the <see cref="ICodecFactory"/>, call <see cref="Codec.SetCodecFactory(ICodecFactory)"/>.
    /// </summary>
    public class DefaultCodecFactory : NamedServiceFactory<Codec>, ICodecFactory, IServiceListable
    {
        // NOTE: The following 2 dictionaries are static, since this instance is stored in a static
        // variable in the Codec class.
        protected readonly IDictionary<string, Type> m_codecNameToTypeMap = new Dictionary<string, Type>();
        private readonly IDictionary<Type, Codec> codecInstanceCache = new Dictionary<Type, Codec>();

        public DefaultCodecFactory()
        {
            ScanForCodecs(new Assembly[] {
                typeof(Codec).GetTypeInfo().Assembly,
                this.CodecsAssembly
            });
        }

        /// <summary>
        /// Scans the given <paramref name="assemblies"/> for subclasses of <see cref="Codec"/>
        /// and adds their names to the <see cref="m_codecNameToTypeMap"/>. Note that names will be
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
        /// and adds their names to the <see cref="m_codecNameToTypeMap"/>. Note that names will be
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
                    string name = GetServiceName(c);
                    m_codecNameToTypeMap[name] = c;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="Codec"/> instance from the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="Codec"/> instance to retrieve.</param>
        /// <returns>The <see cref="Codec"/> instance.</returns>
        public virtual Codec GetCodec(string name)
        {
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
                instance = (Codec)Activator.CreateInstance(type, true);
                codecInstanceCache[type] = instance;
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
            Type codecType;
            m_codecNameToTypeMap.TryGetValue(name, out codecType);
            if (codecType == null)
            {
                throw new InvalidOperationException(string.Format("Codec '{0}' cannot be loaded. If the codec is not " +
                    "in a Lucene.Net assembly, you must subclass DefaultCodecFactory and call ScanForCodecs() with the " + 
                    "target assembly from the subclass constructor.", name));
            }

            return codecType;
        }

        /// <summary>
        /// Gets a list of the available <see cref="Codec"/>s (by name).
        /// </summary>
        /// <returns>A <see cref="ICollection{string}"/> of <see cref="Codec"/> names.</returns>
        public ICollection<string> AvailableServices()
        {
            return m_codecNameToTypeMap.Keys;
        }
    }




    //[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    //public sealed class CodecNameAttribute : Attribute
    //{
    //    public CodecNameAttribute(string name)
    //    {
    //        if (string.IsNullOrEmpty(name))
    //            throw new ArgumentNullException("name");
    //        this.Name = name;
    //    }

    //    public string Name { get; private set; }
    //}

    

    

    

    


    //public interface IServiceInstanceCache<TService>
    //{

    //}

    //public class ServiceInstanceCache<TService> : IServiceInstanceCache<TService>
    //{

    //}

    //// LUCENENET TODO: Make Codec, DocValuesFormat, and PostingsFormat inherit this
    //public abstract class NamedService<TService>
    //{
        
    //}

    //public interface IServiceNameProvider
    //{
    //    string GetServiceName(Type type);
    //}

    //public class ServiceNameProvider : IServiceNameProvider
    //{
    //    public string GetServiceName(Type type)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected virtual string GetCanonicalName(Type t)
    //    {
    //        string name = t.Name;
    //        string genericSuffix = string.Empty;
    //        int genericIndex = name.IndexOf("`");
    //        if (genericIndex > -1)
    //        {
    //            genericSuffix = name.Substring(genericIndex);
    //            name = name.Substring(0, genericIndex);
    //        }
    //        if (name.EndsWith(typeof(TService).GetTypeInfo().Name, StringComparison.Ordinal))
    //        {
    //            name = name.Substring(0, name.Length - 5);
    //        }
    //        // Reappend the suffix to make the name unique (.NET specific)
    //        return name + genericSuffix;
    //    }
    //}


    //[CodecName("Argh")]
    //public class MyFooCodec : Codec
    //{
    //    public MyFooCodec() : base("MyFoo")
    //    {
    //    }

    //    public override DocValuesFormat DocValuesFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override FieldInfosFormat FieldInfosFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override LiveDocsFormat LiveDocsFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override NormsFormat NormsFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override PostingsFormat PostingsFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override SegmentInfoFormat SegmentInfoFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override StoredFieldsFormat StoredFieldsFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }

    //    public override TermVectorsFormat TermVectorsFormat
    //    {
    //        get
    //        {
    //            throw new NotImplementedException();
    //        }
    //    }
    //}
}
