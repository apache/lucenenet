using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public interface IResourceLoader
    {
        Stream OpenResource(string resource);

        Type FindClass(string cname, Type expectedType);

        T NewInstance<T>(string cname);
    }
}
