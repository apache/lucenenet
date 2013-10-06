using Lucene.Net.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class NumericConfig
    {
        private int precisionStep;

        private NumberFormatInfo format;

        private FieldType.NumericType type;

        public NumericConfig(int precisionStep, NumberFormatInfo format, FieldType.NumericType type)
        {
            PrecisionStep = precisionStep;
            NumberFormat = format;
            this.Type = type;
        }

        public int PrecisionStep
        {
            get { return precisionStep; }
            set { precisionStep = value; }
        }

        public NumberFormatInfo NumberFormat
        {
            get { return format; }
            set { format = value; }
        }

        public FieldType.NumericType Type
        {
            get { return type; }
            set { type = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;

            if (obj is NumericConfig)
            {
                NumericConfig other = (NumericConfig)obj;

                if (this.precisionStep == other.precisionStep
                    && this.type == other.type
                    && (this.format == other.format || (this.format.Equals(other.format))))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
