using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    public abstract class AbstractQueryConfig
    {
        private readonly HashMap<ConfigurationKey, object> configMap = new HashMap<ConfigurationKey, object>();

        internal AbstractQueryConfig()
        {
            // although this class is public, it can only be constructed from package
        }

        public T Get<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return (T)this.configMap[key];
        }

        public bool Has<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.ContainsKey(key);
        }

        public void Set<T>(ConfigurationKey<T> key, T value)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            if (value == null)
            {
                Unset(key);
            }
            else
            {
                this.configMap[key] = value;
            }
        }

        public bool Unset<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.Remove(key);
        }
    }
}
