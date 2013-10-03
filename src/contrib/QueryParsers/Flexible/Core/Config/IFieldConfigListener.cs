using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    public interface IFieldConfigListener
    {
        void BuildFieldConfig(FieldConfig fieldConfig);
    }
}
