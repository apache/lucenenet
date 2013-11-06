using Lucene.Net.Facet.Index;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class AssociationsListBuilder : ICategoryListBuilder
    {
        private readonly CategoryAssociationsContainer associations;
        private readonly ByteArrayDataOutput output = new ByteArrayDataOutput();

        public AssociationsListBuilder(CategoryAssociationsContainer associations)
        {
            this.associations = associations;
        }

        public IDictionary<String, BytesRef> Build(IntsRef ordinals, IEnumerable<CategoryPath> categories)
        {
            HashMap<String, BytesRef> res = new HashMap<String, BytesRef>();
            int idx = 0;
            foreach (CategoryPath cp in categories)
            {
                ICategoryAssociation association = associations.GetAssociation(cp);
                if (association == null)
                {
                    ++idx;
                    continue;
                }

                BytesRef bytes = res[association.CategoryListID];
                if (bytes == null)
                {
                    bytes = new BytesRef(32);
                    res[association.CategoryListID] = bytes;
                }

                int maxBytesNeeded = 4 + association.MaxBytesNeeded + bytes.length;
                if (bytes.bytes.Length < maxBytesNeeded)
                {
                    bytes.Grow(maxBytesNeeded);
                }

                output.Reset((byte[])(Array)bytes.bytes, bytes.length, bytes.bytes.Length - bytes.length);
                output.WriteInt(ordinals.ints[idx++]);
                association.Serialize(output);
                bytes.length = output.Position;
            }

            return res;
        }
    }
}
