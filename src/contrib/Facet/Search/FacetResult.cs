using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class FacetResult
    {
        private readonly FacetRequest facetRequest;
        private readonly FacetResultNode rootNode;
        private readonly int numValidDescendants;

        public FacetResult(FacetRequest facetRequest, FacetResultNode rootNode, int numValidDescendants)
        {
            this.facetRequest = facetRequest;
            this.rootNode = rootNode;
            this.numValidDescendants = numValidDescendants;
        }

        public FacetResultNode FacetResultNode
        {
            get
            {
                return rootNode;
            }
        }

        public int NumValidDescendants
        {
            get
            {
                return numValidDescendants;
            }
        }

        public FacetRequest FacetRequest
        {
            get
            {
                return this.facetRequest;
            }
        }

        public virtual string ToString(string prefix)
        {
            StringBuilder sb = new StringBuilder();
            string nl = @"";
            if (this.facetRequest != null)
            {
                sb.Append(nl).Append(prefix).Append(@"Request: ").Append(this.facetRequest.ToString());
                nl = @"\n";
            }

            sb.Append(nl).Append(prefix).Append(@"Num valid Descendants (up to specified depth): ").Append(this.numValidDescendants);
            nl = @"\n";
            if (this.rootNode != null)
            {
                sb.Append(nl).Append(this.rootNode.ToString(prefix + @"\t"));
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(@"");
        }
    }
}
