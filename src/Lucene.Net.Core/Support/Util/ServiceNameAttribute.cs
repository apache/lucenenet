using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    /// <summary>
    /// LUCENENET specific abstract class for <see cref="System.Attribute"/>s that can
    /// be used to override the default convention-based names of services. For example,
    /// "Lucene40Codec" will by convention be named "Lucene40". Using the <see cref="Codecs.CodecNameAttribute"/>,
    /// the name can be overridden with a custom value.
    /// </summary>
    public abstract class ServiceNameAttribute : System.Attribute
    {
        public ServiceNameAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            this.Name = name;
        }

        public string Name { get; private set; }
    }
}
