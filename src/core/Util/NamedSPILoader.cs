using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class NamedSPILoader<S> : IEnumerable<S>
        where S : NamedSPILoader.NamedSPI
    {
        private volatile IDictionary<string, S> services = Collections.EmptyMap<string, S>();

        private readonly Type clazz;

        public NamedSPILoader(Type clazz)
        {
            // no additional overload to take a ClassLoader, since we're not using MEF or anything
            this.clazz = clazz;

            Reload();
        }

        public void Reload()
        {
            IDictionary<String, S> services = new Dictionary<String, S>(this.services);
            SPIClassIterator<S> loader = SPIClassIterator<S>.Get();
            
            foreach (Type c in loader)
            {
                try
                {
                    S service = (S)Activator.CreateInstance(c);
                    String name = service.Name;

                    // only add the first one for each name, later services will be ignored
                    // this allows to place services before others in classpath to make 
                    // them used instead of others
                    if (!services.ContainsKey(name))
                    {
                        NamedSPILoader.CheckServiceName(name);
                        services[name] = service;
                    }
                }
                catch (Exception e)
                {
                    // in java, this is ServiceConfigurationError
                    throw new InvalidOperationException("Cannot instantiate SPI class: " + c.Name, e);
                }
            }

            this.services = Collections.UnmodifiableMap(services);
        }
        
        public S Lookup(string name)
        {
            S service = services[name];

            if (service != null) return service;

            throw new ArgumentException("A SPI class of type " + clazz.Name + " with name '" + name + "' does not exist. " +
                 "You need to add the corresponding DLL file supporting this SPI to your bin folder." +
                 "The current bin folder supports the following names: " + string.Join(", ", AvailableServices));
        }

        public ICollection<string> AvailableServices
        {
            get
            {
                return services.Keys;
            }
        }

        public IEnumerator<S> GetEnumerator()
        {
            return services.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // .NET port: non-generic static methods and nested type
    public static class NamedSPILoader
    {
        public interface NamedSPI
        {
            string Name { get; }
        }

        public static void CheckServiceName(String name)
        {
            // based on harmony charset.java
            if (name.Length >= 128)
            {
                throw new ArgumentException("Illegal service name: '" + name + "' is too long (must be < 128 chars).");
            }
            for (int i = 0, len = name.Length; i < len; i++)
            {
                char c = name[i];
                if (!IsLetterOrDigit(c))
                {
                    throw new ArgumentException("Illegal service name: '" + name + "' must be simple ascii alphanumeric.");
                }
            }
        }

        private static bool IsLetterOrDigit(char c)
        {
            return ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || ('0' <= c && c <= '9');
        }
    }
}
