using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Document
{
    public class BinaryDocValuesField : Field
    {

        /**
         * Type for straight bytes DocValues.
   */

        public static readonly FieldType TYPE = new FieldType();

        public static BinaryDocValuesField()
        {
            TYPE.setDocValueType(FieldInfo.DocValuesType.BINARY);
            TYPE.freeze();
        }


        /**
   * Create a new binary DocValues field.
   * @param name field name
   * @param value binary content
   * @throws IllegalArgumentException if the field name is null
   */

        public BinaryDocValuesField(string name, BytesRef value)
        {
            base(name, TYPE);
            this.fieldsData = value;
        }
    }
}

