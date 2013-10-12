using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class ChainedFilter : Filter
    {
        public const int OR = 0;
        public const int AND = 1;
        public const int ANDNOT = 2;
        public const int XOR = 3;
        /**
         * Logical operation when none is declared. Defaults to OR.
         */
        public const int DEFAULT = OR;

        /**
         * The filter chain
         */
        private Filter[] chain = null;

        private int[] logicArray;

        private int logic = -1;

        public ChainedFilter(Filter[] chain)
        {
            this.chain = chain;
        }

        public ChainedFilter(Filter[] chain, int[] logicArray)
        {
            this.chain = chain;
            this.logicArray = logicArray;
        }

        public ChainedFilter(Filter[] chain, int logic)
        {
            this.chain = chain;
            this.logic = logic;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            int[] index = new int[1]; // use array as reference to modifiable int;
            index[0] = 0;             // an object attribute would not be thread safe.
            if (logic != -1)
            {
                return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, logic, index), acceptDocs);
            }
            else if (logicArray != null)
            {
                return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, logicArray, index), acceptDocs);
            }

            return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, DEFAULT, index), acceptDocs);
        }

        private DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context)
        {
            // we dont pass acceptDocs, we will filter at the end using an additional filter
            DocIdSet docIdSet = filter.GetDocIdSet(context, null);
            if (docIdSet == null)
            {
                return DocIdSet.EMPTY_DOCIDSET.Iterator();
            }
            else
            {
                DocIdSetIterator iter = docIdSet.Iterator();
                if (iter == null)
                {
                    return DocIdSet.EMPTY_DOCIDSET.Iterator();
                }
                else
                {
                    return iter;
                }
            }
        }

        private OpenBitSetDISI InitialResult(AtomicReaderContext context, int logic, int[] index)
        {
            AtomicReader reader = context.AtomicReader;
            OpenBitSetDISI result;
            /**
             * First AND operation takes place against a completely false
             * bitset and will always return zero results.
             */
            if (logic == AND)
            {
                result = new OpenBitSetDISI(GetDISI(chain[index[0]], context), reader.MaxDoc);
                ++index[0];
            }
            else if (logic == ANDNOT)
            {
                result = new OpenBitSetDISI(GetDISI(chain[index[0]], context), reader.MaxDoc);
                result.Flip(0, reader.MaxDoc); // NOTE: may set bits for deleted docs.
                ++index[0];
            }
            else
            {
                result = new OpenBitSetDISI(reader.MaxDoc);
            }
            return result;
        }

        private DocIdSet GetDocIdSet(AtomicReaderContext context, int logic, int[] index)
        {
            OpenBitSetDISI result = InitialResult(context, logic, index);
            for (; index[0] < chain.Length; index[0]++)
            {
                // we dont pass acceptDocs, we will filter at the end using an additional filter
                DoChain(result, logic, chain[index[0]].GetDocIdSet(context, null));
            }
            return result;
        }

        private DocIdSet GetDocIdSet(AtomicReaderContext context, int[] logic, int[] index)
        {
            if (logic.Length != chain.Length)
            {
                throw new ArgumentException("Invalid number of elements in logic array");
            }

            OpenBitSetDISI result = InitialResult(context, logic[0], index);
            for (; index[0] < chain.Length; index[0]++)
            {
                // we dont pass acceptDocs, we will filter at the end using an additional filter
                DoChain(result, logic[index[0]], chain[index[0]].GetDocIdSet(context, null));
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ChainedFilter: [");
            foreach (Filter aChain in chain)
            {
                sb.Append(aChain);
                sb.Append(' ');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private void DoChain(OpenBitSetDISI result, int logic, DocIdSet dis)
        {

            if (dis is OpenBitSet)
            {
                // optimized case for OpenBitSets
                switch (logic)
                {
                    case OR:
                        result.Or((OpenBitSet)dis);
                        break;
                    case AND:
                        result.And((OpenBitSet)dis);
                        break;
                    case ANDNOT:
                        result.AndNot((OpenBitSet)dis);
                        break;
                    case XOR:
                        result.Xor((OpenBitSet)dis);
                        break;
                    default:
                        DoChain(result, DEFAULT, dis);
                        break;
                }
            }
            else
            {
                DocIdSetIterator disi;
                if (dis == null)
                {
                    disi = DocIdSet.EMPTY_DOCIDSET.Iterator();
                }
                else
                {
                    disi = dis.Iterator();
                    if (disi == null)
                    {
                        disi = DocIdSet.EMPTY_DOCIDSET.Iterator();
                    }
                }

                switch (logic)
                {
                    case OR:
                        result.InPlaceOr(disi);
                        break;
                    case AND:
                        result.InPlaceAnd(disi);
                        break;
                    case ANDNOT:
                        result.InPlaceNot(disi);
                        break;
                    case XOR:
                        result.InPlaceXor(disi);
                        break;
                    default:
                        DoChain(result, DEFAULT, dis);
                        break;
                }
            }
        }
    }
}
