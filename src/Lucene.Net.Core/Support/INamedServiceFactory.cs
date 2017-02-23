using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public interface INamedServiceFactory<TService>
    {
        TService Create(string serviceName);
    }
}
