// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Queries
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// <para>
    /// Allows multiple <see cref="Filter"/>s to be chained.
    /// Logical operations such as <b>NOT</b> and <b>XOR</b>
    /// are applied between filters. One operation can be used
    /// for all filters, or a specific operation can be declared
    /// for each filter.
    /// </para>
    /// <para>
    /// Order in which filters are called depends on
    /// the position of the filter in the chain. It's probably
    /// more efficient to place the most restrictive filters/least 
    /// computationally-intensive filters first.
    /// </para>
    /// </summary>
    public class ChainedFilter : Filter
    {
        public const int OR = 0;
        public const int AND = 1;
        public const int ANDNOT = 2;
        public const int XOR = 3;
        /// <summary>
        /// Logical operation when none is declared. Defaults to OR.
        /// </summary>
        public const int DEFAULT = OR;

        /// <summary>
        /// The filter chain
        /// </summary>
        private readonly Filter[] chain = null;

        private readonly int[] logicArray;

        private readonly int logic = -1;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="chain"> The chain of filters </param>
        public ChainedFilter(Filter[] chain)
        {
            this.chain = chain;
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="chain"> The chain of filters </param>
        /// <param name="logicArray"> Logical operations to apply between filters </param>
        public ChainedFilter(Filter[] chain, int[] logicArray)
        {
            this.chain = chain;
            this.logicArray = logicArray;
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="chain"> The chain of filters </param>
        /// <param name="logic"> Logical operation to apply to ALL filters </param>
        public ChainedFilter(Filter[] chain, int logic)
        {
            this.chain = chain;
            this.logic = logic;
        }

        /// <summary>
        /// <seealso cref="Filter.GetDocIdSet"/>.
        /// </summary>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            int[] index = new int[1]; // use array as reference to modifiable int;
            index[0] = 0; // an object attribute would not be thread safe.
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

        private static DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context) // LUCENENET: CA1822: Mark members as static
        {
            // we dont pass acceptDocs, we will filter at the end using an additional filter
            DocIdSet docIdSet = filter.GetDocIdSet(context, null);
            if (docIdSet is null)
            {
                return DocIdSetIterator.GetEmpty();
            }
            else
            {
                DocIdSetIterator iter = docIdSet.GetIterator();
                if (iter is null)
                {
                    return DocIdSetIterator.GetEmpty();
                }
                else
                {
                    return iter;
                }
            }
        }

        private FixedBitSet InitialResult(AtomicReaderContext context, int logic, int[] index)
        {
            AtomicReader reader = context.AtomicReader;
            FixedBitSet result = new FixedBitSet(reader.MaxDoc);
            if (logic == AND)
            {
                result.Or(GetDISI(chain[index[0]], context));
                ++index[0];
            }
            else if (logic == ANDNOT)
            {
                result.Or(GetDISI(chain[index[0]], context));
                result.Flip(0, reader.MaxDoc); // NOTE: may set bits for deleted docs.
                ++index[0];
            }
            return result;
        }

        /// <summary>
        /// Delegates to each filter in the chain.
        /// </summary>
        /// <param name="context"> AtomicReaderContext </param>
        /// <param name="logic"> Logical operation </param>
        /// <param name="index"></param>
        /// <returns> DocIdSet </returns>
        private DocIdSet GetDocIdSet(AtomicReaderContext context, int logic, int[] index)
        {
            FixedBitSet result = InitialResult(context, logic, index);
            for (; index[0] < chain.Length; index[0]++)
            {
                // we dont pass acceptDocs, we will filter at the end using an additional filter
                DoChain(result, logic, chain[index[0]].GetDocIdSet(context, null));
            }
            return result;
        }

        /// <summary>
        /// Delegates to each filter in the chain.
        /// </summary>
        /// <param name="context"> AtomicReaderContext </param>
        /// <param name="logic"> Logical operation </param>
        /// <param name="index"></param>
        /// <returns> DocIdSet </returns>
        private DocIdSet GetDocIdSet(AtomicReaderContext context, int[] logic, int[] index)
        {
            if (logic.Length != chain.Length)
            {
                throw new ArgumentException("Invalid number of elements in logic array");
            }

            FixedBitSet result = InitialResult(context, logic[0], index);
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

        /// <exception cref="IOException"/>
        private void DoChain(FixedBitSet result, int logic, DocIdSet dis)
        {
            if (dis is FixedBitSet fixedBitSet)
            {
                // optimized case for FixedBitSets
                switch (logic)
                {
                    case OR:
                        result.Or(fixedBitSet);
                        break;
                    case AND:
                        result.And(fixedBitSet);
                        break;
                    case ANDNOT:
                        result.AndNot(fixedBitSet);
                        break;
                    case XOR:
                        result.Xor(fixedBitSet);
                        break;
                    default:
                        DoChain(result, DEFAULT, dis);
                        break;
                }
            }
            else
            {
                DocIdSetIterator disi;
                if (dis is null)
                {
                    disi = DocIdSetIterator.GetEmpty();
                }
                else
                {
                    disi = dis.GetIterator() ?? DocIdSetIterator.GetEmpty();
                }

                switch (logic)
                {
                    case OR:
                        result.Or(disi);
                        break;
                    case AND:
                        result.And(disi);
                        break;
                    case ANDNOT:
                        result.AndNot(disi);
                        break;
                    case XOR:
                        result.Xor(disi);
                        break;
                    default:
                        DoChain(result, DEFAULT, dis);
                        break;
                }
            }
        }
    }
}