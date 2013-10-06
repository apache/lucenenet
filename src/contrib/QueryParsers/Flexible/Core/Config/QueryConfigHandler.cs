using Lucene.Net.QueryParsers.Flexible.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    public abstract class QueryConfigHandler : AbstractQueryConfig
    {
        private readonly LinkedList<IFieldConfigListener> listeners = new LinkedList<IFieldConfigListener>();

        public FieldConfig GetFieldConfig(string fieldName)
        {
            FieldConfig fieldConfig = new FieldConfig(StringUtils.ToString(fieldName));

            foreach (IFieldConfigListener listener in this.listeners)
            {
                listener.BuildFieldConfig(fieldConfig);
            }

            return fieldConfig;
        }

        public void AddFieldConfigListener(IFieldConfigListener listener)
        {
            this.listeners.AddLast(listener);
        }
    }
}
