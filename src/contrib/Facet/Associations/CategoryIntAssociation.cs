using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class CategoryIntAssociation : ICategoryAssociation
    {
        public static readonly string ASSOCIATION_LIST_ID = @"$assoc_int$";
        private int value;

        public CategoryIntAssociation()
        {
        }

        public CategoryIntAssociation(int value)
        {
            this.value = value;
        }

        public void Serialize(ByteArrayDataOutput output)
        {
            try
            {
                output.WriteInt(value);
            }
            catch (IOException e)
            {
                throw new Exception(@"unexpected exception writing to a byte[]", e);
            }
        }

        public void Deserialize(ByteArrayDataInput input)
        {
            value = input.ReadInt();
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

        public virtual int Value
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
