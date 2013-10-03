using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    public class FieldConfig : AbstractQueryConfig
    {
        private string fieldName;

        public FieldConfig(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentException("field name should not be null!");
            }

            this.fieldName = fieldName;
        }

        public string Field
        {
            get { return this.fieldName; }
        }

        public override string ToString()
        {
            return "<fieldconfig name=\"" + this.fieldName + "\" configurations=\""
                + base.ToString() + "\"/>";
        }
    }
}
