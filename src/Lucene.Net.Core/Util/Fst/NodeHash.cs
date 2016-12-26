using Lucene.Net.Support;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
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

    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedGrowableWriter = Lucene.Net.Util.Packed.PagedGrowableWriter;

    // Used to dedup states (lookup already-frozen states)
    internal sealed class NodeHash<T>
    {
        private PagedGrowableWriter table;
        private long count;
        private long mask;
        private readonly FST<T> fst;
        private readonly FST.Arc<T> scratchArc = new FST.Arc<T>();
        private readonly FST.BytesReader input;

        public NodeHash(FST<T> fst, FST.BytesReader input)
        {
            table = new PagedGrowableWriter(16, 1 << 30, 8, PackedInts.COMPACT);
            mask = 15;
            this.fst = fst;
            this.input = input;
        }

        private bool NodesEqual(Builder<T>.UnCompiledNode<T> node, long address)
        {
            fst.ReadFirstRealTargetArc(address, scratchArc, input);
            if (scratchArc.BytesPerArc != 0 && node.NumArcs != scratchArc.NumArcs)
            {
                return false;
            }
            for (int arcUpto = 0; arcUpto < node.NumArcs; arcUpto++)
            {
                Builder<T>.Arc<T> arc = node.Arcs[arcUpto];
                if (arc.Label != scratchArc.Label || !arc.Output.Equals(scratchArc.Output) || ((Builder<T>.CompiledNode)arc.Target).Node != scratchArc.Target || !arc.NextFinalOutput.Equals(scratchArc.NextFinalOutput) || arc.IsFinal != scratchArc.IsFinal)
                {
                    return false;
                }

                if (scratchArc.IsLast)
                {
                    if (arcUpto == node.NumArcs - 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                fst.ReadNextRealArc(scratchArc, input);
            }

            return false;
        }

        // hash code for an unfrozen node.  this must be identical
        // to the frozen case (below)!!
        private long Hash(Builder<T>.UnCompiledNode<T> node)
        {
            const int PRIME = 31;
            //System.out.println("hash unfrozen");
            long h = 0;
            // TODO: maybe if number of arcs is high we can safely subsample?
            for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                Builder<T>.Arc<T> arc = node.Arcs[arcIdx];
                h = PRIME * h + arc.Label;
                long n = ((Builder<T>.CompiledNode)arc.Target).Node;
                h = PRIME * h + (int)(n ^ (n >> 32));
                h = PRIME * h + arc.Output.GetHashCode();
                h = PRIME * h + arc.NextFinalOutput.GetValueHashCode();
                if (arc.IsFinal)
                {
                    h += 17;
                }
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        // hash code for a frozen node
        private long Hash(long node)
        {
            const int PRIME = 31;
            //System.out.println("hash frozen node=" + node);
            long h = 0;
            fst.ReadFirstRealTargetArc(node, scratchArc, input);
            while (true)
            {
                //System.out.println("  label=" + scratchArc.label + " target=" + scratchArc.target + " h=" + h + " output=" + fst.outputs.outputToString(scratchArc.output) + " next?=" + scratchArc.flag(4) + " final?=" + scratchArc.isFinal() + " pos=" + in.getPosition());
                h = PRIME * h + scratchArc.Label;
                h = PRIME * h + (int)(scratchArc.Target ^ (scratchArc.Target >> 32));
                h = PRIME * h + scratchArc.Output.GetHashCode();
                h = PRIME * h + scratchArc.NextFinalOutput.GetValueHashCode();
                if (scratchArc.IsFinal)
                {
                    h += 17;
                }
                if (scratchArc.IsLast)
                {
                    break;
                }
                fst.ReadNextRealArc(scratchArc, input);
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        public long Add(Builder<T>.UnCompiledNode<T> nodeIn)
        {
            //System.out.println("hash: add count=" + count + " vs " + table.size() + " mask=" + mask);
            long h = Hash(nodeIn);
            long pos = h & mask;
            int c = 0;
            while (true)
            {
                long v = table.Get(pos);
                if (v == 0)
                {
                    // freeze & add
                    long node = fst.AddNode(nodeIn);
                    //System.out.println("  now freeze node=" + node);
                    long hashNode = Hash(node);
                    Debug.Assert(hashNode == h, "frozenHash=" + hashNode + " vs h=" + h);
                    count++;
                    table.Set(pos, node);
                    // Rehash at 2/3 occupancy:
                    if (count > 2 * table.Size() / 3)
                    {
                        Rehash();
                    }
                    return node;
                }
                else if (NodesEqual(nodeIn, v))
                {
                    // same node is already here
                    return v;
                }

                // quadratic probe
                pos = (pos + (++c)) & mask;
            }
        }

        // called only by rehash
        private void AddNew(long address)
        {
            long pos = Hash(address) & mask;
            int c = 0;
            while (true)
            {
                if (table.Get(pos) == 0)
                {
                    table.Set(pos, address);
                    break;
                }

                // quadratic probe
                pos = (pos + (++c)) & mask;
            }
        }

        private void Rehash()
        {
            PagedGrowableWriter oldTable = table;

            table = new PagedGrowableWriter(2 * oldTable.Size(), 1 << 30, PackedInts.BitsRequired(count), PackedInts.COMPACT);
            mask = table.Size() - 1;
            for (long idx = 0; idx < oldTable.Size(); idx++)
            {
                long address = oldTable.Get(idx);
                if (address != 0)
                {
                    AddNew(address);
                }
            }
        }
    }
}