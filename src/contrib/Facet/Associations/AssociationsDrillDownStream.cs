using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Facet.Index;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class AssociationsDrillDownStream : DrillDownStream
    {
        private readonly IPayloadAttribute payloadAttribute;
        private readonly BytesRef payload;
        private readonly ByteArrayDataOutput output = new ByteArrayDataOutput();
        private readonly CategoryAssociationsContainer associations;

        public AssociationsDrillDownStream(CategoryAssociationsContainer associations, FacetIndexingParams indexingParams)
            : base(associations, indexingParams)
        {
            this.associations = associations;
            payloadAttribute = AddAttribute<IPayloadAttribute>();
            BytesRef bytes = payloadAttribute.Payload;
            if (bytes == null)
            {
                bytes = new BytesRef(new sbyte[4]);
                payloadAttribute.Payload = bytes;
            }

            bytes.offset = 0;
            this.payload = bytes;
        }

        protected override void AddAdditionalAttributes(CategoryPath cp, bool isParent)
        {
            if (isParent)
            {
                return;
            }

            ICategoryAssociation association = associations.GetAssociation(cp);
            if (association == null)
            {
                return;
            }

            if (payload.bytes.Length < association.MaxBytesNeeded)
            {
                payload.Grow(association.MaxBytesNeeded);
            }

            output.Reset((byte[])(Array)payload.bytes);
            association.Serialize(output);
            payload.length = output.Position;
        }
    }
}
