using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    /// <summary>
    /// This class is the base of {@link QueryConfigHandler} and {@link FieldConfig}.
    /// It has operations to set, unset and get configuration values.
    /// <para>
    /// Each configuration is is a key->value pair. The key should be an unique
    /// {@link ConfigurationKey} instance and it also holds the value's type.
    /// </para>
    /// <seealso cref="ConfigurationKey"/>
    /// </summary>
    public abstract class AbstractQueryConfig
    {
        private readonly IDictionary<ConfigurationKey, object> configMap = new Dictionary<ConfigurationKey, object>();

        internal AbstractQueryConfig()
        {
            // although this class is public, it can only be constructed from package
        }

        /**
         * Returns the value held by the given key.
         * 
         * @param <T> the value's type
         * 
         * @param key the key, cannot be <code>null</code>
         * 
         * @return the value held by the given key
         */
        public virtual T Get<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }
            object result;
            this.configMap.TryGetValue(key, out result);
            return result == null ? default(T) : (T)result;
        }

        /**
         * Returns true if there is a value set with the given key, otherwise false.
         * 
         * @param <T> the value's type
         * @param key the key, cannot be <code>null</code>
         * @return true if there is a value set with the given key, otherwise false
         */
        public virtual bool Has<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.ContainsKey(key);
        }

        /**
         * Sets a key and its value.
         * 
         * @param <T> the value's type
         * @param key the key, cannot be <code>null</code>
         * @param value value to set
         */
        public virtual void Set<T>(ConfigurationKey<T> key, T value)
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

        /**
         * Unsets the given key and its value.
         * 
         * @param <T> the value's type
         * @param key the key
         * @return true if the key and value was set and removed, otherwise false
         */
        public virtual bool Unset<T>(ConfigurationKey<T> key)
        {
            if (key == null)
            {
                throw new ArgumentException("key cannot be null!");
            }

            return this.configMap.Remove(key);
        }
    }
}
