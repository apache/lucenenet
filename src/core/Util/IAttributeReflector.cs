using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public interface IAttributeReflector
    {
        void Reflect<T>(string key, object value)
            where T : IAttribute;

        void Reflect(Type type, string key, object value);
    }
}
