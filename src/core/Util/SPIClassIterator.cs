using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary>
    /// TODO: Not sure what to do here.
    /// </summary>
    /// <typeparam name="S"></typeparam>
    public class SPIClassIterator<S> : IEnumerable<Type>
    {
        private static HashSet<Type> _types;

        static SPIClassIterator()
        {
            _types = new HashSet<Type>();

            // .NET Port Hack: We do a 2-level deep check here because if the assembly you're
            // hoping would be loaded hasn't been loaded yet into the app domain,
            // it is unavailable. So we go to the next level on each and check each referenced
            // assembly.

            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in loadedAssembly.GetTypes())
                    {
                        try
                        {
                            if (typeof(S).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                                _types.Add(type);
                        }
                        catch
                        {
                            // swallow
                        }
                    }
                }
                catch
                {
                    // swallow
                }

                foreach (var assemblyName in loadedAssembly.GetReferencedAssemblies())
                {                    
                    try
                    {
                        var assembly = Assembly.Load(assemblyName);

                        foreach (var type in assembly.GetTypes())
                        {
                            try
                            {
                                if (typeof(S).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                                    _types.Add(type);
                            }
                            catch
                            {
                                // swallow
                            }
                        }
                    }
                    catch
                    {
                        // swallow
                    }
                }
            }
        }

        //private static readonly string META_INF_SERVICES = "META-INF/services/";

        

        public static SPIClassIterator<S> Get()
        {
            return new SPIClassIterator<S>();
        }
        
        public IEnumerator<Type> GetEnumerator()
        {
            return _types.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
