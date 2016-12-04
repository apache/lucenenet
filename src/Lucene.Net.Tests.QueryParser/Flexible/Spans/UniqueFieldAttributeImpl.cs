using Lucene.Net.Util;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This attribute is used by the <see cref="UniqueFieldQueryNodeProcessor"/>
    /// processor. It holds a value that defines which is the unique field name that
    /// should be set in every <see cref="Core.Nodes.IFieldableNode"/>.
    /// </summary>
    /// <seealso cref="UniqueFieldQueryNodeProcessor"/>
    public class UniqueFieldAttributeImpl : AttributeImpl, IUniqueFieldAttribute
    {
        private string uniqueField;

        public UniqueFieldAttributeImpl()
        {
            Clear();
        }

        public override void Clear()
        {
            this.uniqueField = "";
        }

        public virtual string UniqueField
        {
            get { return this.uniqueField; }
            set { this.uniqueField = value; }
        }

        public override void CopyTo(Attribute target)
        {

            if (!(target is UniqueFieldAttributeImpl))
            {
                throw new System.ArgumentException(
                    "cannot copy the values from attribute UniqueFieldAttribute to an instance of "
                        + target.GetType().Name);
            }

            UniqueFieldAttributeImpl uniqueFieldAttr = (UniqueFieldAttributeImpl)target;
            uniqueFieldAttr.uniqueField = uniqueField.toString();
        }

        public override bool Equals(object other)
        {
            if (other is UniqueFieldAttributeImpl)
            {

                return ((UniqueFieldAttributeImpl)other).uniqueField
                    .equals(this.uniqueField);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.uniqueField.GetHashCode();
        }

        public override string ToString()
        {
            return "<uniqueField uniqueField='" + this.uniqueField + "'/>";
        }
    }
}
