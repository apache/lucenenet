using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    /// <summary>
    /// An instance of this class represents a key that is used to retrieve a value
    /// from {@link AbstractQueryConfig}. It also holds the value's type, which is
    /// defined in the generic argument.
    /// </summary>
    /// <seealso cref="AbstractQueryConfig"/>
    /// <typeparam name="T"></typeparam>
    public sealed class ConfigurationKey<T> : ConfigurationKey
    {
        internal ConfigurationKey() { }

        /**
         * Creates a new instance.
         * 
         * @param <T> the value's type
         * 
         * @return a new instance
         */
        //public static ConfigurationKey<T> NewInstance()
        //{
        //    return new ConfigurationKey<T>();
        //}
    }

    /// <summary>
    /// LUCENENET specific class used to access the NewInstance
    /// static method without referring to the ConfigurationKey{T}'s
    /// generic closing type.
    /// </summary>
    public abstract class ConfigurationKey
    {
        public static ConfigurationKey<T> NewInstance<T>()
        {
            return new ConfigurationKey<T>();
        }
    }
}
