using Lucene.Net.QueryParsers.Flexible.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class FieldBoostMapFCListener : IFieldConfigListener
    {
        private QueryConfigHandler config = null;

        public FieldBoostMapFCListener(QueryConfigHandler config)
        {
            this.config = config;
        }

        public void BuildFieldConfig(FieldConfig fieldConfig)
        {
            IDictionary<string, float> fieldBoostMap = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys.FIELD_BOOST_MAP);

            if (fieldBoostMap != null)
            {
                float boost = fieldBoostMap[fieldConfig.Field];

                if (boost != default(float))
                {
                    fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.BOOST, boost);
                }
            }
        }
    }
}
