using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Analysis.Util
{
    internal sealed class AnalysisSPILoader<S>
        where S : AbstractAnalysisFactory
    {
        private volatile IDictionary<string, Type> services = new HashMap<string, Type>();
        private readonly Type clazz;
        private readonly string[] suffixes;

        public AnalysisSPILoader(Type clazz)
            : this(clazz, new string[] { clazz.Name })
        {
        }

        public AnalysisSPILoader(Type clazz, string[] suffixes)
        {
            this.clazz = clazz;
            this.suffixes = suffixes;
            // if clazz' classloader is not a parent of the given one, we scan clazz's classloader, too:
            //final ClassLoader clazzClassloader = clazz.getClassLoader();
            //if (clazzClassloader != null && !SPIClassIterator.isParentClassLoader(clazzClassloader, classloader)) {
            //  reload(clazzClassloader);
            //}
            Reload();
        }

        public void Reload()
        {
            lock (this)
            {
                HashMap<String, Type> services =
                  new HashMap<String, Type>(this.services);
                SPIClassIterator<S> loader = SPIClassIterator<S>.Get();
                foreach (var service in loader)
                {
                    //Class<? extends S> service = loader.next();
                    String clazzName = service.Name;
                    String name = null;
                    foreach (String suffix in suffixes)
                    {
                        if (clazzName.EndsWith(suffix))
                        {
                            name = clazzName.Substring(0, clazzName.Length - suffix.Length).ToLowerInvariant();
                            break;
                        }
                    }
                    if (name == null)
                    {
                        throw new InvalidOperationException("The class name " + service.FullName +
                          " has wrong suffix, allowed are: " + Arrays.ToString(suffixes));
                    }
                    // only add the first one for each name, later services will be ignored
                    // this allows to place services before others in classpath to make 
                    // them used instead of others
                    //
                    // TODO: Should we disallow duplicate names here?
                    // Allowing it may get confusing on collisions, as different packages
                    // could contain same factory class, which is a naming bug!
                    // When changing this be careful to allow reload()!
                    if (!services.ContainsKey(name))
                    {
                        services[name] = service;
                    }
                }
                //this.services = Collections.unmodifiableMap(services);
            }
        }

        public S NewInstance(string name, IDictionary<string, string> args)
        {
            Type service = LookupClass(name);
            try
            {
                //var ctor = service.GetConstructor(new[] { typeof(IDictionary<string, string>) });
                return (S)Activator.CreateInstance(service, args);
            }
            catch (Exception e)
            {
                throw new ArgumentException("SPI class of type " + clazz.FullName + " with name '" + name + "' cannot be instantiated. " +
                      "This is likely due to a misconfiguration of the java class '" + service.FullName + "': ", e);
            }
        }

        public Type LookupClass(String name)
        {
            Type service = services[name.ToLowerInvariant()];
            if (service != null)
            {
                return service;
            }
            else
            {
                throw new ArgumentException("A SPI class of type " + clazz.FullName + " with name '" + name + "' does not exist. " +
                    "You need to add the corresponding JAR file supporting this SPI to your classpath." +
                    "The current classpath supports the following names: " + Arrays.ToString(AvailableServices));
            }
        }

        public ICollection<String> AvailableServices
        {
            get
            {
                return services.Keys;
            }
        }
    }
}
