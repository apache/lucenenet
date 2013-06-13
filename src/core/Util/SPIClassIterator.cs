using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary>
    /// TODO: Not sure what to do here.
    /// </summary>
    /// <typeparam name="S"></typeparam>
    public class SPIClassIterator<S> : IEnumerable<Type>, IEnumerator<Type>
    {
        private static readonly string META_INF_SERVICES = "META-INF/services/";

        private readonly Type clazz;
        private readonly IEnumerable<Uri> profilesEnum;
        private IEnumerator<string> linesIterator;

        public static SPIClassIterator<S> Get(Type clazz)
        {
            throw new NotImplementedException();
        }
        
        public Type Current
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        object System.Collections.IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}
