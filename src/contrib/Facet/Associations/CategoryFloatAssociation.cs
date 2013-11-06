using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class CategoryFloatAssociation : ICategoryAssociation
    {
        public static readonly string ASSOCIATION_LIST_ID = @"$assoc_float$";
        private float value;

        public CategoryFloatAssociation()
        {
        }

        public CategoryFloatAssociation(float value)
        {
            this.value = value;
        }

        public void Serialize(ByteArrayDataOutput output)
        {
            try
            {
                output.WriteInt(Number.FloatToIntBits(value));
            }
            catch (IOException e)
            {
                throw new Exception(@"unexpected exception writing to a byte[]", e);
            }
        }

        public void Deserialize(ByteArrayDataInput input)
        {
            value = Number.IntBitsToFloat(input.ReadInt());
        }

        public int MaxBytesNeeded
        {
            get
            {
                return 4;
            }
        }

        public string CategoryListID
        {
            get
            {
                return ASSOCIATION_LIST_ID;
            }
        }

        public virtual float Value
        {
            get
            {
                return value;
            }
        }

        public override string ToString()
        {
            return GetType().Name + @"(" + value + @")";
        }
    }
}
