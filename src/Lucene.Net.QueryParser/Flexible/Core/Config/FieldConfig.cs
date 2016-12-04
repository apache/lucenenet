using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
{
    /// <summary>
    /// This class represents a field configuration.
    /// </summary>
    public class FieldConfig : AbstractQueryConfig
    {
        private string fieldName;

        /**
         * Constructs a {@link FieldConfig}
         * 
         * @param fieldName the field name, it cannot be null
         * @throws IllegalArgumentException if the field name is null
         */
        public FieldConfig(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentException("field name should not be null!");
            }

            this.fieldName = fieldName;
        }

        /**
         * Returns the field name this configuration represents.
         * 
         * @return the field name
         */
        public virtual string Field
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
