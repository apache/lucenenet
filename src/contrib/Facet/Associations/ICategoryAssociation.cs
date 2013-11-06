using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public interface ICategoryAssociation
    {
        void Serialize(ByteArrayDataOutput output);
        
        void Deserialize(ByteArrayDataInput input);
        
        int MaxBytesNeeded { get; }

        string CategoryListID { get; }
    }
}
