using Lucene.Net.QueryParsers.Flexible.Core.Config;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    /// <summary>
    /// This listener listens for every field configuration request and assign a
    /// {@link ConfigurationKeys#BOOST} to the
    /// equivalent {@link FieldConfig} based on a defined map: fieldName -> boostValue stored in
    /// {@link ConfigurationKeys#FIELD_BOOST_MAP}.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#FIELD_BOOST_MAP"/>
    /// <seealso cref="ConfigurationKeys#BOOST"/>
    /// <seealso cref="FieldConfig"/>
    /// <seealso cref="IFieldConfigListener"/>
    public class FieldBoostMapFCListener : IFieldConfigListener
    {
        private QueryConfigHandler config = null;

        public FieldBoostMapFCListener(QueryConfigHandler config)
        {
            this.config = config;
        }

        public virtual void BuildFieldConfig(FieldConfig fieldConfig)
        {
            IDictionary<string, float?> fieldBoostMap = this.config.Get(ConfigurationKeys.FIELD_BOOST_MAP);

            if (fieldBoostMap != null)
            {
                float? boost;
                if (fieldBoostMap.TryGetValue(fieldConfig.Field, out boost) && boost != null)
                {
                    fieldConfig.Set(ConfigurationKeys.BOOST, boost);
                }
            }
        }
    }
}
