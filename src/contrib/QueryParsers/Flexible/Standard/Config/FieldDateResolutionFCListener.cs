using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class FieldDateResolutionFCListener : IFieldConfigListener
    {
        private QueryConfigHandler config = null;

        public FieldDateResolutionFCListener(QueryConfigHandler config)
        {
            this.config = config;
        }

        public void BuildFieldConfig(FieldConfig fieldConfig)
        {
            DateTools.Resolution dateRes = null;
            IDictionary<string, DateTools.Resolution> dateResMap = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP);

            if (dateResMap != null)
            {
                dateRes = dateResMap[fieldConfig.Field];
            }

            if (dateRes == null)
            {
                dateRes = this.config.Get(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION);
            }

            if (dateRes != null)
            {
                fieldConfig.Set(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION, dateRes);
            }
        }
    }
}
