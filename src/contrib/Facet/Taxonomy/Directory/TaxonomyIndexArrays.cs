using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    internal class TaxonomyIndexArrays : ParallelTaxonomyArrays
    {
        private readonly int[] parents;
        private volatile bool initializedChildren = false;
        private int[] children, siblings;

        private TaxonomyIndexArrays(int[] parents)
        {
            this.parents = parents;
        }

        public TaxonomyIndexArrays(IndexReader reader)
        {
            parents = new int[reader.MaxDoc];
            if (parents.Length > 0)
            {
                InitParents(reader, 0);
                parents[0] = TaxonomyReader.INVALID_ORDINAL;
            }
        }

        public TaxonomyIndexArrays(IndexReader reader, TaxonomyIndexArrays copyFrom)
        {
            int[] copyParents = copyFrom.Parents;
            this.parents = new int[reader.MaxDoc];
            Array.Copy(copyParents, 0, parents, 0, copyParents.Length);
            InitParents(reader, copyParents.Length);
            if (copyFrom.initializedChildren)
            {
                InitChildrenSiblings(copyFrom);
            }
        }

        private void InitChildrenSiblings(TaxonomyIndexArrays copyFrom)
        {
            lock (this)
            {
                if (!initializedChildren)
                {
                    children = new int[parents.Length];
                    siblings = new int[parents.Length];
                    if (copyFrom != null)
                    {
                        Array.Copy(copyFrom.Children, 0, children, 0, copyFrom.Children.Length);
                        Array.Copy(copyFrom.Siblings, 0, siblings, 0, copyFrom.Siblings.Length);
                    }

                    ComputeChildrenSiblings(parents, 0);
                    initializedChildren = true;
                }
            }
        }

        private void ComputeChildrenSiblings(int[] parents, int first)
        {
            for (int i = first; i < parents.Length; i++)
            {
                children[i] = TaxonomyReader.INVALID_ORDINAL;
            }

            if (first == 0)
            {
                first = 1;
                siblings[0] = TaxonomyReader.INVALID_ORDINAL;
            }

            for (int i = first; i < parents.Length; i++)
            {
                siblings[i] = children[parents[i]];
                children[parents[i]] = i;
            }
        }

        private void InitParents(IndexReader reader, int first)
        {
            if (reader.MaxDoc == first)
            {
                return;
            }

            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, null, Consts.FIELD_PAYLOADS, Consts.PAYLOAD_PARENT_BYTES_REF, DocsAndPositionsEnum.FLAG_PAYLOADS);
            if (positions == null || positions.Advance(first) == DocIdSetIterator.NO_MORE_DOCS)
            {
                throw new CorruptIndexException(@"Missing parent data for category " + first);
            }

            int num = reader.MaxDoc;
            for (int i = first; i < num; i++)
            {
                if (positions.DocID == i)
                {
                    if (positions.Freq == 0)
                    {
                        throw new CorruptIndexException(@"Missing parent data for category " + i);
                    }

                    parents[i] = positions.NextPosition();
                    if (positions.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i + 1 < num)
                        {
                            throw new CorruptIndexException(@"Missing parent data for category " + (i + 1));
                        }

                        break;
                    }
                }
                else
                {
                    throw new CorruptIndexException(@"Missing parent data for category " + i);
                }
            }
        }

        internal virtual TaxonomyIndexArrays Add(int ordinal, int parentOrdinal)
        {
            if (ordinal >= parents.Length)
            {
                int[] newarray = ArrayUtil.Grow(parents, ordinal + 1);
                newarray[ordinal] = parentOrdinal;
                return new TaxonomyIndexArrays(newarray);
            }

            parents[ordinal] = parentOrdinal;
            return this;
        }

        public override int[] Parents
        {
            get
            {
                return parents;
            }
        }

        public override int[] Children
        {
            get
            {
                if (!initializedChildren)
                {
                    InitChildrenSiblings(null);
                }

                return children;
            }
        }

        public override int[] Siblings
        {
            get
            {
                if (!initializedChildren)
                {
                    InitChildrenSiblings(null);
                }

                return siblings;
            }
        }
    }
}
