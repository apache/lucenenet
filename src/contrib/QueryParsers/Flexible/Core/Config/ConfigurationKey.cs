using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    public sealed class ConfigurationKey<T> : ConfigurationKey
    {
        internal ConfigurationKey() 
        {
            this.t = typeof(T);
        }        
    }

    public class ConfigurationKey
    {
        protected Type t;

        public static ConfigurationKey<T> NewInstance<T>()
        {
            return new ConfigurationKey<T>();
        }

        public override int GetHashCode()
        {
            return t.GetHashCode();
        }
    }
}
