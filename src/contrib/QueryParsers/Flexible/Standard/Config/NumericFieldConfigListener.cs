using Lucene.Net.QueryParsers.Flexible.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class NumericFieldConfigListener : IFieldConfigListener
    {
        private readonly QueryConfigHandler config;

        public NumericFieldConfigListener(QueryConfigHandler config)
        {
            if (config == null)
            {
                throw new ArgumentException("config cannot be null!");
            }

            this.config = config;
        }

        public void BuildFieldConfig(FieldConfig fieldConfig)
        {
            IDictionary<string, NumericConfig> numericConfigMap = config.Get(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG_MAP);

            if (numericConfigMap != null)
            {
                NumericConfig numericConfig = numericConfigMap[fieldConfig.Field];

                if (numericConfig != null)
                {
                    fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG, numericConfig);
                }
            }
        }
    }
}
