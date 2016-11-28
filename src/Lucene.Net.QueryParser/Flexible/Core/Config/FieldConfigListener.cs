using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    /// <summary>
    /// This interface should be implemented by classes that wants to listen for
    /// field configuration requests. The implementation receives a
    /// {@link FieldConfig} object and may add/change its configuration.
    /// </summary>
    /// <seealso cref="FieldConfig"/>
    /// <seealso cref="QueryConfigHandler"/>
    public interface IFieldConfigListener
    {
        /// <summary>
        /// This method is called ever time a field configuration is requested.
        /// </summary>
        /// <param name="fieldConfig">the field configuration requested, should never be null</param>
        void BuildFieldConfig(FieldConfig fieldConfig);
    }
}
