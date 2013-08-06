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
        private static List<Type> _types;

        static SPIClassIterator()
        {
            _types = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(S).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                        _types.Add(type);
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
